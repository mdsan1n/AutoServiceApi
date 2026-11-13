using Microsoft.AspNetCore.Mvc;
using AutoServiceApi.Data;
using AutoServiceApi.Models;

namespace AutoServiceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkOrdersController : ControllerBase
{
    private readonly IWorkOrderRepository _repository;
    private readonly ILogger<WorkOrdersController> _logger;

    public WorkOrdersController(
        IWorkOrderRepository repository,
        ILogger<WorkOrdersController> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WorkOrder>>> GetAll(
        CancellationToken cancellationToken)
    {
        var items = await _repository.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkOrder>> GetById(
        int id, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkOrder>> Create(
        CreateWorkOrderRequest request,
        CancellationToken cancellationToken)
    {
        var all = await _repository.GetAllAsync(cancellationToken);

        bool hasOpenOrder = all.Any(w =>
            w.CarNumber.Equals(request.CarNumber, StringComparison.OrdinalIgnoreCase)
            && w.Status == "Открыт");

        if (hasOpenOrder)
        {
            return Conflict(
                $"На автомобиль с госномером {request.CarNumber} уже есть открытый заказ");
        }

        var workOrder = new WorkOrder
        {
            CarNumber = request.CarNumber.ToUpperInvariant(),
            ClientName = request.ClientName,
            Description = request.Description,
            Cost = request.Cost!.Value
        };

        var created = await _repository.AddAsync(workOrder, cancellationToken);
        _logger.LogInformation("Создан заказ {WorkOrderId}", created.Id);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        int id, CancellationToken cancellationToken)
    {
        var workOrder = await _repository.GetByIdAsync(id, cancellationToken);
        if (workOrder is null)
            return NotFound();

        if (workOrder.Status == "В работе")
            return Conflict("Нельзя удалить заказ, который находится в работе");
        await _repository.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
