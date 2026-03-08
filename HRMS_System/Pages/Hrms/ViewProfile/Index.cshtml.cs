using System.Security.Claims;
using HRMS_System.Data;
using HRMS_System.Infrastructure;
using HRMS_System.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HRMS_System.Pages.Hrms.ViewProfile
{
    [RoleAuthorize(UserRole.Supervisor, UserRole.Manager, UserRole.CEO)]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public UserInformationModel? Profile { get; private set; }
        public bool ProfileNotFound => Profile == null;

        public async Task OnGetAsync()
        {
            // get logged-in user id from claim
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idStr, out var userId))
            {
                Profile = null;
                return;
            }

            // Try load profile by matching id
            Profile = await _context.UserInformation
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.id == userId);
        }
    }
}