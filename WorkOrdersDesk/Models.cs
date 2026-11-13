using System;
using System.Collections.Generic;

namespace WorkOrdersDesk
{
    public class WorkOrder
    {
        public int Id { get; set; }
        public string CarNumber { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ValidationProblem
    {
        public string Title { get; set; }
        public Dictionary<string, string[]> Errors { get; set; }
    }
}
