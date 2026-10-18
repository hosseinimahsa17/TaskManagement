namespace TaskManagement.Api.DTOs
{
    public class DailyTaskSummaryDto
    {
        public string Date { get; set; } = string.Empty;
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
    }
}
