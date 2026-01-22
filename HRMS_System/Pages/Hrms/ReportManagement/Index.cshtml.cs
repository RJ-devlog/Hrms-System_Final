using HRMS_System.Data;
using HRMS_System.Models;
using HRMS_System.Models.Evaluation;
using HRMS_System.Models.Reports;
using HRMS_System.Models.Training;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HRMS_System.Pages.Hrms.ReportManagement
{
    public class IndexModel : PageModel
    {

        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }
        [BindProperty(SupportsGet = true)]
        public ReportFilterModel Filter { get; set; } = new();
        // Dropdown sources
        public List<SelectListItem> DepartmentOptions { get; set; } = new();

        // UI Rows
        public List<PerformanceRowVM> PerformanceRows { get; set; } = new();
        public List<AttendanceRowVM> AttendanceRows { get; set; } = new();
        public List<TrainingRowVM> TrainingRows { get; set; } = new();

        // Summaries
        public PerformanceSummaryVM PerformanceSummary { get; set; } = new();
        public AttendanceSummaryVM AttendanceSummary { get; set; } = new();
        public TrainingSummaryVM TrainingSummary { get; set; } = new();

        public async Task OnGetAsync()
        {
            // dropdown for attendance filter
            DepartmentOptions = EmployeeCatalog.Departments;

            if (Filter.ActiveTab == "attendance")
                await LoadAttendanceAsync();
            else if (Filter.ActiveTab == "training")
                await LoadTrainingAsync();
            else
                await LoadPerformanceAsync();
        }

        private async Task LoadPerformanceAsync()
        {
            // NOTE: update DbSet name if yours is different
            IQueryable<EvaluationModel> q = _context.Set<EvaluationModel>();

            if (!string.IsNullOrWhiteSpace(Filter.Period))
                q = q.Where(x => x.Period == Filter.Period);

            if (Filter.FromDate.HasValue)
                q = q.Where(x => x.EvaluationDate >= Filter.FromDate.Value.Date);

            if (Filter.ToDate.HasValue)
                q = q.Where(x => x.EvaluationDate <= Filter.ToDate.Value.Date);

            // If you have navigation to employee, join that table here.
            // For now, use UserId and show as "User #"
            var rows = await q
                .OrderByDescending(x => x.EvaluationDate)
                .ToListAsync();

            PerformanceRows = rows.Select(x =>
            {
                var avg = Avg5(x.WorkQuality, x.Productivity, x.Teamwork, x.Attendance, x.Communication);
                return new PerformanceRowVM
                {
                    EvaluationDate = x.EvaluationDate,
                    EmployeeDisplay = x.UserId.HasValue ? $"User #{x.UserId}" : "Unknown",
                    Period = x.Period ?? "",
                    WorkQuality = x.WorkQuality,
                    Productivity = x.Productivity,
                    Teamwork = x.Teamwork,
                    Attendance = x.Attendance,
                    Communication = x.Communication,
                    AvgScore = avg,
                    OverallRating = x.OverallRating ?? "-"
                };
            }).ToList();

            PerformanceSummary.TotalEvaluations = PerformanceRows.Count;
            PerformanceSummary.AvgScore = PerformanceRows.Count == 0 ? 0 : PerformanceRows.Average(r => r.AvgScore);
            PerformanceSummary.TopPeriod = PerformanceRows
                .GroupBy(r => r.Period)
                .OrderByDescending(g => g.Average(x => x.AvgScore))
                .Select(g => g.Key)
                .FirstOrDefault() ?? "-";
        }

        private async Task LoadAttendanceAsync()
        {
            IQueryable<AttendanceTrackingModel> q = _context.Set<AttendanceTrackingModel>()
                .Include(x => x.User);

            if (Filter.FromDate.HasValue)
                q = q.Where(x => x.AttendanceDate >= Filter.FromDate.Value.Date);

            if (Filter.ToDate.HasValue)
                q = q.Where(x => x.AttendanceDate <= Filter.ToDate.Value.Date);

            if (!string.IsNullOrWhiteSpace(Filter.Department))
                q = q.Where(x => x.User.Department == Filter.Department);

            if (!string.IsNullOrWhiteSpace(Filter.Search))
            {
                q = q.Where(x =>
                    x.User.EmployeeNumber.Contains(Filter.Search) ||
                    x.User.FirstName!.Contains(Filter.Search) ||
                    x.User.LastName!.Contains(Filter.Search) ||
                    x.User.Email!.Contains(Filter.Search));
            }

            var rows = await q
                .OrderByDescending(x => x.AttendanceDate)
                .ToListAsync();

            AttendanceRows = rows.Select(x => new AttendanceRowVM
            {
                AttendanceDate = x.AttendanceDate,
                EmployeeDisplay = $"{x.User.FirstName} {x.User.LastName} ({x.User.EmployeeNumberDigits})",
                AttendanceStatus = x.AttendanceStatus ?? "-",
                TimeInDisplay = x.TimeIn?.ToString("HH:mm") ?? "-",
                TimeOutDisplay = x.TimeOut?.ToString("HH:mm") ?? "-"
            }).ToList();

            AttendanceSummary.TotalRecords = AttendanceRows.Count;
            AttendanceSummary.PresentCount = AttendanceRows.Count(r => (r.AttendanceStatus ?? "").Equals("Present", StringComparison.OrdinalIgnoreCase));
            AttendanceSummary.AbsentCount = AttendanceRows.Count(r => (r.AttendanceStatus ?? "").Equals("Absent", StringComparison.OrdinalIgnoreCase));
        }

        private async Task LoadTrainingAsync()
        {
            IQueryable<TrainingRecord> q = _context.Set<TrainingRecord>()
                .Include(r => r.User)
                .Include(r => r.Session);

            if (Filter.FromDate.HasValue)
                q = q.Where(r => r.Session!.StartDate >= Filter.FromDate.Value.Date);

            if (Filter.ToDate.HasValue)
                q = q.Where(r => r.Session!.StartDate <= Filter.ToDate.Value.Date);

            if (!string.IsNullOrWhiteSpace(Filter.Provider))
                q = q.Where(r => r.Provider.Contains(Filter.Provider));

            if (!string.IsNullOrWhiteSpace(Filter.Search))
            {
                q = q.Where(r =>
                    r.Session!.Title!.Contains(Filter.Search) ||
                    r.User.FirstName!.Contains(Filter.Search) ||
                    r.User.LastName!.Contains(Filter.Search));
            }

            var rows = await q
                .OrderByDescending(r => r.Session!.StartDate)
                .ToListAsync();

            TrainingRows = rows.Select(r => new TrainingRowVM
            {
                SessionDate = r.Session!.StartDate,
                Title = r.Session.Title ?? "-",
                Provider = r.Provider,
                TrainingType = r.Session.TrainingType.ToString(),
                EmployeeDisplay = $"{r.User.FirstName} {r.User.LastName} ({r.User.EmployeeNumberDigits})",
                Progress = r.Progress.ToString(),
                DateCompletedDisplay = r.DateCompleted?.ToString("yyyy-MM-dd") ?? "-",
                CertificationId = r.CertificationId
            }).ToList();

            TrainingSummary.TotalRecords = TrainingRows.Count;
            TrainingSummary.TotalSessions = rows.Select(x => x.TrainingSessionId).Distinct().Count();
            TrainingSummary.CompletedCount = rows.Count(x => x.Progress == TrainingProgress.Completed);
        }

        private static double Avg5(params int?[] vals)
        {
            var list = vals.Where(v => v.HasValue).Select(v => (double)v!.Value).ToList();
            return list.Count == 0 ? 0 : list.Average();
        }





























        // ----- ViewModels -----
        public class PerformanceRowVM
        {
            public DateTime EvaluationDate { get; set; }
            public string EmployeeDisplay { get; set; } = "";
            public string Period { get; set; } = "";
            public int? WorkQuality { get; set; }
            public int? Productivity { get; set; }
            public int? Teamwork { get; set; }
            public int? Attendance { get; set; }
            public int? Communication { get; set; }
            public double AvgScore { get; set; }
            public string OverallRating { get; set; } = "-";
        }

        public class AttendanceRowVM
        {
            public DateTime AttendanceDate { get; set; }
            public string EmployeeDisplay { get; set; } = "";
            public string? AttendanceStatus { get; set; }
            public string TimeInDisplay { get; set; } = "-";
            public string TimeOutDisplay { get; set; } = "-";
        }

        public class TrainingRowVM
        {
            public DateTime SessionDate { get; set; }
            public string Title { get; set; } = "-";
            public string Provider { get; set; } = "-";
            public string TrainingType { get; set; } = "-";
            public string EmployeeDisplay { get; set; } = "";
            public string Progress { get; set; } = "-";
            public string DateCompletedDisplay { get; set; } = "-";
            public string? CertificationId { get; set; }
        }

        public class PerformanceSummaryVM
        {
            public int TotalEvaluations { get; set; }
            public double AvgScore { get; set; }
            public string TopPeriod { get; set; } = "-";
        }

        public class AttendanceSummaryVM
        {
            public int TotalRecords { get; set; }
            public int PresentCount { get; set; }
            public int AbsentCount { get; set; }
        }

        public class TrainingSummaryVM
        {
            public int TotalSessions { get; set; }
            public int TotalRecords { get; set; }
            public int CompletedCount { get; set; }
        }
    }
}
