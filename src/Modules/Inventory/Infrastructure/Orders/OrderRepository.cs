using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Inventory.Infrastructure.Orders;

internal sealed class OrderRepository : IOrderRepository
{
    private readonly InventoryDbContext _context;

    public OrderRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct)
    {
        return await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task AddAsync(Order order, CancellationToken ct)
    {
        await _context.Orders.AddAsync(order, ct);
    }
}
