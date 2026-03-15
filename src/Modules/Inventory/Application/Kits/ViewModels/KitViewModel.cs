namespace LabViroMol.Modules.Inventory.Application.Kits.ViewModels;

public record KitViewModel(Guid Id, string Name, string Description, List<KitItemViewModel> Items);
