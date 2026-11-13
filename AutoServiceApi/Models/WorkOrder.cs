namespace AutoServiceApi.Models;

public class WorkOrder
{
    public int Id { get; set; }
    public string CarNumber { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Cost { get; set; }
    public string Status { get; set; } = "Открыт";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}