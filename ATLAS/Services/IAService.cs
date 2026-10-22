using ATLAS.Models;
using Microsoft.Extensions.Options;

namespace ATLAS.Services;

/// <summary>
/// Assistente virtual LOCAL (mock) — não chama API externa.
/// Respostas baseadas em palavras-chave da pergunta do usuário.
/// </summary>
public class IAService : IIAService
{
    private readonly IAConfig _cfg;
    private readonly ILogger<IAService> _logger;

    // Respostas pré-definidas por categoria
    private static readonly Dictionary<string, string[]> Respostas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["horario"] = new[]
        {
            "🕐 **Horários de funcionamento:**\n• Seg a Sex: 5:40–11:00 e 13:00–21:00\n• Sábado: 8:00–11:00\n• Domingo e feriados: Fechado",
            "Funcionamos de segunda a sexta das 5h40 às 11h e das 13h às 21h. Sábado só de manhã (8h–11h). Domingo fechado."
        },
        ["plano"] = new[]
        {
            "💰 **Planos disponíveis:**\n• Mensal: R$ 100,00\n• Avulsa (aula única): R$ 20,00\n• Trimestral: R$ 260,00\n\nTodos incluem acesso total + avaliação física.",
            "Temos: Mensal (R$ 100), Avulsa (R$ 20/aula) e Trimestral (R$ 260). O trimestral sai por R$ 86,66/mês."
        },
        ["modalidade"] = new[]
        {
            "🏋️ **Modalidades:**\n• Musculação\n• Treinamento Funcional\n• Pilates\n• Fisioterapia\n\nTodas com acompanhamento profissional.",
            "Oferecemos Musculação, Funcional, Pilates e Fisioterapia. Qual te interessa mais?"
        },
        ["cadastro"] = new[]
        {
            "📝 **Como se cadastrar:**\n1. Clique em \"Criar conta\" no topo\n2. Preencha nome, email, telefone, data de nascimento e senha\n3. Pronto! Já pode agendar sua avaliação.",
            "Vá em /cadastro, preencha o formulário (2 min) e sua conta fica ativa na hora."
        },
        ["login"] = new[]
        {
            "🔑 **Login:** Use seu email e senha em /login. Se esqueceu a senha, clique em \"Esqueci minha senha\".",
            "Acesse /login com seu email e senha. Problemas? Use a recuperação de senha."
        },
        ["senha"] = new[]
        {
            "🔐 **Recuperar senha:** Vá em /esqueci-senha, informe seu email e enviaremos um link para redefinir (válido por 30 min).",
            "Clique em \"Esqueci minha senha\" na tela de login, digite seu email e siga o link que chegar na sua caixa de entrada."
        },
        ["treino"] = new[]
        {
            "💪 **Seu treino:** Após a avaliação, seu personal monta o treino no app. Você vê em \"Meus Treinos\" (área do aluno).",
            "Treinos são personalizados pelo seu professor. Acesse a área do aluno → Meus Treinos para ver séries, cargas e vídeos.",
            "📅 **Frequência ideal:** Para iniciantes, **3 a 4 dias por semana** (alternados) é ótimo. Avançados podem treinar 5-6x. O importante é consistência e recuperação.",
            "🏋️ **Quantos dias treinar:** Iniciante = 3x/semana (ex: seg/qua/sex). Intermediário = 4x. Avançado = 5-6x. Seu personal define o ideal na avaliação."
        },
        ["agendamento"] = new[]
        {
            "📅 **Agendamentos:** Na área do aluno → Agendamentos. Escolha modalidade, dia, horário e confirmamos na hora.",
            "Agende aulas de Funcional, Pilates ou avaliação direto no app. Área do aluno → Agendamentos."
        },
        ["contato"] = new[]
        {
            "📞 **Contato:**\n• WhatsApp: (11) 99999-0000\n• Email: contato@atlasct.com.br\n• Presencial: Av. Dr. José Erineu Ortigosa, 187 - Barra Bonita/SP",
            "Fale conosco pelo WhatsApp (11) 99999-0000 ou email contato@atlasct.com.br."
        },
        ["default"] = new[]
        {
            "🤖 Sou o Assistente Atlas! Posso ajudar com:\n• Horários e planos\n• Modalidades (Musculação, Funcional, Pilates, Fisioterapia)\n• Cadastro, login, recuperação de senha\n• Treinos e agendamentos\n• Contato e localização\n\nO que você gostaria de saber?",
            "Olá! 👋 Sou o assistente virtual do Atlas. Pergunte sobre horários, planos, treinos, agendamentos ou como se cadastrar."
        }
    };

    public IAService(IOptions<IAConfig> cfg, ILogger<IAService> logger)
    {
        _cfg = cfg.Value;
        _logger = logger;
    }

    public bool Configurado => _cfg.Habilitado;

    public Task<RespostaIA> ConversarAsync(IReadOnlyList<MensagemIA> historico, string mensagem, CancellationToken ct = default)
    {
        if (!Configurado)
        {
            return Task.FromResult(new RespostaIA(StatusIA.NaoConfigurado,
                "O assistente está desabilitado nas configurações."));
        }

        var msg = (mensagem ?? string.Empty).ToLowerInvariant();
        var resposta = EscolherResposta(msg);

        _logger.LogInformation("IA (mock): pergunta='{Pergunta}' → categoria='{Categoria}'", mensagem, DetectarCategoria(msg));

        return Task.FromResult(new RespostaIA(StatusIA.Sucesso, resposta));
    }

    private string DetectarCategoria(string msg)
    {
        if (msg.Contains("horario") || msg.Contains("horário") || msg.Contains("abre") || msg.Contains("fecha") || msg.Contains("funciona")) return "horario";
        if (msg.Contains("plano") || msg.Contains("preço") || msg.Contains("preco") || msg.Contains("valor") || msg.Contains("mensal") || msg.Contains("anual")) return "plano";
        if (msg.Contains("modalidade") || msg.Contains("aula") || msg.Contains("pilates") || msg.Contains("funcional") || msg.Contains("musculação") || msg.Contains("musculacao") || msg.Contains("fisio")) return "modalidade";
        if (msg.Contains("cadastro") || msg.Contains("cadastrar") || msg.Contains("criar conta") || msg.Contains("registrar")) return "cadastro";
        if (msg.Contains("login") || msg.Contains("entrar") || msg.Contains("acessar")) return "login";
        if (msg.Contains("senha") || msg.Contains("esqueci") || msg.Contains("redefinir") || msg.Contains("recuperar")) return "senha";
        if (msg.Contains("treino") || msg.Contains("treinar") || msg.Contains("exercício") || msg.Contains("exercicio") || msg.Contains("série") || msg.Contains("serie") || msg.Contains("carga") || msg.Contains("dia") || msg.Contains("semana") || msg.Contains("frequência") || msg.Contains("frequencia") || msg.Contains("vezes") || msg.Contains("quantos") || msg.Contains("quantas")) return "treino";
        if (msg.Contains("agend") || msg.Contains("marcar") || msg.Contains("aula") || msg.Contains("reservar")) return "agendamento";
        if (msg.Contains("contato") || msg.Contains("telefone") || msg.Contains("whatsapp") || msg.Contains("email") || msg.Contains("endereço") || msg.Contains("endereco") || msg.Contains("onde fica")) return "contato";
        return "default";
    }

    private string EscolherResposta(string msg)
    {
        var categoria = DetectarCategoria(msg);
        var opcoes = Respostas.GetValueOrDefault(categoria, Respostas["default"]);
        var idx = Random.Shared.Next(opcoes.Length);
        return opcoes[idx];
    }
}
