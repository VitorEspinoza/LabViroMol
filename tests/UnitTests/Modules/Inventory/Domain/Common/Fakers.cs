using Bogus;
using LabViroMol.Modules.Inventory.Domain.Kits;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Inventory.Domain.References;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.UnitTests.Common;

internal static class Fakers
{
    private static readonly Faker F = new("pt_BR");

    #region Primitives

    public static UserId AnyUserId() => IdFactory.New<UserId>();
    public static MaterialId AnyMaterialId() => IdFactory.New<MaterialId>();
    public static ProjectId AnyProjectId() => IdFactory.New<ProjectId>();

    public static Quantity AnyQuantity(decimal? value = null)
        => new(value ?? F.Random.Decimal(1, 500));

    public static Quantity QuantityOf(decimal value) => new(value);


    #endregion
   
    #region MaterialType

    public static MaterialType CreateMaterialType(string? name = null)
        => MaterialType.Create(AnyUserId(), name ?? F.Commerce.Categories(1)[0]);
    
    public static MaterialType CreateActiveMaterialType(string? name = null)
        => CreateMaterialType(name);
    
    public static MaterialType CreateInactiveMaterialType(string? name = null)
    {
        var type = CreateMaterialType(name);
        type.Deactivate(AnyUserId());
        return type;
    }

    #endregion

    #region Material

    public static Material CreateMaterial(
        MaterialType? type = null,
        string? name = null,
        string? location = null,
        Quantity? minQuantity = null,
        Quantity? stockQuantity = null,
        Unit? unit = null,
        UserId? createdBy = null)
    {
        var resolvedType = type ?? CreateActiveMaterialType();

        return Material.Create(
            createdBy ?? AnyUserId(),
            name ?? F.Commerce.ProductName(),
            location ?? F.Address.City(),
            minQuantity ?? QuantityOf(10),
            stockQuantity ?? QuantityOf(50),
            unit ?? Unit.Gram,
            resolvedType).Data!;
    }

    public static Material CreateMaterialWithStock(decimal stock = 100, decimal min = 10)
        => CreateMaterial(stockQuantity: QuantityOf(stock), minQuantity: QuantityOf(min));


    #endregion
   
    #region Kit

    public static KitItem AnyKitItem(MaterialId? materialId = null, Quantity? quantity = null)
        => new(materialId ?? AnyMaterialId(), quantity ?? AnyQuantity());

    public static List<KitItem> AnyKitItems(int count = 2)
        => Enumerable.Range(0, count).Select(_ => AnyKitItem()).ToList();

    public static Kit CreateKit(
        string? name = null,
        string? description = null,
        List<KitItem>? items = null)
        => Kit.Create(
            AnyUserId(),
            name ?? F.Commerce.ProductName(),
            description ?? F.Lorem.Sentence(),
            items ?? AnyKitItems());

    #endregion

    #region Order

    public static Order CreateOrder(
        MaterialId? materialId = null,
        ProjectId? projectId = null,
        Quantity? quantity = null,
        string? description = null,
        UserId? createdBy = null)
        => Order.Create(
            materialId ?? AnyMaterialId(),
            projectId ?? AnyProjectId(),
            createdBy ?? AnyUserId(),
            quantity ?? AnyQuantity(),
            description ?? F.Lorem.Sentence());

    #endregion

   
}
