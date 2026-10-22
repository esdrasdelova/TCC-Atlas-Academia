namespace ATLAS.Models.ViewModels;

public class HorarioItem
{
    public string Dia { get; set; } = string.Empty;
    public string Horario { get; set; } = string.Empty;
    public string Detalhe { get; set; } = string.Empty;
    public string Icone { get; set; } = string.Empty;
    public bool Fechado { get; set; }
}