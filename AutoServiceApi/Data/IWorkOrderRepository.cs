using AutoServiceApi.Models;

namespace AutoServiceApi.Data;

public interface IWorkOrderRepository
{
    Task<IReadOnlyList<WorkOrder>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<WorkOrder?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default);

    Task<WorkOrder> AddAsync(
        WorkOrder workOrder, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int id, CancellationToken cancellationToken = default);
}