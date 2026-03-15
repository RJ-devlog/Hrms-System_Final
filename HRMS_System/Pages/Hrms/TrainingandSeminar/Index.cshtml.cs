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

        public List<TrainingRecord> Records { get; set; } = new();
        public List<TrainingSession> Sessions { get; set; } = new();

        [BindProperty]
        public TrainingSession CreateSession { get; set; } = new();

        [BindProperty]
        public TrainingSession EditSession { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Tab { get; set; } = "tab-records";

        public bool ReopenCreateModal { get; set; }
        public bool ReopenEditModal { get; set; }

        public async Task OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(Tab))
                Tab = "tab-records";

            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostCreateSessionAsync()
        {
            Tab = "tab-sessions";

            // Normalize values first
            CreateSession.Title = (CreateSession.Title ?? string.Empty).Trim();
            CreateSession.Description = (CreateSession.Description ?? string.Empty).Trim();
            CreateSession.TargetAudience = (CreateSession.TargetAudience ?? string.Empty).Trim();
            CreateSession.Provider = string.IsNullOrWhiteSpace(CreateSession.Provider)
                ? "Internal"
                : CreateSession.Provider.Trim();

            // IMPORTANT: clear all automatic validation state,
            // then validate ONLY the posted form model
            ModelState.Clear();
            TryValidateModel(CreateSession, nameof(CreateSession));
            ValidateSession(CreateSession, nameof(CreateSession));

            if (!ModelState.IsValid)
            {
                ReopenCreateModal = true;
                await LoadDataAsync();
                return Page();
            }

            _context.TrainingSessions.Add(CreateSession);
            await _context.SaveChangesAsync();

            await SyncTrainingRecordsForSessionAsync(CreateSession);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { tab = "tab-sessions" });
        }

        public async Task<IActionResult> OnPostEditSessionAsync()
        {
            Tab = "tab-sessions";

            // Normalize values first
            EditSession.Title = (EditSession.Title ?? string.Empty).Trim();
            EditSession.Description = (EditSession.Description ?? string.Empty).Trim();
            EditSession.TargetAudience = (EditSession.TargetAudience ?? string.Empty).Trim();
            EditSession.Provider = string.IsNullOrWhiteSpace(EditSession.Provider)
                ? "Internal"
                : EditSession.Provider.Trim();

            // IMPORTANT: clear all automatic validation state,
            // then validate ONLY the posted form model
            ModelState.Clear();
            TryValidateModel(EditSession, nameof(EditSession));
            ValidateSession(EditSession, nameof(EditSession));

            if (!ModelState.IsValid)
            {
                ReopenEditModal = true;
                await LoadDataAsync();
                return Page();
            }

            var session = await _context.TrainingSessions
                .FirstOrDefaultAsync(s => s.Id == EditSession.Id);

            if (session == null)
            {
                ModelState.AddModelError(string.Empty, "Session not found.");
                ReopenEditModal = true;
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

            await SyncTrainingRecordsForSessionAsync(session);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { tab = "tab-sessions" });
        }

        private void ValidateSession(TrainingSession session, string prefix)
        {
            if (session.StartDate == default)
            {
                ModelState.AddModelError($"{prefix}.StartDate", "Start date is required.");
            }

            if (session.EndTime <= session.StartTime)
            {
                ModelState.AddModelError($"{prefix}.EndTime", "End time must be later than start time.");
            }

            if (session.Progress == TrainingProgress.Completed && !HasSessionEnded(session))
            {
                ModelState.AddModelError(
                    $"{prefix}.Progress",
                    "Completed can only be selected after the training end date and time has passed."
                );
            }
        }

        private static bool HasSessionEnded(TrainingSession session)
        {
            var endDateTime = session.StartDate.Date.Add(session.EndTime);
            return DateTime.Now >= endDateTime;
        }

        private async Task SyncTrainingRecordsForSessionAsync(TrainingSession session)
        {
            var records = await _context.TrainingRecords
                .Where(r => r.TrainingSessionId == session.Id)
                .ToListAsync();

            foreach (var record in records)
            {
                record.Progress = session.Progress;

                if (session.Progress == TrainingProgress.Completed)
                {
                    record.DateCompleted = session.EndDateTime;
                    record.CertificationId = GenerateCertificationId(record, session);

                    if (string.IsNullOrWhiteSpace(record.Duration) && session.EndTime > session.StartTime)
                    {
                        record.Duration = FormatDuration(session.EndTime - session.StartTime);
                    }
                }
                else
                {
                    record.DateCompleted = null;
                    record.CertificationId = null;
                }
            }
        }

        private static string GenerateCertificationId(TrainingRecord record, TrainingSession session)
        {
            return $"CERT-S{session.Id:D4}-U{record.UserId:D4}-R{record.Id:D6}";
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalMinutes <= 0)
                return "0m";

            if (duration.Hours > 0 && duration.Minutes > 0)
                return $"{duration.Hours}h {duration.Minutes}m";

            if (duration.Hours > 0)
                return $"{duration.Hours}h";

            return $"{duration.Minutes}m";
        }

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