namespace HRMS_System.Models.ViewModels
{
    public class EmployeeRowVM
    {
        public string EmployeeNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int TenureMonths { get; set; }

        public float? LatestEvalAvg { get; set; }
        public float? LatestPerformanceEvaluationScore { get; set; }

        public string Rating { get; set; } = "—";
        public string SearchText { get; set; } = string.Empty;
    }
}
