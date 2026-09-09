using ATLAS.Models.ViewModels;

namespace ATLAS.Models;

/// <summary>
/// Informações da academia exibidas no site e gerenciáveis futuramente
/// pelo painel administrativo (sem alterar código).
/// Hoje vêm do appsettings.json (seção "SiteConfig"); amanhã, do banco de dados.
/// </summary>
public class SiteConfig
{
    public string Nome { get; set; } = "Atlas Centro de Treinamento";
    public string Slogan { get; set; } = "Supere. Treine. Evolua.";
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string HorarioSemana { get; set; } = string.Empty;
    public string HorarioSabado { get; set; } = string.Empty;

    /// <summary>URL de incorporação do Google Maps (iframe, sem API key).</summary>
    public string MapaEmbedUrl { get; set; } = string.Empty;

    /// <summary>Link para abrir a localização no site do Google Maps.</summary>
    public string MapaLinkUrl { get; set; } = string.Empty;

    /// <summary>Estatísticas exibidas nos cards de destaque da Home.</summary>
    public List<DestaqueItem> Destaques { get; set; } = new();

    /// <summary>Modalidades/serviços exibidos na Home.</summary>
    public List<ModalidadeItem> Modalidades { get; set; } = new();
}
