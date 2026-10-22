using ATLAS.Models.Entities;
using ATLAS.Models.Enums;
using ATLAS.Services;

namespace ATLAS.Data;

/// <summary>
/// Popula o banco com dados iniciais na primeira execução: catálogo de
/// exercícios, personal, alunos e treinos de exemplo — mantendo a mesma
/// nomenclatura dos dados demonstrativos já exibidos nas telas.
/// </summary>
public static class DbSeeder
{
    /// <summary>Senha inicial das contas criadas pelo seed (troca futura no perfil).</summary>
    public const string SenhaPadrao = "123456";

    /// <summary>Marcador usado antes da autenticação existir — corrigido no startup.</summary>
    public const string SenhaPlaceholderAntigo = "hash-pendente-etapa-de-login";

    public static void Seed(AtlasDbContext db)
    {
        if (db.Usuarios.Any())
        {
            return;
        }

        var senhaPendente = SegurancaSenha.GerarHash(SenhaPadrao);

        var personal = new PersonalTrainer
        {
            NomeCompleto = "Carlos Mendes",
            Email = "carlos.mendes@atlasct.com.br",
            Telefone = "(14) 99777-0101",
            SenhaHash = senhaPendente,
            DataNascimento = new DateTime(1988, 9, 3),
            RegistroProfissional = "000000-G/SP",
            Especialidade = "Hipertrofia e força",
            CriadoEm = DateTime.UtcNow.AddMonths(-20)
        };

        var administrador = new Administrador
        {
            NomeCompleto = "Administrador Atlas",
            Email = "admin@atlasct.com.br",
            Telefone = "(14) 99999-0001",
            SenhaHash = senhaPendente,
            DataNascimento = new DateTime(1990, 1, 15),
            CriadoEm = DateTime.UtcNow.AddMonths(-24)
        };

        var joao = new Aluno
        {
            NomeCompleto = "João Silva",
            Email = "joao.silva@email.com",
            Telefone = "(11) 99999-1234",
            SenhaHash = senhaPendente,
            DataNascimento = new DateTime(1998, 4, 12),
            Objetivo = "Ganho de massa muscular (+4kg até dezembro)",
            ProximaAvaliacao = new DateTime(2026, 9, 15),
            Personal = personal,
            CriadoEm = DateTime.UtcNow.AddMonths(-17)
        };

        var pedro = new Aluno
        {
            NomeCompleto = "Pedro Santos",
            Email = "pedro.santos@email.com",
            Telefone = "(11) 99999-5678",
            SenhaHash = senhaPendente,
            DataNascimento = new DateTime(2000, 11, 30),
            Objetivo = "Força",
            Personal = personal,
            CriadoEm = DateTime.UtcNow.AddMonths(-10)
        };

        var maria = new Aluno
        {
            NomeCompleto = "Maria Oliveira",
            Email = "maria.oliveira@email.com",
            Telefone = "(11) 99999-9012",
            SenhaHash = senhaPendente,
            DataNascimento = new DateTime(1995, 6, 21),
            Objetivo = "Emagrecimento",
            Personal = personal,
            CriadoEm = DateTime.UtcNow.AddMonths(-7)
        };

        var lucas = new Aluno
        {
            NomeCompleto = "Lucas Souza",
            Email = "lucas.souza@email.com",
            Telefone = "(11) 99999-3456",
            SenhaHash = senhaPendente,
            DataNascimento = new DateTime(1999, 2, 8),
            Objetivo = "Condicionamento",
            Status = StatusConta.Inativa,
            Personal = personal,
            CriadoEm = DateTime.UtcNow.AddMonths(-4)
        };

        db.AddRange(personal, administrador, joao, pedro, maria, lucas);

        var catalogo = new List<Exercicio>
        {
            new() { Nome = "Supino reto com barra", GrupoMuscular = "Peito", Descricao = "Exercício base para o peitoral." },
            new() { Nome = "Supino inclinado com halteres", GrupoMuscular = "Peito" },
            new() { Nome = "Crucifixo na máquina", GrupoMuscular = "Peito" },
            new() { Nome = "Puxada frontal", GrupoMuscular = "Costas" },
            new() { Nome = "Remada baixa no cabo", GrupoMuscular = "Costas" },
            new() { Nome = "Remada unilateral com halter", GrupoMuscular = "Costas" },
            new() { Nome = "Agachamento livre", GrupoMuscular = "Pernas" },
            new() { Nome = "Leg press 45°", GrupoMuscular = "Pernas" },
            new() { Nome = "Cadeira extensora", GrupoMuscular = "Pernas" },
            new() { Nome = "Mesa flexora", GrupoMuscular = "Pernas" },
            new() { Nome = "Panturrilha em pé", GrupoMuscular = "Pernas" },
            new() { Nome = "Desenvolvimento com halteres", GrupoMuscular = "Ombros" },
            new() { Nome = "Elevação lateral", GrupoMuscular = "Ombros" },
            new() { Nome = "Rosca direta na barra", GrupoMuscular = "Bíceps" },
            new() { Nome = "Tríceps na polia", GrupoMuscular = "Tríceps" },
            new() { Nome = "Tríceps francês", GrupoMuscular = "Tríceps" },
            new() { Nome = "Prancha isométrica", GrupoMuscular = "Abdômen" }
        };
        db.Exercicios.AddRange(catalogo);

        Exercicio ex(string nome) => catalogo.First(e => e.Nome == nome);

        var treinoA = new Treino
        {
            Aluno = joao,
            Personal = personal,
            Nome = "Treino A",
            Objetivo = "Peito e Tríceps · Hipertrofia",
            Observacoes = "Aquecer 5 min na esteira antes de iniciar. Cadência controlada na fase excêntrica (2s descendo).",
            Publicado = true,
            Ativo = true,
            DataCriacao = DateTime.UtcNow.AddDays(-3)
        };
        treinoA.Itens =
        [
            new TreinoExercicio { Exercicio = ex("Supino reto com barra"), Ordem = 1, Series = 4, Repeticoes = "8-10", Carga = "40 kg", DescansoSegundos = 90 },
            new TreinoExercicio { Exercicio = ex("Supino inclinado com halteres"), Ordem = 2, Series = 4, Repeticoes = "10-12", Carga = "18 kg", DescansoSegundos = 75 },
            new TreinoExercicio { Exercicio = ex("Crucifixo na máquina"), Ordem = 3, Series = 3, Repeticoes = "12-15", Carga = "25 kg", DescansoSegundos = 60 },
            new TreinoExercicio { Exercicio = ex("Tríceps na polia"), Ordem = 4, Series = 4, Repeticoes = "12", Carga = "30 kg", DescansoSegundos = 60 },
            new TreinoExercicio { Exercicio = ex("Tríceps francês"), Ordem = 5, Series = 3, Repeticoes = "10-12", Carga = "15 kg", DescansoSegundos = 60, Observacoes = "Cotovelos travados durante toda a execução." }
        ];

        var treinoB = new Treino
        {
            Aluno = joao,
            Personal = personal,
            Nome = "Treino B",
            Objetivo = "Costas e Bíceps · Hipertrofia",
            Publicado = true,
            Ativo = false,
            DataCriacao = DateTime.UtcNow.AddDays(-10)
        };
        treinoB.Itens =
        [
            new TreinoExercicio { Exercicio = ex("Puxada frontal"), Ordem = 1, Series = 4, Repeticoes = "10", Carga = "45 kg", DescansoSegundos = 90 },
            new TreinoExercicio { Exercicio = ex("Remada baixa no cabo"), Ordem = 2, Series = 4, Repeticoes = "10-12", Carga = "38 kg", DescansoSegundos = 75 },
            new TreinoExercicio { Exercicio = ex("Remada unilateral com halter"), Ordem = 3, Series = 3, Repeticoes = "12", Carga = "22 kg", DescansoSegundos = 60 },
            new TreinoExercicio { Exercicio = ex("Rosca direta na barra"), Ordem = 4, Series = 4, Repeticoes = "10-12", Carga = "20 kg", DescansoSegundos = 60 }
        ];

        var treinoPedro = new Treino
        {
            Aluno = pedro,
            Personal = personal,
            Nome = "Treino A",
            Objetivo = "Corpo inteiro · Força",
            Publicado = true,
            Ativo = true,
            DataCriacao = DateTime.UtcNow.AddDays(-7)
        };
        treinoPedro.Itens =
        [
            new TreinoExercicio { Exercicio = ex("Agachamento livre"), Ordem = 1, Series = 5, Repeticoes = "5", Carga = "80 kg", DescansoSegundos = 150, Observacoes = "Foco em força — progressão de carga semanal." },
            new TreinoExercicio { Exercicio = ex("Supino reto com barra"), Ordem = 2, Series = 5, Repeticoes = "5", Carga = "55 kg", DescansoSegundos = 150 },
            new TreinoExercicio { Exercicio = ex("Leg press 45°"), Ordem = 3, Series = 3, Repeticoes = "10", Carga = "120 kg", DescansoSegundos = 90 }
        ];

        db.Treinos.AddRange(treinoA, treinoB, treinoPedro);

        db.SaveChanges();
    }

    /// <summary>
    /// Reparo em bancos criados antes da autenticação: substitui o placeholder
    /// antigo pelo hash da senha padrão, sem recriar nem apagar o banco atual.
    /// </summary>
    public static void CorrigirSenhasPendentes(AtlasDbContext db)
    {
        // A validação do formato do hash é feita em memória (não traduzível p/ SQL);
        // a tabela de usuários é pequena, então carregá-la é barato e seguro.
        var usuarios = db.Usuarios.ToList();
        var pendentes = usuarios
            .Where(u => u.SenhaHash == SenhaPlaceholderAntigo || !SegurancaSenha.EhHashValido(u.SenhaHash))
            .ToList();

        if (pendentes.Count == 0)
        {
            return;
        }

        foreach (var usuario in pendentes)
        {
            usuario.SenhaHash = SegurancaSenha.GerarHash(SenhaPadrao);
        }

        db.SaveChanges();
    }
}
