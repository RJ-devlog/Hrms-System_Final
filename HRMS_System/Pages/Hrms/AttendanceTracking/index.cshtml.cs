using HRMS_System.Data;
using HRMS_System.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HRMS_System.Pages.Dashboard.AttendanceTracking
{
    public class AttendanceTrackingPageModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AttendanceTrackingPageModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // TEMP: load employees only
        public List<UserInformationModel> Employees { get; set; } = new();

        public async Task OnGetAsync()
        {
            Employees = await _context.UserInformation
                .OrderBy(e => e.LastName)
                .ToListAsync();
        }
    }
}
