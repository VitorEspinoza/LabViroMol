namespace LabViroMol.Modules.Inventory.Application.Materials.ViewModels;

public record MaterialViewModel(Guid Id, string Name, string MaterialType, decimal MinStock, decimal StockQuantity, string Unit, string Location);
