using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HRMS_System.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
          //    return RedirectToPage("/Dashboard/EmployeeManagement");
               return RedirectToPage("/Hrms/EmployeeManagement/Index");
         //   return RedirectToPage("/Dashboard/EditEmployeeInfo");
        }
    }
}
