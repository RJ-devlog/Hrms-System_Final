using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HRMS_System.Pages.Hrms.DailyLogs.UploadHistory
{
    public class IndexModel : PageModel
    {
        public List<UploadHistoryRow> History { get; set; } = new();

        public void OnGet()
        {
            // TEMP placeholder (palitan later galing DB)
        }

        public class UploadHistoryRow
        {
            public DateTime UploadedAt { get; set; }
            public string FileName { get; set; } = "";
            public string UploadedBy { get; set; } = "";
            public int ParsedCount { get; set; }
            public int ImportedCount { get; set; }
            public int ErrorCount { get; set; }
            public bool IsSuccess { get; set; }
        }
    }
}
