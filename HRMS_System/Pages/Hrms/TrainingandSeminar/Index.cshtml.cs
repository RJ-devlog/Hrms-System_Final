using HRMS_System.Data;
using HRMS_System.Models.Training;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HRMS_System.Pages.Hrms.TrainingandSeminar
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public IndexModel(ApplicationDbContext context) => _context = context;

        public List<TrainingSession> Sessions { get; set; } = new();
        public List<TrainingRecord> Records { get; set; } = new();
        [BindProperty] public CreateSessionInput CreateSession { get; set; } = new();
        [BindProperty] public EditSessionInput EditSession { get; set; } = new();

        public async Task OnGetAsync()
        {
            Sessions = await _context.TrainingSessions
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            Records = await _context.TrainingRecords
                .Include(r => r.Session)
                .Include(r => r.User)
                .OrderByDescending(r => r.Id)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateSessionAsync()
        {
            //Remove EditSession validation for CreateSession submit
            var removeKeys = ModelState.Keys
                .Where(k => k.StartsWith("EditSession.", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var k in removeKeys)
                ModelState.Remove(k);

            if (CreateSession.Progress == TrainingProgress.Completed)
            {
                CreateSession.Progress = TrainingProgress.NotStarted;
            }
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            if (CreateSession.EndTime <= CreateSession.StartTime)
            {
                ModelState.AddModelError("CreateSession.EndTime", "End Time must be later than Start Time.");
                await OnGetAsync();
                return Page();
            }

            var s = new TrainingSession
            {
                Title = CreateSession.Title.Trim(),
                Description = CreateSession.Description.Trim(),
                TargetAudience = CreateSession.TargetAudience?.Trim(),
                SessionType = CreateSession.SessionType,
                StartDate = CreateSession.StartDate.Date,
                StartTime = CreateSession.StartTime,
                EndTime = CreateSession.EndTime,
                Provider = CreateSession.Provider.Trim(),
                TrainingType = CreateSession.TrainingType,
                Progress = CreateSession.Progress
            };

            _context.TrainingSessions.Add(s);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditSessionAsync()
        {
            //Remove CreateSession validation for EditSession submit
            var removeKeys = ModelState.Keys
                .Where(k => k.StartsWith("CreateSession.", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var k in removeKeys)
                ModelState.Remove(k);

            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            if (EditSession.EndTime <= EditSession.StartTime)
            {
                ModelState.AddModelError("EditSession.EndTime", "End Time must be later than Start Time.");
                await OnGetAsync();
                return Page();
            }

            var session = await _context.TrainingSessions.FirstOrDefaultAsync(x => x.Id == EditSession.Id);
            if (session == null) return NotFound();

            session.Title = EditSession.Title.Trim();
            session.Description = EditSession.Description.Trim();
            session.TargetAudience = EditSession.TargetAudience?.Trim();
            session.SessionType = EditSession.SessionType;
            session.StartDate = EditSession.StartDate.Date;
            session.StartTime = EditSession.StartTime;
            session.EndTime = EditSession.EndTime;
            session.Provider = EditSession.Provider.Trim();
            session.TrainingType = EditSession.TrainingType;
            session.Progress = EditSession.Progress;
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }



        //Optional: Create a record + generate CertificationId
        private static string GenerateCertificationId(string sessionTitle, string empDigits)
        {
            // Example: HEQ-20260115-000001
            var prefix = new string(sessionTitle
                .Where(char.IsLetterOrDigit)
                .Take(3)
                .ToArray())
                .ToUpper();

            if (string.IsNullOrWhiteSpace(prefix)) prefix = "TRN";
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var emp = empDigits.PadLeft(6, '0');

            return $"{prefix}-{date}-{emp}";
        }

        public class CreateSessionInput
        {
            [StringLength(150)]
            public string Title { get; set; } = string.Empty;
                
            [StringLength(2000)]
            public string Description { get; set; } = string.Empty;

            [StringLength(300)]
            public string? TargetAudience { get; set; }

            [Required]
            public SessionType SessionType { get; set; } = SessionType.Mandatory;

            [Required, DataType(DataType.Date)]
            public DateTime StartDate { get; set; } = DateTime.Today;

            [Required]
            public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);

            [Required]
            public TimeSpan EndTime { get; set; } = new TimeSpan(17, 0, 0);

            [Required, StringLength(150)]
            public string Provider { get; set; } = "Internal";

            [Required]
            public TrainingType TrainingType { get; set; } = TrainingType.Workshop;
            [Required]
            public TrainingProgress Progress { get; set; } = TrainingProgress.NotStarted;
        }

        public class EditSessionInput : CreateSessionInput
        {
            [Required]
            public int Id { get; set; }
            [Required]
            public TrainingProgress Progress { get; set; } = TrainingProgress.NotStarted;
        }

    }
}
