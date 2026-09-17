using ATLAS.Data;
using ATLAS.Middleware;
using ATLAS.Models;
using ATLAS.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Atlas") ?? string.Empty;

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configurações do site (editáveis futuramente pelo painel ADM)
builder.Services.Configure<SiteConfig>(builder.Configuration.GetSection("SiteConfig"));
builder.Services.AddSingleton<ISiteConfigService, SiteConfigService>();

// Envio real de e-mails via SMTP (formulários de contato e agendamento)
builder.Services.Configure<EmailConfig>(builder.Configuration.GetSection("Email"));
builder.Services.AddSingleton<IEmailService, EmailService>();

// Cache em memória: usado para limite de tentativas/solicitações na recuperação de senha.
builder.Services.AddMemoryCache();

// Banco de dados (PostgreSQL no Supabase) + serviços de negócio.
// A connection string vem de appsettings/env, com a senha injetada via dotnet user-secrets.
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
    // O header X-Frame-Options já é aplicado globalmente pelo middleware.
    o.SuppressXFrameOptionsHeader = true;
});

// Autenticação por cookies (login único para aluno, personal e administrador,
// com o papel definido pelo tipo da conta no banco).
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

        // Consumidores da API JSON recebem 401/403 (e não redirect para o login),
        // enquanto o restante da aplicação continua com o redirect normal.
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

// Aplica o schema do banco (cria tabelas se necessário) e popula/separa os dados iniciais.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
    BancoInicializador.GarantirSchema(db);
    BancoInicializador.GarantirNotificacoesLidas(db);
    DbSeeder.Seed(db);
    DbSeeder.CorrigirSenhasPendentes(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// 404 e demais códigos de status passam pela página de erro estilizada.
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

// Cabeçalhos de segurança e controle de cache das áreas autenticadas/API.
app.UseMiddleware<SegurancaHttpMiddleware>();

app.UseRouting();

app.UseAuthentication();

// Áreas autenticadas usam [Authorize(Roles = Permissoes.X)] nos controllers.
app.UseAuthorization();

app.MapStaticAssets();

// Health check: dotnet run → GET /healthz (não requer login).
app.MapHealthChecks("/healthz");

// Site institucional (raiz): / , /Home/Privacy ...
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Áreas autenticadas: /aluno , /personal , /admin , /login , /cadastro
// e API JSON (/api/...). Rotas definidas por atributos nos controllers.
app.MapControllers()
   .WithStaticAssets();

app.Run();