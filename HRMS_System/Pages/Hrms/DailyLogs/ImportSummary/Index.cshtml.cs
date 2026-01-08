using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HRMS_System.Pages.Hrms.DailyLogs.ImportSummary
{
    public class IndexModel : PageModel
    {
        // Summary cards
        public int SummaryParsed { get; set; }
        public int SummaryValid { get; set; }
        public int SummaryImported { get; set; }
        public int SummaryErrors { get; set; }

        // Error list
        public List<string> Errors { get; set; } = new();

        public void OnGet()
        {
            // TEMP placeholder values (palitan mo later galing DB)
            SummaryParsed = 0;
            SummaryValid = 0;
            SummaryImported = 0;
            SummaryErrors = 0;
        }
    }
}
