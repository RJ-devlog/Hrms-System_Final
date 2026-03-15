namespace HRMS_System.Models.ViewModels
{
    public class ManagementSummaryRow
    {
        public int UserId { get; set; }
        public string EmployeeNumber { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string Department { get; set; } = "";
        public string EvaluatorName { get; set; } = "";
        public string EvaluatorRole { get; set; } = "";
        public decimal OverallAverage { get; set; }
    }
}
