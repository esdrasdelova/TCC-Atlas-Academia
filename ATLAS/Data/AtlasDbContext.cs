using ATLAS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ATLAS.Data;

/// <summary>
/// Contexto do banco de dados — PostgreSQL no Supabase (via Npgsql). A
/// definição das entidades não depende do provider, então trocar de banco não
/// exige mudanças nas entidades nem nos serviços, apenas na connection string
/// e na configuração do provider em Program.cs.
/// </summary>
public class AtlasDbContext : DbContext
{
    public AtlasDbContext(DbContextOptions<AtlasDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Aluno> Alunos => Set<Aluno>();
    public DbSet<PersonalTrainer> Personais => Set<PersonalTrainer>();
    public DbSet<Administrador> Administradores => Set<Administrador>();
    public DbSet<Exercicio> Exercicios => Set<Exercicio>();
    public DbSet<Treino> Treinos => Set<Treino>();
    public DbSet<TreinoExercicio> TreinoExercicios => Set<TreinoExercicio>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<NotificacaoLida> NotificacoesLidas => Set<NotificacaoLida>();
    public DbSet<MedidaCorporal> Medidas => Set<MedidaCorporal>();
    public DbSet<FotoProgresso> Fotos => Set<FotoProgresso>();
    public DbSet<Fatura> Faturas => Set<Fatura>();
    public DbSet<RegistroFrequencia> Frequencias => Set<RegistroFrequencia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Garante que todos os DateTime sejam armazenados como UTC no PostgreSQL.
        // O Npgsql exige Kind=UTC em colunas timestamptz.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                        v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                        v => v));
                }
            }
        }

        // E-mail único por conta — evita contas duplicadas e acelera o login.
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // A carteira do personal é a consulta mais frequente da área dele.
        modelBuilder.Entity<Aluno>()
            .HasIndex(a => a.PersonalId);

        // Treino ativo por aluno (dashboard do aluno) e por personal (carteira).
        modelBuilder.Entity<Treino>()
            .HasIndex(t => new { t.AlunoId, t.Ativo });

        modelBuilder.Entity<Treino>()
            .HasIndex(t => t.PersonalId);

        modelBuilder.Entity<Treino>()
            .HasMany(t => t.Itens)
            .WithOne(i => i.Treino)
            .HasForeignKey(i => i.TreinoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Exercícios do catálogo não podem ser apagados por cascata
        // enquanto existirem treinos que os referenciam.
        modelBuilder.Entity<TreinoExercicio>()
            .HasOne(i => i.Exercicio)
            .WithMany()
            .HasForeignKey(i => i.ExercicioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TreinoExercicio>()
            .HasIndex(i => i.TreinoId);

        modelBuilder.Entity<TreinoExercicio>()
            .HasIndex(i => i.ExercicioId);

        modelBuilder.Entity<Exercicio>()
            .HasIndex(e => e.GrupoMuscular);

        // Filtros de agendamento (aluno, personal e por data/hora).
        modelBuilder.Entity<Agendamento>()
            .HasIndex(a => a.AlunoId);

        modelBuilder.Entity<Agendamento>()
            .HasIndex(a => a.PersonalId);

        modelBuilder.Entity<Agendamento>()
            .HasIndex(a => a.DataHora);

        // Agendamentos de um aluno são removidos junto com a conta.
        modelBuilder.Entity<Agendamento>()
            .HasOne(a => a.Aluno)
            .WithMany(a => a.Agendamentos)
            .HasForeignKey(a => a.AlunoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Agendamento>()
            .HasOne(a => a.Personal)
            .WithMany()
            .HasForeignKey(a => a.PersonalId)
            .OnDelete(DeleteBehavior.SetNull);

        // Notificações lidas por aluno — uma única marca por chave.
        modelBuilder.Entity<NotificacaoLida>()
            .HasOne(n => n.Aluno)
            .WithMany()
            .HasForeignKey(n => n.AlunoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NotificacaoLida>()
            .HasIndex(n => new { n.AlunoId, n.Chave })
            .IsUnique();

        // Medidas corporais — histórico do aluno ordenado por data.
        modelBuilder.Entity<MedidaCorporal>()
            .HasOne(m => m.Aluno)
            .WithMany(a => a.Medidas)
            .HasForeignKey(m => m.AlunoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MedidaCorporal>()
            .HasIndex(m => new { m.AlunoId, m.DataMedicao });

        // Fotos de progresso — a galeria do aluno.
        modelBuilder.Entity<FotoProgresso>()
            .HasOne(f => f.Aluno)
            .WithMany(a => a.Fotos)
            .HasForeignKey(f => f.AlunoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FotoProgresso>()
            .HasIndex(f => new { f.AlunoId, f.EnviadaEm });

        // Faturas — extrato financeiro do aluno.
        modelBuilder.Entity<Fatura>()
            .HasOne(f => f.Aluno)
            .WithMany(a => a.Faturas)
            .HasForeignKey(f => f.AlunoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Fatura>()
            .HasIndex(f => new { f.AlunoId, f.DataVencimento });

        // Frequência — check-ins para o cálculo de presença.
        modelBuilder.Entity<RegistroFrequencia>()
            .HasOne(r => r.Aluno)
            .WithMany(a => a.Frequencias)
            .HasForeignKey(r => r.AlunoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RegistroFrequencia>()
            .HasIndex(r => new { r.AlunoId, r.DataTreino });
    }
}
