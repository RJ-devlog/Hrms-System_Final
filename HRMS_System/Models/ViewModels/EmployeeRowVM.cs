namespace HRMS_System.Models.ViewModels
{
    public class EmployeeRowVM
    {
        public string EmployeeNumber { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? Department { get; set; }
        public int TenureMonths { get; set; }
        public float LatestEvalAvg { get; set; }

        public string SearchText => $"{EmployeeNumber} {FullName} {Department}";
    }
}