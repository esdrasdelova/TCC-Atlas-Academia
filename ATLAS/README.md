# Atlas — Centro de Treinamento

Sistema da academia **Atlas** com dois objetivos complementares:

1. **Site institucional** — apresentar a academia (Home, sobre, modalidades, contato).
2. **Plataforma de apoio ao gerenciamento** — contas de alunos/personais/administrador, treinos online e painel administrativo, reduzindo o impacto da alta demanda sobre os profissionais.

---

## Tecnologias

- ASP.NET Core MVC (.NET 10), C#, Razor Views
- HTML5 + CSS3 + JavaScript vanilla (sem frameworks de UI)
- Entity Framework Core + **SQLite** (`atlas.db`) com migrations
- Autenticação por **cookies** (hash de senha PBKDF2, sem texto puro)

## Como executar

```bash
dotnet run --project ATLAS.csproj
```

Na primeira execução o banco é criado/migrado automaticamente (`Migrations/InitialCreate`) e populado com dados iniciais (`Data/DbSeeder.cs`). O banco é preservado entre execuções.

### Contas de demonstração (senha `123456`)

| Perfil | E-mail |
|---|---|
| Administrador | `admin@atlasct.com.br` |
| Personal | `carlos.mendes@atlasct.com.br` |
| Aluno | `joao.silva@email.com` |

## Estrutura do projeto

```
ATLAS/
├── Program.cs                     # DI + auth por cookie + rotas (público + áreas)
├── Migrations/                    # InitialCreate (EF Core)
├── Data/
│   ├── AtlasDbContext.cs          # DbContext (TPH: Usuario → Aluno/Personal/Admin)
│   ├── DbSeeder.cs                # seed idempotente (usuários, exercícios, treinos)
│   └── MigracoesBootstrap.cs      # registra a migration em bancos legados
├── Controllers/                   # Site institucional (público)
├── Areas/
│   ├── Account/                   # /login /cadastro /logout /sem-acesso
│   ├── Aluno/                     # [Authorize] dashboard, meus treinos, perfil
│   ├── Personal/                  # [Authorize(Personal)] alunos, treinos, banco de exercícios
│   └── Admin/                     # [Authorize(Administrador)] CRUDs completos
├── Models/
│   ├── Entities/                  # Usuario (TPH), Treino, TreinoExercicio, Exercicio…
│   ├── Enums/                     # StatusConta, StatusAgendamento
│   ├── Permissoes.cs              # papéis p/ [Authorize(Roles=...)]
│   └── *ViewModels.cs             # formulários e listagens das áreas
├── Services/
│   ├── ITreinoService.cs          # regras de negócio dos treinos
│   ├── TreinoService.cs           # (criar/editar/excluir/publicar, escopo por perfil)
│   └── SiteConfigService.cs       # infos do site via appsettings.json
├── Views/Shared/
│   ├── _Layout.cshtml             # público (navbar + footer + assistente)
│   ├── _LayoutAuth.cshtml         # login/cadastro (tela centrada)
│   ├── _LayoutDashboard.cshtml    # sidebar compartilhado pelas 3 áreas + assistente
│   ├── _FormularioTreino.cshtml   # formulário dinâmico de treino (Personal e ADM)
│   └── _Assistente.cshtml         # assistente local de dúvidas (sem APIs externas)
├── appsettings.json               # connection string "Atlas" + seção "SiteConfig"
└── wwwroot/
    ├── css/site.css | dashboard.css | assistente.css
    ├── js/site.js | treino-form.js | assistente.js
    └── images/logo-atlas.png
```

## Rotas principais

| Rota | Acesso | Descrição |
|---|---|---|
| `/`, `/sobre`, `/modalidades`, `/planos`, `/contato`, `/agendamento` | pública | site institucional |
| `/login`, `/cadastro`, `/logout` | pública | autenticação (cookies) |
| `/aluno` | Aluno | treino ativo, histórico e perfil próprios |
| `/personal` | Personal | carteira de alunos vinculados (`Aluno.PersonalId`), CRUD de treinos próprios e banco de exercícios |
| `/admin` | Administrador | dashboard real, CRUD de alunos/personais/treinos |

Regras aplicadas:

- Cada área é protegida por `[Authorize(Roles = ...)]`; tentativas caem em `/sem-acesso`.
- O personal enxerga **apenas** seus alunos e os treinos sob sua responsabilidade.
- O aluno vê apenas os treinos publicados para ele.
- Exclusões de alunos/personais removem os treinos dependentes (cascata controlada no código), com confirmação explícita na interface.
- O ADM não pode desativar/excluir a própria conta enquanto estiver autenticado.

## Assistente local

Botão flutuante "Precisa de ajuda?" presente no site público e nos painéis: respostas educativas sobre treino/saúde geradas **localmente** (JavaScript puro, sem chamadas externas), com aviso de que não substitui orientação profissional.

## Identidade visual

- Fundo escuro `#0a0a0b`, superfícies cinza `#141416`
- Verde fluorescente suave `#b8ff3d` apenas em detalhes (botões, ícones, hovers)
- Títulos em Oswald (esportiva) + textos em Inter
- Logo em `wwwroot/images/logo-atlas.png`

## Roadmap futuro

1. Edição de conteúdo público pelo ADM (modalidades, planos, `SiteConfig` no banco).
2. Agendamentos (entidade pronta; tela do aluno e agenda do personal).
3. Avaliações físicas e acompanhamento de evolução.
