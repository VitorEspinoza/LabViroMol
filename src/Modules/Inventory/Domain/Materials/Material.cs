
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Inventory.Domain.References;
using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.Materials;

public class Material : AggregateRoot<MaterialId>
{
    private Material(MaterialId id, UserId createdBy, string name, string location, Quantity minStock, Quantity stockQuantity, Unit unit, MaterialTypeId typeId)
        : base(id, createdBy)
    {
        Name = name;
        Location = location;
        StockQuantity = stockQuantity;
        MinStock = minStock;
        Unit = unit;
        TypeId = typeId;
    }
    
    private Material() { }

    public string Name { get; private set; }
    public Quantity StockQuantity { get; private set; }
    public Unit Unit { get; private set; }
    public Quantity MinStock { get; private set; }
    public string Location { get; private set; }
    public MaterialTypeId TypeId { get; private set; }

    private readonly List<StockTransaction> _transactions = new();
    public IReadOnlyCollection<StockTransaction> Transactions => _transactions;

    public static Result<Material> Create(UserId createdBy, string name, string location, Quantity minStock, Quantity stockQuantity, Unit unit, MaterialType type )
    {
        if (!type.Active)
            return Result<Material>.BusinessRule("Não é possível cadastrar um material para um tipo inativo.");

        var material = new Material(IdFactory.New<MaterialId>(), createdBy, name, location, minStock, stockQuantity, unit, type.Id);
        
        if (stockQuantity.Value > 0)
        {
            var initialTransaction = StockTransaction.CreateExceptionIn(
                material.Id, stockQuantity, "Inventário inicial no cadastro do sistema.", createdBy); 
            
            material._transactions.Add(initialTransaction);
        }
        
        return Result<Material>.Success(material);
    }

    public void Update(string name, Quantity minStock, string location, UserId modifiedBy)
    {
        Name = name;
        MinStock = minStock;
        Location = location;
        MarkAsUpdated(modifiedBy);
    }

 public Result ReceiveFromOrder(OrderId orderId, Quantity quantity, UserId modifiedBy)
    {
        var transaction = StockTransaction.CreateReceipt(Id, orderId, quantity, modifiedBy);
        _transactions.Add(transaction);
        
        StockQuantity += quantity;
        MarkAsUpdated(modifiedBy);
        
        return Result.Success();
    }

    public void AddStockException(Quantity quantity, string justification, UserId modifiedBy)
    {
        var transaction = StockTransaction.CreateExceptionIn(Id, quantity, justification, modifiedBy);
        _transactions.Add(transaction);

        StockQuantity += quantity;
        MarkAsUpdated(modifiedBy);
    }
    
    public Result ConsumeForProject(ProjectId projectId, Quantity quantity, UserId modifiedBy)
    {
        if (quantity > StockQuantity)
            return Result.BusinessRule("Quantidade insuficiente para o consumo do projeto.");

        var transaction = StockTransaction.CreateProjectConsumption(Id, projectId, quantity, modifiedBy);
        _transactions.Add(transaction);

        StockQuantity -= quantity;
        MarkAsUpdated(modifiedBy);
        CheckMinStockThreshold();

        return Result.Success();
    }

    public Result RemoveStockException(Quantity quantity, string justification, UserId modifiedBy)
    {
        if (quantity > StockQuantity)
            return Result.BusinessRule("Quantidade insuficiente para realizar esta baixa.");

        var transaction = StockTransaction.CreateExceptionOut(Id, quantity, justification, modifiedBy);
        _transactions.Add(transaction);

        StockQuantity -= quantity;
        MarkAsUpdated(modifiedBy);
        CheckMinStockThreshold();

        return Result.Success();
    }

    private void CheckMinStockThreshold()
    {
        if (StockQuantity <= MinStock)
        {
            AddEvent(new LowStockDomainEvent(Id, StockQuantity));
        }
    }

   
}
