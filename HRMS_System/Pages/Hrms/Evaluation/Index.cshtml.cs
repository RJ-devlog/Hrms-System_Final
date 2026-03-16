using HRMS_System.Data;
using HRMS_System.Enums;
using HRMS_System.Infrastructure;
using HRMS_System.Models;
using HRMS_System.Models.ViewModels;
using HRMS_System.Models.Evaluation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HRMS_System.Pages.Hrms.Evaluation
{
    [RoleAuthorize(AccessRole.Supervisor,AccessRole.Manager)]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public EvaluationModel Input { get; set; } = new();

        public List<PerformanceRecord> PerformanceRecords { get; set; } = new();
        public List<UserInformationModel> Employees { get; set; } = new();

        // dropdown source
        public List<SelectListItem> EmployeeOptions { get; set; } = new();

        // records tab
        public List<EvaluationModel> Records { get; set; } = new();

        // next allowed evaluation date per employee
        public Dictionary<int, DateTime> NextEvaluationDates { get; set; } = new();

        public List<ManagementSummaryRow> ManagementSummaries { get; set; } = new();

        public List<SelectListItem> MonthOptions { get; set; } = new();
        public async Task OnGetAsync()
        {
            if (Input.EvaluationDate == default)
                Input.EvaluationDate = DateTime.Today;

            await LoadEmployeesAsync();
            await LoadRecordsAsync();
            await LoadNextEvaluationDatesAsync();
            await LoadPerformanceEmployeesAsync();
            LoadMonthOptions();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadEmployeesAsync();
            await LoadRecordsAsync();
            await LoadNextEvaluationDatesAsync();
            await LoadPerformanceEmployeesAsync();
            LoadMonthOptions();

            if (!ModelState.IsValid)
            {
                TempData["OpenEvaluateModal"] = "true";
                return Page();
            }

            if (!Input.UserId.HasValue)
            {
                ModelState.AddModelError("Input.UserId", "Employee is required.");
                TempData["OpenEvaluateModal"] = "true";
                return Page();
            }

            var lastEvaluation = await _context.Evaluation
                .Where(e => e.UserId == Input.UserId.Value)
                .OrderByDescending(e => e.EvaluationDate)
                .FirstOrDefaultAsync();

            if (lastEvaluation != null)
            {
                var nextAllowedDate = lastEvaluation.EvaluationDate.AddMonths(1);

                if (DateTime.Today < nextAllowedDate.Date)
                {
                    ModelState.AddModelError("", $"This employee can be evaluated again on {nextAllowedDate:dd/MM/yyyy}.");
                    TempData["OpenEvaluateModal"] = "true";
                    return Page();
                }
            }

            var userExists = await _context.UserInformation
                .AnyAsync(u => u.Id == Input.UserId.Value);

            if (!userExists)
            {
                ModelState.AddModelError("Input.UserId", "Employee not found.");
                TempData["OpenEvaluateModal"] = "true";
                return Page();
            }

            Input.OverallRating = Math.Round(
                (Input.WorkQuality.Value +
                 Input.Productivity.Value +
                 Input.Teamwork.Value +
                 Input.Attendance.Value +
                 Input.Communication.Value) / 5m, 2
            ).ToString("0.00");

            var evaluatorEmployeeNumber = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                           ?? User.FindFirst("EmployeeNumber")?.Value;

            var evaluatorRoleClaim = User.FindFirst("UserRole")?.Value;

            AccessRole? evaluatorRole = null;
            if (!string.IsNullOrWhiteSpace(evaluatorRoleClaim) &&
                Enum.TryParse<AccessRole>(evaluatorRoleClaim, out var parsedRole))
            {
                evaluatorRole = parsedRole;
            }

            if (evaluatorRole != AccessRole.Supervisor && evaluatorRole != AccessRole.Manager)
            {
                ModelState.AddModelError("", "Only Supervisor or Manager can evaluate employees.");
                TempData["OpenEvaluateModal"] = "true";
                return Page();
            }

            var evaluatorUser = await _context.UserInformation
                .Include(u => u.Login)
                .FirstOrDefaultAsync(u => u.EmployeeNumber == evaluatorEmployeeNumber);

            if (evaluatorUser != null)
            {
                Input.EvaluatorUserId = evaluatorUser.Id;
                Input.EvaluatorName = $"{evaluatorUser.FirstName} {evaluatorUser.LastName}";
                Input.EvaluatorRole = evaluatorRole;
            }


            // Period is now stored as integer month (1-12)
            if (Input.Period < 1 || Input.Period > 12)
                Input.Period = DateTime.Today.Month;

            if (Input.EvaluationDate == default)
                Input.EvaluationDate = DateTime.Today;

            if (Input.EvaluationCurrentYear == default)
                Input.EvaluationCurrentYear = DateTime.Today.Year;

            _context.Evaluation.Add(Input);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Employee evaluation saved successfully.";
            return RedirectToPage();
        }
        public async Task<IActionResult> OnGetPerformanceDetailsAsync(int id)
        {
            var employee = await _context.UserInformation
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (employee == null)
                return NotFound();

            var evaluations = await _context.Evaluation
                .AsNoTracking()
                .Where(e => e.UserId == id)
                .OrderByDescending(e => e.EvaluationDate)
                .ToListAsync();

            decimal latestScore = 0;

            if (evaluations.Any())
            {
                var latest = evaluations.First();

                latestScore =
                    (
                        (latest.WorkQuality ?? 0) +
                        (latest.Productivity ?? 0) +
                        (latest.Teamwork ?? 0) +
                        (latest.Attendance ?? 0) +
                        (latest.Communication ?? 0)
                    ) / 25m * 100m;
            }

            return new JsonResult(new
            {
                id = employee.EmployeeNumber,
                name = $"{employee.FirstName} {employee.LastName}",
                jobRole = employee.JobRole ?? "-",
                department = employee.Department ?? "-",
                dateHired = employee.StartDate.ToString("dd/MM/yyyy"),
                overallScore = latestScore == 0 ? "-" : $"{latestScore:0.##}%"
            });
        }
        public async Task<IActionResult> OnGetPerformanceHistoryAsync(int id)
        {
            var evaluations = await _context.Evaluation
                .AsNoTracking()
                .Where(e => e.UserId == id)
                .ToListAsync();

            var history = evaluations
                .GroupBy(e => e.EvaluationCurrentYear)
                .Select(g =>
                {
                    var avgRaw = g.Average(x =>
                        ((x.WorkQuality ?? 0) +
                         (x.Productivity ?? 0) +
                         (x.Teamwork ?? 0) +
                         (x.Attendance ?? 0) +
                         (x.Communication ?? 0)) / 25m * 100m
                    );

                    string trend = avgRaw == 50m
                        ? "Stable"
                        : avgRaw < 50m
                            ? "Decline"
                            : "Improving";

                    return new
                    {
                        year = g.Key,
                        score = Math.Round(avgRaw, 2),
                        trend = trend
                    };
                })
                .OrderBy(x => x.year)
                .ToList();

            return new JsonResult(history);
        }
        private async Task LoadNextEvaluationDatesAsync()
        {
            var latestEvaluations = await _context.Evaluation
                .AsNoTracking()
                .Where(e => e.UserId.HasValue)
                .GroupBy(e => e.UserId!.Value)
                .Select(g => new
                {
                    UserId = g.Key,
                    LastDate = g.Max(x => x.EvaluationDate)
                })
                .ToListAsync();

            NextEvaluationDates = latestEvaluations.ToDictionary(
                x => x.UserId,
                x => x.LastDate.AddMonths(1)
            );
        }

        private async Task LoadPerformanceEmployeesAsync()
        {
            Employees = await _context.UserInformation
                .AsNoTracking()
                .OrderBy(e => e.EmployeeNumber)
                .ToListAsync();
        }

        private async Task LoadEmployeesAsync()
        {
            EmployeeOptions = await _context.UserInformation
                .AsNoTracking()
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = $"{u.EmployeeNumber} - {u.FirstName} {u.LastName}"
                })
                .ToListAsync();
        }

        private async Task LoadRecordsAsync()
        {
            Records = await _context.Evaluation
                .AsNoTracking()
                .Include(e => e.User)
                .OrderByDescending(e => e.EvaluationCurrentYear)
                .ThenByDescending(e => e.EvaluationDate)
                .ThenByDescending(e => e.Id)
                .Take(200)
                .ToListAsync();
        }

        private void LoadMonthOptions()
        {
            MonthOptions = Enumerable.Range(1, 12)
                .Select(m => new SelectListItem
                {
                    Value = m.ToString(), // numeric month value for binding to int Period
                    Text = new DateTime(2000, m, 1).ToString("MMMM")
                }).ToList();
        }
        private static string GetMonthName(int month)
        {
            return month >= 1 && month <= 12
                ? new DateTime(2000, month, 1).ToString("MMMM")
                : "-";
        }
        public async Task<IActionResult> OnGetEvalDetailsAsync(int id)
        {
            var eval = await _context.Evaluation
                .AsNoTracking()
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eval == null)
                return NotFound();

            return new JsonResult(new
            {
                id = eval.Id,
                employee = eval.User != null
                    ? $"{eval.User.EmployeeNumber} - {eval.User.FirstName} {eval.User.LastName}"
                    : $"User #{eval.UserId}",
                period = GetMonthName(eval.Period),
                date = eval.EvaluationDate.ToString("MMMM d, yyyy"),

                workQuality = eval.WorkQuality,
                productivity = eval.Productivity,
                teamwork = eval.Teamwork,
                attendance = eval.Attendance,
                communication = eval.Communication,

                strengths = eval.Strengths ?? "",
                improvements = eval.Improvements ?? "",
                comments = eval.Comments ?? "",
                overall = string.IsNullOrWhiteSpace(eval.OverallRating) ? "" : eval.OverallRating,

                evaluatedBy = eval.EvaluatorRole.HasValue ? eval.EvaluatorRole.Value.ToString() : "-",
                evaluatedDate = eval.EvaluationDate.ToString("MM/dd/yyyy")
            });
        }
    }
}