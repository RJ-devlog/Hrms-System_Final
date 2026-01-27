using HRMS_System.Data;
using HRMS_System.Models.Training;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HRMS_System.Pages.Hrms.TrainingandSeminar
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Used by your .cshtml loops
        public List<TrainingRecord> Records { get; set; } = new();
        public List<TrainingSession> Sessions { get; set; } = new();

        // ===== Bind these to your modals =====
        [BindProperty]
        public TrainingSession CreateSession { get; set; } = new();

        [BindProperty]
        public TrainingSession EditSession { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        // ================= CREATE SESSION =================
        public async Task<IActionResult> OnPostCreateSessionAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            _context.TrainingSessions.Add(CreateSession);
            await _context.SaveChangesAsync();

            // ✅ Redirect to the SAME page (avoids route mismatch issues)
            return RedirectToPage(new { tab = "tab-sessions" });
        }

        // ================= EDIT SESSION =================
        public async Task<IActionResult> OnPostEditSessionAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            var session = await _context.TrainingSessions
                .FirstOrDefaultAsync(s => s.Id == EditSession.Id);

            if (session == null)
            {
                ModelState.AddModelError("", "Session not found.");
                await LoadDataAsync();
                return Page();
            }

            session.Title = EditSession.Title;
            session.Description = EditSession.Description;
            session.TargetAudience = EditSession.TargetAudience;

            session.SessionType = EditSession.SessionType;
            session.StartDate = EditSession.StartDate;
            session.StartTime = EditSession.StartTime;
            session.EndTime = EditSession.EndTime;

            session.Provider = EditSession.Provider;
            session.TrainingType = EditSession.TrainingType;
            session.Progress = EditSession.Progress;

            await _context.SaveChangesAsync();

            // ✅ Redirect to the SAME page (avoids route mismatch issues)
            return RedirectToPage(new { tab = "tab-sessions" });
        }

        // ================= HELPER: LOAD LISTS =================
        private async Task LoadDataAsync()
        {
            Records = await _context.TrainingRecords
                .Include(r => r.User)
                .Include(r => r.Session)
                .OrderByDescending(r => r.DateCompleted ?? DateTime.MinValue)
                .ToListAsync();

            Sessions = await _context.TrainingSessions
                .OrderByDescending(s => s.StartDate)
                .ThenByDescending(s => s.StartTime)
                .ToListAsync();
        }
    }
}
