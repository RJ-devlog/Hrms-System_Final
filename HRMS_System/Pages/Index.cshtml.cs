using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HRMS_System.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            //    return RedirectToPage("/Account/LoginPage");                      // Login
                 return RedirectToPage("/Hrms/EmployeeManagement/Index");   // EmployeeManagement
            //   return RedirectToPage("/Hrms/AttendanceTracking/index");     // AttendanceTracking
         //  return RedirectToPage("/Hrms/DailyLogs/DailyLogsUpload/Index");  // Supervisor Dashboard
        }
    }
}
