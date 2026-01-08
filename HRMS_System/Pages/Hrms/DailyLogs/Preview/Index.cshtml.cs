using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HRMS_System.Pages.Hrms.DailyLogs.Preview
{
    public class IndexModel : PageModel
    {
        public List<PreviewRow> PreviewRows { get; set; } = new();

        public void OnGet()
        {
            // TEMP placeholder (palitan later galing DB / staging)
        }

        public class PreviewRow
        {
            public string EmployeeIdNumber { get; set; } = "";
            public string EmployeeName { get; set; } = "";
            public DateTime AttendanceDate { get; set; }
            public TimeSpan? TimeIn { get; set; }
            public TimeSpan? TimeOut { get; set; }
            public string Status { get; set; } = "On-Time";
            public string StatusClass { get; set; } = "pill-ok";
            public string Note { get; set; } = "";
        }
    }
}
