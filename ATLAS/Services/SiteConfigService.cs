using ATLAS.Models;
using Microsoft.Extensions.Options;

namespace ATLAS.Services;

/// <summary>
/// Implementação atual: lê a seção "SiteConfig" do appsettings.json.
/// Quando o banco for implementado, trocar por uma versão que consulta
/// as tabelas de configuração — o restante do site não precisa mudar.
/// </summary>
public class SiteConfigService : ISiteConfigService
{
    private readonly SiteConfig _config;

    public SiteConfigService(IOptions<SiteConfig> options)
    {
        _config = options.Value;
    }

    public SiteConfig Obter() => _config;
}
