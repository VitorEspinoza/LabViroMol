using LabViroMol.Modules.Shared.Kernel.Primitives;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.References;
using LabViroMol.Modules.Shared.Kernel.Identity;

namespace LabViroMol.Modules.Inventory.Domain.Orders;

public class Order : AggregateRoot<OrderId>, ICreationAuditable, IModificationAuditable
{

    private Order() {}
    private Order(OrderId id, MaterialId materialId, ProjectId projectId, Quantity requestedQuantity, string description)
        : base(id)
    {
        MaterialId = materialId;
        ProjectId = projectId;
        Status = OrderStatus.Pending;
        RequestedQuantity = requestedQuantity;
        Description = description;
    }
    public MaterialId MaterialId { get; private set; }
    public ProjectId ProjectId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Quantity RequestedQuantity { get; private set; }
    public OrderProcessing? Processing { get; private set; }
    public OrderReceipt? Receipt { get; private set; }
    public string Description { get; private set; }

    public static Order Create(MaterialId materialId, ProjectId projectId, Quantity quantity, string description)
    {
        return new Order(OrderId.New(), materialId, projectId, quantity, description);
    }

    public Result FixDetails(ProjectId newProjectId, Quantity newRequestedQuantity, string description)
    {
        if (Status != OrderStatus.Pending)
            return Result.BusinessRule("Apenas pedidos pendentes podem ter detalhes corrigidos.");

        ProjectId = newProjectId;
        RequestedQuantity = newRequestedQuantity;
        Description = description;

        return Result.Success();
    }

    public Result Process(UserId processedBy, string processedByName, string? processingNotes)
    {
        if (Status != OrderStatus.Pending)
            return Result.BusinessRule("Apenas pedidos pendentes podem ser processados.");

        Status = OrderStatus.Processing;

        Processing = new OrderProcessing(processedBy, processedByName, DateTimeOffset.UtcNow, processingNotes);
        return Result.Success();
    }

    public Result Receive(UserId receivedBy, string receivedByName, Quantity quantityReceived,  string? receiptNotes)
    {
        if (Status != OrderStatus.Processing)
            return Result.BusinessRule("Apenas pedidos em processamento podem ser recebidos.");

        Status = OrderStatus.Completed;

        Receipt = new OrderReceipt(receivedBy, receivedByName, receiptNotes, quantityReceived, DateTimeOffset.UtcNow);
        AddEvent(new OrderReceivedDomainEvent(Id, MaterialId,quantityReceived, receivedBy));

        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status != OrderStatus.Pending)
            return Result.BusinessRule("Apenas pedidos pendentes podem ser cancelados.");

        Status = OrderStatus.Canceled;

        return Result.Success();
    }
}
