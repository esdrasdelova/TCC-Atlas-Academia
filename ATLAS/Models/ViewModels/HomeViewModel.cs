namespace ATLAS.Models.ViewModels;

public class HomeViewModel
{
    public List<DestaqueItem> Destaques { get; set; } = new();
    public List<ModalidadeItem> Modalidades { get; set; } = new();
    public List<HorarioItem> Horarios { get; set; } = new();
}