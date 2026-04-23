using LayeredSample.Application.Abstractions;
using LayeredSample.Domain.Orders;

namespace LayeredSample.Infrastructure.Data;

public sealed class SqlOrderRepository : IOrderRepository
{
    public Order GetCurrentOrder()
    {
        return new Order("Northwind", 180m);
    }
}
