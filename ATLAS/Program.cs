using ATLAS.Data;
using ATLAS.Middleware;
using ATLAS.Models;
using ATLAS.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Segredos locais (NÃO versionados): connection string do banco.
// Fica num arquivo separado para que o código-fonte possa ir para o GitHub
// sem expor senha do banco. Em nuvem (Render/Azure), use variáveis de ambiente.
builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

// Hospedagens em nuvem injetam a porta via variável PORT (o servidor precisa
// escutar em 0.0.0.0, não em localhost, para o tráfego externo chegar).
if (!string.IsNullOrWhiteSpace(builder.Configuration["PORT"])
    && string.IsNullOrWhiteSpace(builder.Configuration["ASPNETCORE_URLS"]))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{builder.Configuration["PORT"]}");
}

var connectionString = builder.Configuration.GetConnectionString("Atlas") ?? string.Empty;

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configurações do site (editáveis futuramente pelo painel ADM)
builder.Services.Configure<SiteConfig>(builder.Configuration.GetSection("SiteConfig"));
builder.Services.AddSingleton<ISiteConfigService, SiteConfigService>();

// Envio de e-mails: mock (dev) ou real via Resend/Brevo/SMTP conforme config "Email"
builder.Services.AddHttpClient();
builder.Services.Configure<EmailConfig>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailService, EmailService>();

// Assistente virtual LOCAL (mock) — respostas pré-definidas, sem API externa
builder.Services.Configure<IAConfig>(builder.Configuration.GetSection("IA"));
builder.Services.AddScoped<IIAService, IAService>();

// Cache em memória: usado para limite de tentativas/solicitações na recuperação de senha.
builder.Services.AddMemoryCache();

// Banco de dados (PostgreSQL no Supabase) + serviços de negócio.
builder.Services.AddDbContext<AtlasDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<ITreinoService, TreinoService>();
builder.Services.AddScoped<IAgendamentoService, AgendamentoService>();

// Proteção contra força bruta no login (janela deslizante + bloqueio temporário)
builder.Services.Configure<LoginProtectionOptions>(builder.Configuration.GetSection("LoginProtection"));
builder.Services.AddSingleton<ILoginProtectionService, LoginProtectionService>();

// Observabilidade: health check com verificação real do banco e o nome da app.
builder.Services.AddHealthChecks()
    .AddCheck<BancoHealthCheck>("postgres", Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, timeout: TimeSpan.FromSeconds(5));

builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "X-CSRF-TOKEN";
    o.SuppressXFrameOptionsHeader = true;
});

// Autenticação por cookies (login único para aluno, personal e administrador)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/sem-acesso";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "Atlas.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.Events.OnRedirectToLogin = contexto =>
        {
            if (contexto.Request.Path.StartsWithSegments("/api"))
            {
                contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            contexto.Response.Redirect(contexto.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = contexto =>
        {
            if (contexto.Request.Path.StartsWithSegments("/api"))
            {
                contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            contexto.Response.Redirect(contexto.RedirectUri);
            return Task.CompletedTask;
        };
    });

var app = builder.Build();

// URLs corretas (ex.: link de recuperação de senha) mesmo atrás de proxy
var encaminhados = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
if (app.Configuration.GetValue<bool>("ConfiarProxy"))
{
    encaminhados.KnownIPNetworks.Clear();
    encaminhados.KnownProxies.Clear();
}
app.UseForwardedHeaders(encaminhados);

// Aplica o schema do banco (cria tabelas se necessário) e popula/separa os dados iniciais.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
    BancoInicializador.GarantirSchema(db);
    BancoInicializador.GarantirNotificacoesLidas(db);
    DbSeeder.Seed(db);
    DbSeeder.CorrigirSenhasPendentes(db);

    // Diagnóstico de startup (dentro do scope para resolver serviços scoped)
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var ia = scope.ServiceProvider.GetRequiredService<IIAService>();
    var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

    if (!ia.Configurado)
    {
        logger.LogWarning("Assistente IA desabilitado (configuração).");
    }
    else
    {
        logger.LogInformation("Assistente IA (mock) habilitado.");
    }

    logger.LogInformation("E-mail modo '{Modo}' — remetente {Remetente}.", app.Configuration["Email:Modo"], app.Configuration["Email:Remetente"]);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");
app.UseMiddleware<SegurancaHttpMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapHealthChecks("/healthz");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapControllers().WithStaticAssets();

app.Run();