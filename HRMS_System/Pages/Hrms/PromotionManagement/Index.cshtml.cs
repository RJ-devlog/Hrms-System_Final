using HRMS_System.Data;
using HRMS_System.Models;
using HRMS_System.Models.Evaluation;
using HRMS_System.Models.ViewModels;
using HRMS_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HRMS_System.Pages.Hrms.PromotionManagement
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly PromotionFeatureBuilder _featureBuilder;
        private readonly PromotionWeightedScoringService _weightedService;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
            _featureBuilder = new PromotionFeatureBuilder(context);
            _weightedService = new PromotionWeightedScoringService();
        }

        [BindProperty(SupportsGet = true)]
        public int? SelectedUserId { get; set; }

        [BindProperty]
        public string? ProposedRole { get; set; }

        public List<SelectListItem> EmployeeOptions { get; set; } = new();
        public List<EmployeeRowVM> EmployeeRows { get; set; } = new();
        public List<SelectListItem> RoleOptions { get; set; } = new();

        public string SelectedEmployeeName { get; set; } = "—";
        public string SelectedCategoryName { get; set; } = "—";

        private sealed class PromotionSnapshot
        {
            public int EmployeeId { get; set; }
            public string EmployeeDisplay { get; set; } = "";
            public string CategoryName { get; set; } = "—";
            public float AttendancePercent { get; set; }
            public float? LatestEvalAvg { get; set; }
            public float? PerformancePercent { get; set; }
            public int CertificationBonus { get; set; }
            public float PromotionChance { get; set; }
        }
        private sealed class LatestEvalInfo
        {
            public int UserId { get; set; }
            public float LatestEvalAvg { get; set; }
            public string? OverallRating { get; set; }
        }

        private sealed class AttendanceInfo
        {
            public int UserId { get; set; }
            public int TotalCount { get; set; }
            public int PresentCount { get; set; }
            public float AttendancePercent { get; set; }
        }

        private sealed class TrainingInfo
        {
            public int UserId { get; set; }
            public int TotalPoints { get; set; }
        }

        public async Task OnGetAsync()
        {
            LoadRoleOptions();
            await LoadEmployeesAsync();
        }

        public async Task<IActionResult> OnPostCreatePromotionAsync()
        {
            LoadRoleOptions();
            await LoadEmployeesAsync();

            if (!SelectedUserId.HasValue)
            {
                TempData["Error"] = "Select an employee first.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(ProposedRole))
            {
                TempData["Error"] = "Please select the proposed role.";
                return Page();
            }

            var emp = await _context.Set<UserInformationModel>()
                .FirstOrDefaultAsync(x => x.Id == SelectedUserId.Value);

            if (emp == null)
            {
                TempData["Error"] = "Employee not found.";
                return Page();
            }

            if (string.Equals(emp.JobRole, ProposedRole, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "The proposed role is the same as the employee's current role.";
                return Page();
            }

            var oldRole = emp.JobRole ?? "—";
            var employeeDisplay = $"{emp.EmployeeNumber} - {emp.FirstName} {emp.LastName}".Trim();

            emp.JobRole = ProposedRole;

            _context.Set<PromotionRecord>().Add(new PromotionRecord
            {
                EmployeeId = emp.Id,
                OldRole = oldRole,
                NewRole = ProposedRole,
                PromotionDate = DateTime.Now,
                ApprovedBy = User.Identity?.Name ?? "HR",
                Notes = "Created from Promotion Management page"
            });

            _context.Set<PromotionNotificationModel>().Add(new PromotionNotificationModel
            {
                EmployeeId = emp.Id,
                EmployeeName = employeeDisplay,
                Title = "Promotion Record Created",
                Message = $"Promotion record created: {oldRole} -> {ProposedRole}.",
                StatusKey = "created",
                IsRead = false,
                IsArchived = false,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Employee role updated to: {ProposedRole}. Promotion record saved.";
            return RedirectToPage(new { SelectedUserId = SelectedUserId });
        }

        private void LoadRoleOptions()
        {
            RoleOptions = EmployeeCatalog.JobRoles;
        }
        private async Task<PromotionSnapshot?> BuildPromotionSnapshotAsync(int userId)
        {
            var employee = await _context.Set<UserInformationModel>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (employee == null)
                return null;

            var attendanceRows = await _context.Set<AttendanceTrackingModel>()
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .ToListAsync();

            var totalAttendance = attendanceRows.Count;

            var attendedCount = attendanceRows.Count(x =>
                !string.IsNullOrWhiteSpace(x.AttendanceStatus) &&
                !x.AttendanceStatus.Equals("Absent", StringComparison.OrdinalIgnoreCase));

            var attendancePercent = totalAttendance == 0
                ? 0f
                : (float)attendedCount / totalAttendance * 100f;

            var latestEval = await _context.Set<EvaluationModel>()
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.EvaluationDate)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            float? latestEvalAvg = null;

            if (latestEval != null)
            {
                latestEvalAvg = (
                    (latestEval.WorkQuality ?? 0) +
                    (latestEval.Productivity ?? 0) +
                    (latestEval.Teamwork ?? 0) +
                    (latestEval.Attendance ?? 0) +
                    (latestEval.Communication ?? 0)
                ) / 5f;
            }

            var totalTrainingPoints = await _context.Set<HRMS_System.Models.TrainingandSeminar>()
                .AsNoTracking()
                .Where(x => x.UserInformationId == userId)
                .SumAsync(x => (int?)x.Points) ?? 0;

            var certificationBonus = Math.Min(totalTrainingPoints, 6);

            float? performancePercent = null;

            try
            {
                var featureRow = _featureBuilder.BuildFeatureRow(userId);
                var weightedResult = _weightedService.Calculate(featureRow);
                performancePercent = weightedResult.PerformanceScore;
            }
            catch
            {
                performancePercent = null;
            }

            var promotionChance =
                (attendancePercent * 0.40f) +
                ((performancePercent ?? 0f) * 0.60f) +
                certificationBonus;

            if (promotionChance > 106f)
                promotionChance = 106f;

            return new PromotionSnapshot
            {
                EmployeeId = employee.Id,
                EmployeeDisplay = $"{employee.EmployeeNumber} - {employee.FirstName} {employee.LastName}".Trim(),
                CategoryName = employee.Category?.ToString() ?? "No Category",
                AttendancePercent = attendancePercent,
                LatestEvalAvg = latestEvalAvg,
                PerformancePercent = performancePercent,
                CertificationBonus = certificationBonus,
                PromotionChance = promotionChance
            };
        }
        public async Task<IActionResult> OnPostSelectEmployeeAsync()
        {
            if (!SelectedUserId.HasValue)
            {
                TempData["Error"] = "Please select an employee first.";
                return RedirectToPage();
            }

            var employee = await _context.Set<UserInformationModel>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == SelectedUserId.Value);

            if (employee == null)
            {
                TempData["Error"] = "Employee not found.";
                return RedirectToPage();
            }

            var attendanceRows = await _context.Set<AttendanceTrackingModel>()
                .AsNoTracking()
                .Where(x => x.UserId == employee.Id)
                .ToListAsync();

            var totalAttendance = attendanceRows.Count;

            var attendedCount = attendanceRows.Count(x =>
                !string.IsNullOrWhiteSpace(x.AttendanceStatus) &&
                !x.AttendanceStatus.Equals("Absent", StringComparison.OrdinalIgnoreCase));

            var attendancePercent = totalAttendance == 0
                ? 0f
                : (float)attendedCount / totalAttendance * 100f;

            var latestEval = await _context.Set<EvaluationModel>()
                .AsNoTracking()
                .Where(x => x.UserId == employee.Id)
                .OrderByDescending(x => x.EvaluationDate)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            float? latestEvalAvg = null;

            if (latestEval != null)
            {
                latestEvalAvg =
                    (
                        (latestEval.WorkQuality ?? 0) +
                        (latestEval.Productivity ?? 0) +
                        (latestEval.Teamwork ?? 0) +
                        (latestEval.Attendance ?? 0) +
                        (latestEval.Communication ?? 0)
                    ) / 5f;
            }

            var totalTrainingPoints = await _context.Set<HRMS_System.Models.TrainingandSeminar>()
                .AsNoTracking()
                .Where(x => x.UserInformationId == employee.Id)
                .SumAsync(x => (int?)x.Points) ?? 0;

            var certificationBonus = Math.Min(totalTrainingPoints, 6);

            float? performancePercent = null;

            try
            {
                var featureRow = _featureBuilder.BuildFeatureRow(employee.Id);
                var weightedResult = _weightedService.Calculate(featureRow);
                performancePercent = weightedResult.PerformanceScore;
            }
            catch
            {
                performancePercent = null;
            }

            var promotionChance =
                (attendancePercent * 0.40f) +
                ((performancePercent ?? 0f) * 0.60f) +
                certificationBonus;

            if (promotionChance > 106f)
                promotionChance = 106f;

            var employeeDisplay = $"{employee.EmployeeNumber} - {employee.FirstName} {employee.LastName}".Trim();
            var categoryName = employee.Category?.ToString() ?? "No Category";

            _context.Set<PromotionNotificationModel>().Add(new PromotionNotificationModel
            {
                EmployeeId = employee.Id,
                EmployeeName = employeeDisplay,
                Title = "Promotion Chance Updated",
                Message =
                    $"Promotion chance for {employeeDisplay} is {promotionChance:F2}%. " +
                    $"Attendance: {attendancePercent:F2}%, " +
                    $"Evaluation: {(latestEvalAvg.HasValue ? latestEvalAvg.Value.ToString("F2") : "—")}, " +
                    $"Performance: {(performancePercent.HasValue ? performancePercent.Value.ToString("F2") + "%" : "—")}, " +
                    $"Certification Bonus: +{certificationBonus}, " +
                    $"Category: {categoryName}.",
                StatusKey = "promotion_chance",
                IsRead = false,
                IsArchived = false,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["NewNotif"] = true;
            TempData["Success"] = $"Promotion chance notification created for {employeeDisplay}: {promotionChance:F2}%";
            TempData["Success"] = $"Promotion chance notification created for {employeeDisplay}: {promotionChance:F2}%";
            return RedirectToPage(new { SelectedUserId = employee.Id });
        }
        private async Task LoadEmployeesAsync()
        {
            var allEmployees = await _context.Set<UserInformationModel>()
                .AsNoTracking()
                .OrderBy(e => e.EmployeeNumber)
                .ToListAsync();

            EmployeeOptions = allEmployees.Select(e => new SelectListItem
            {
                Value = e.Id.ToString(),
                Text = $"{e.EmployeeNumber} - {e.FirstName} {e.LastName}"
            }).ToList();

            UserInformationModel? selectedEmployee = null;

            if (SelectedUserId.HasValue)
            {
                selectedEmployee = allEmployees.FirstOrDefault(x => x.Id == SelectedUserId.Value);

                if (selectedEmployee != null)
                {
                    SelectedEmployeeName = $"{selectedEmployee.EmployeeNumber} - {selectedEmployee.FirstName} {selectedEmployee.LastName}".Trim();
                    SelectedCategoryName = selectedEmployee.Category?.ToString() ?? "No Category";
                }
            }

            var employeesToShow = allEmployees.AsEnumerable();

            if (selectedEmployee?.Category != null)
            {
                employeesToShow = employeesToShow.Where(x => x.Category == selectedEmployee.Category);
            }

            var employeeList = employeesToShow.ToList();

            var attendanceList = await _context.Set<AttendanceTrackingModel>()
                .AsNoTracking()
                .GroupBy(a => a.UserId)
                .Select(g => new AttendanceInfo
                {
                    UserId = g.Key,
                    TotalCount = g.Count(),
                    PresentCount = g.Count(x =>
                        x.AttendanceStatus != null &&
                        (
                            x.AttendanceStatus == "Present" ||
                            x.AttendanceStatus == "On-Time"
                        )),
                    AttendancePercent = g.Count() == 0
                        ? 0
                        : (float)g.Count(x =>
                            x.AttendanceStatus != null &&
                            (
                                x.AttendanceStatus == "Present" ||
                                x.AttendanceStatus == "On-Time"
                            )) / g.Count() * 100f
                })
                .ToListAsync();

            var attendanceMap = attendanceList.ToDictionary(x => x.UserId, x => x);

            var latestEvalList = await _context.Set<EvaluationModel>()
                .AsNoTracking()
                .Where(e => e.UserId.HasValue)
                .GroupBy(e => e.UserId!.Value)
                .Select(g => g
                    .OrderByDescending(x => x.EvaluationDate)
                    .ThenByDescending(x => x.Id)
                    .Select(x => new LatestEvalInfo
                    {
                        UserId = x.UserId!.Value,
                        LatestEvalAvg = (
                            (x.WorkQuality ?? 0) +
                            (x.Productivity ?? 0) +
                            (x.Teamwork ?? 0) +
                            (x.Attendance ?? 0) +
                            (x.Communication ?? 0)
                        ) / 5f,
                        OverallRating = x.OverallRating
                    })
                    .FirstOrDefault()!)
                .ToListAsync();

            var latestEvalMap = latestEvalList
                .Where(x => x != null)
                .ToDictionary(x => x.UserId, x => x);

            var trainingList = await _context.Set<HRMS_System.Models.TrainingandSeminar>()
                .AsNoTracking()
                .GroupBy(t => t.UserInformationId)
                .Select(g => new TrainingInfo
                {
                    UserId = g.Key,
                    TotalPoints = g.Sum(x => x.Points)
                })
                .ToListAsync();

            var trainingMap = trainingList.ToDictionary(x => x.UserId, x => x);

            EmployeeRows = new List<EmployeeRowVM>();

            foreach (var e in employeeList)
            {
                attendanceMap.TryGetValue(e.Id, out var attendanceInfo);
                latestEvalMap.TryGetValue(e.Id, out var latestEval);
                trainingMap.TryGetValue(e.Id, out var trainingInfo);

                var attendancePercent = attendanceInfo?.AttendancePercent ?? 0f;
                var certificationBonus = Math.Min(trainingInfo?.TotalPoints ?? 0, 6);

                float? performancePercent = null;

                try
                {
                    var featureRow = _featureBuilder.BuildFeatureRow(e.Id);
                    var weightedResult = _weightedService.Calculate(featureRow);
                    performancePercent = weightedResult.PerformanceScore;
                }
                catch
                {
                    performancePercent = null;
                }

                var promotionChance =
                    (attendancePercent * 0.40f) +
                    ((performancePercent ?? 0f) * 0.60f) +
                    certificationBonus;

                if (promotionChance > 106f)
                    promotionChance = 106f;

                EmployeeRows.Add(new EmployeeRowVM
                {
                    EmployeeNumber = e.EmployeeNumber ?? "",
                    FullName = $"{e.FirstName} {e.LastName}".Trim(),
                    AttendancePercent = attendancePercent,
                    LatestEvalAvg = latestEval?.LatestEvalAvg,
                    PerformancePercent = performancePercent,
                    PromotionChance = promotionChance,
                    CertificationBonus = certificationBonus,
                    Rating = string.IsNullOrWhiteSpace(latestEval?.OverallRating) ? "—" : latestEval!.OverallRating!,
                    SearchText = $"{e.EmployeeNumber} {e.FirstName} {e.LastName}".Trim()
                });
            }
        }

        public class EmployeeRowVM
        {
            public string EmployeeNumber { get; set; } = "";
            public string FullName { get; set; } = "";
            public float AttendancePercent { get; set; }
            public float? LatestEvalAvg { get; set; }
            public float? PerformancePercent { get; set; }
            public float PromotionChance { get; set; }
            public int CertificationBonus { get; set; }
            public string Rating { get; set; } = "—";
            public string SearchText { get; set; } = "";
        }
    }
}