namespace HRMS_System.Models.ViewModels
{
    public class PerformanceHistoryRow
    {
        public int Year { get; set; }
        public decimal Score { get; set; }
        public string Trend { get; set; } = "";
    }
}
