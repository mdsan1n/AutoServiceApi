using AutoServiceApi.Models;

namespace AutoServiceApi.Data;

public class InMemoryWorkOrderRepository : IWorkOrderRepository
{
    private readonly List<WorkOrder> _items =
    [
        new()
        {
            Id = 1,
            CarNumber = "А123ВС777",
            ClientName = "Иванов Сергей Петрович",
            Description = "Замена масла и масляного фильтра, диагностика ходовой",
            Cost = 4500m,
            Status = "Открыт"
        },
        new()
        {
            Id = 2,
            CarNumber = "К456МН199",
            ClientName = "Петрова Анна Владимировна",
            Description = "Ремонт тормозной системы, замена колодок и дисков",
            Cost = 18500m,
            Status = "В работе"
        },
        new()
        {
            Id = 3,
            CarNumber = "Е789ОР777",
            ClientName = "Сидоров Алексей Николаевич",
            Description = "Компьютерная диагностика двигателя, замена свечей",
            Cost = 7200m,
            Status = "Закрыт"
        }
    ];

    private readonly object _sync = new();
    private int _nextId = 4;

    public Task<IReadOnlyList<WorkOrder>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<WorkOrder>>(_items.ToList());
        }
    }

    public Task<WorkOrder?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_items.FirstOrDefault(w => w.Id == id));
        }
    }

    public Task<WorkOrder> AddAsync(
        WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            workOrder.Id = _nextId++;
            _items.Add(workOrder);
            return Task.FromResult(workOrder);
        }
    }

    public Task<bool> DeleteAsync(
        int id, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            int removed = _items.RemoveAll(w => w.Id == id);
            return Task.FromResult(removed > 0);
        }
    }
}