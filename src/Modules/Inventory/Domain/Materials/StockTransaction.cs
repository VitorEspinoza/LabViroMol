using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Inventory.Domain.References;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.Materials;

public class StockTransaction : BaseEntity<StockTransactionId>
{
    public MaterialId MaterialId { get; private set; } 
    public OrderId? OrderId { get; private set; }   
    public ProjectId? ProjectId { get; private set; }
    
    public Quantity Quantity { get; private set; } 
    
    public TransactionType Type { get; private set; } 
    
    public DateTime TransactedAt { get; private set; }
    public UserId TransactedByUserId { get; private set; }
    
    public string? Justification { get; private set; } 

    private StockTransaction() { }

    internal static StockTransaction CreateReceipt(
        MaterialId materialId, 
        OrderId orderId, 
        Quantity quantity, 
        UserId userId)
    {
        if (quantity == 0)
            throw new DomainException("A quantidade de recebimento não pode ser igual a zero.");

        return new StockTransaction
        {
            Id = IdFactory.New<StockTransactionId>(),
            MaterialId = materialId,
            OrderId = orderId,
            Quantity = quantity, 
            Type = TransactionType.OrderReceipt,
            TransactedAt = DateTimeOffset.UtcNow.UtcDateTime,
            TransactedByUserId = userId,
            Justification = null
        };
    }
    
    internal static StockTransaction CreateProjectConsumption(
        MaterialId materialId, 
        ProjectId projectId, 
        Quantity quantity, 
        UserId userId)
    {
        if (quantity == 0)
            throw new DomainException("A quantidade de consumo não pode ser igual a zero.");
        
        return new StockTransaction
        {
            Id = IdFactory.New<StockTransactionId>(),
            MaterialId = materialId,
            ProjectId = projectId,
            Quantity = quantity, 
            Type = TransactionType.ProjectConsumption,
            TransactedAt = DateTimeOffset.UtcNow.UtcDateTime,
            TransactedByUserId = userId,
            Justification = null
        };
    }

    internal static StockTransaction CreateExceptionIn(MaterialId materialId, Quantity quantity, string justification, UserId userId)
    {
        if (quantity == 0)
            throw new DomainException("A quantidade da entrada não pode ser igual a zero.");

        return new StockTransaction
        {
            Id = IdFactory.New<StockTransactionId>(),
            MaterialId = materialId,
            Quantity = quantity,
            Type = TransactionType.ExceptionIn,
            TransactedAt = DateTimeOffset.UtcNow.UtcDateTime,
            TransactedByUserId = userId,
            Justification = Guard.AgainstMinLength(justification, 10, "Para entradas de exceção, uma justificativa detalhada (mínimo 10 caracteres) é obrigatória."),
            
        };
    }
    
    internal static StockTransaction CreateExceptionOut(
        MaterialId materialId, 
        Quantity quantity, 
        string justification, 
        UserId userId)
    {
        if (quantity == 0)
            throw new DomainException("A quantidade da baixa não pode ser igual a zero.");

        return new StockTransaction
        {
            Id = IdFactory.New<StockTransactionId>(),
            MaterialId = materialId,
            Quantity = quantity, 
            Type = TransactionType.ExceptionOut,
            TransactedAt = DateTimeOffset.UtcNow.UtcDateTime,
            TransactedByUserId = userId,
            Justification = Guard.AgainstMinLength(justification, 10, "Para saídas de exceção, uma justificativa detalhada (mínimo 10 caracteres) é obrigatória."),
        };
    }
}
