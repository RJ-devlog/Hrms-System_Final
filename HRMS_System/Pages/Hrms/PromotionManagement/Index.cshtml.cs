using HRMS_System.Data;
using HRMS_System.Models;
using HRMS_System.Models.PromotionML;
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
        /* ===================== DB + SERVICES ===================== */
        private readonly ApplicationDbContext _context;
        private readonly PromotionPredictionService _mlService;
        private readonly PromotionFeatureBuilder _featureBuilder;
        private readonly PromotionWeightedScoringService _weightedService;

        public IndexModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;

            var modelPath = Path.Combine(env.ContentRootPath, "App_Data", "ml", "promotion-model.zip");
            _mlService = new PromotionPredictionService(modelPath);

            _featureBuilder = new PromotionFeatureBuilder(context);
            _weightedService = new PromotionWeightedScoringService();
        }

        /* ===================== UI BINDINGS (INPUTS FROM FORM) ===================== */
        [BindProperty]
        public int? SelectedUserId { get; set; }

        [BindProperty]
        public string? ProposedRole { get; set; }

        /* ===================== DATA FOR UI DISPLAY ===================== */
        public List<SelectListItem> EmployeeOptions { get; set; } = new();
        public List<EmployeeRowVM> EmployeeRows { get; set; } = new();
        public List<SelectListItem> RoleOptions { get; set; } = new();

        /* ===================== PREDICTION OUTPUT (RIGHT SIDE PANEL) ===================== */
        public bool PredictionAvailable { get; set; }
        public bool PredictedPromoted { get; set; }
        public float PredictionProbability { get; set; }
        public string PredictionEmployeeName { get; set; } = "—";
        public string RecommendationText { get; set; } = "—";

        private sealed class LatestEvalInfo
        {
            public int UserId { get; set; }
            public float LatestEvalAvg { get; set; }
            public string? OverallRating { get; set; }
        }

        private void LoadRoleOptions()
        {
            RoleOptions = HRMS_System.Data.EmployeeCatalog.JobRoles;
        }

        /* ===================== GET (PAGE LOAD) ===================== */
        public async Task OnGetAsync()
        {
            await LoadEmployeesAsync();
            LoadRoleOptions();
        }

        /* ===================== POST: TRAIN MODEL ===================== */
        public async Task<IActionResult> OnPostTrainModelAsync()
        {
            await LoadEmployeesAsync();
            LoadRoleOptions();

            try
            {
                var trainingRows = _featureBuilder.BuildTrainingRows();

                if (!trainingRows.Any())
                {
                    TempData["Error"] = "No training data found. Add employee evaluation, attendance, training, and promotion history first.";
                    return Page();
                }

                if (!trainingRows.Any(x => x.WasPromoted) || !trainingRows.Any(x => !x.WasPromoted))
                {
                    TempData["Error"] = "Training data must contain both promoted and non-promoted employees.";
                    return Page();
                }

                if (trainingRows.Count < 10)
                {
                    TempData["Error"] = "Not enough training data yet. Add more employee history first.";
                    return Page();
                }

                _mlService.TrainAndSaveModel(trainingRows);

                TempData["Success"] = "Promotion model trained successfully.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return Page();
            }
        }

        /* ===================== POST: PREDICT PROMOTION ===================== */
        public async Task<IActionResult> OnPostPredictAsync()
        {
            await LoadEmployeesAsync();
            LoadRoleOptions();

            if (!SelectedUserId.HasValue)
            {
                TempData["Error"] = "Please select an employee first.";
                return Page();
            }

            PredictionEmployeeName =
                EmployeeOptions.FirstOrDefault(x => x.Value == SelectedUserId.Value.ToString())?.Text
                ?? "Selected Employee";

            try
            {
                var featureRow = _featureBuilder.BuildFeatureRow(SelectedUserId.Value);
                var weightedResult = _weightedService.Calculate(featureRow);

                PredictionAvailable = true;
                PredictedPromoted = weightedResult.IsRecommended;
                PredictionProbability = weightedResult.OverallScore / 100f;

                var reasons = new List<string>();

                if (weightedResult.AttendanceScore < 92f)
                    reasons.Add("attendance-related records may still need improvement");

                if (weightedResult.TrainingScore < 92f)
                    reasons.Add("training and certification records may still be strengthened");

                if (weightedResult.PerformanceScore < 92f)
                    reasons.Add("performance and evaluation results may still need improvement");

                if (PredictedPromoted)
                {
                    RecommendationText =
                        $"Based on the current records, this employee meets the promotion recommendation threshold. " +
                        $"Overall weighted score: {weightedResult.OverallScore:F2}%. " +
                        $"Attendance: {weightedResult.AttendanceScore:F2}%, " +
                        $"Training and Certification: {weightedResult.TrainingScore:F2}%, " +
                        $"Performance and Evaluation: {weightedResult.PerformanceScore:F2}%.";

                    TempData["Success"] =
                        $"This employee is recommended for promotion review. " +
                        $"Overall weighted score: {weightedResult.OverallScore:F2}%.";
                }
                else
                {
                    var reasonText = reasons.Any()
                        ? string.Join(", ", reasons)
                        : "some records may still need further improvement";

                    RecommendationText =
                        $"Based on the current records, this employee does not yet meet the promotion recommendation threshold of 92.00%. " +
                        $"Overall weighted score: {weightedResult.OverallScore:F2}%. " +
                        $"Attendance: {weightedResult.AttendanceScore:F2}%, " +
                        $"Training and Certification: {weightedResult.TrainingScore:F2}%, " +
                        $"Performance and Evaluation: {weightedResult.PerformanceScore:F2}%. " +
                        $"Areas that may still need attention include {reasonText}.";

                    TempData["Error"] =
                        $"This employee is not yet recommended for promotion review. " +
                        $"Overall weighted score: {weightedResult.OverallScore:F2}%.";
                }

                _context.PromotionNotifications.Add(new PromotionNotificationModel
                {
                    EmployeeId = SelectedUserId.Value,
                    EmployeeName = PredictionEmployeeName,
                    Title = PredictedPromoted
                        ? "Promotion Recommendation: QUALIFIED"
                        : "Promotion Recommendation: NOT YET QUALIFIED",
                    Message = PredictedPromoted
                        ? $"Recommended for promotion with an overall weighted score of {weightedResult.OverallScore:F2}%."
                        : $"Not yet recommended for promotion. Overall weighted score: {weightedResult.OverallScore:F2}%.",
                    StatusKey = PredictedPromoted ? "predicted_yes" : "predicted_no",
                    IsRead = false,
                    IsArchived = false,
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
                TempData["NewNotif"] = true;
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return Page();
        }

        /* ===================== POST: CREATE PROMOTION RECORD (MANUAL) ===================== */
        public async Task<IActionResult> OnPostCreatePromotionAsync()
        {
            await LoadEmployeesAsync();
            LoadRoleOptions();

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

            var emp = await _context.UserInformation
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

            _context.PromotionRecords.Add(new PromotionRecord
            {
                EmployeeId = emp.Id,
                OldRole = oldRole,
                NewRole = ProposedRole,
                PromotionDate = DateTime.Now,
                ApprovedBy = User.Identity?.Name ?? "HR",
                Notes = "Created from Promotion Management page"
            });

            _context.PromotionNotifications.Add(new PromotionNotificationModel
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
            return RedirectToPage();
        }

        /* ===================== DB LOADING (DROPDOWN + TABLE) ===================== */
        private async Task LoadEmployeesAsync()
        {
            var today = DateTime.Today;

            var employees = await _context.UserInformation
                .AsNoTracking()
                .OrderBy(e => e.EmployeeNumber)
                .ToListAsync();

            var latestEvalList = await _context.Evaluation
                .AsNoTracking()
                .Where(e => e.UserId.HasValue)
                .GroupBy(e => e.UserId!.Value)
                .Select(g => g
                    .OrderByDescending(x => x.EvaluationDate)
                    .ThenByDescending(x => x.Id)
                    .Select(x => new LatestEvalInfo
                    {
                        UserId = x.UserId!.Value,
                        LatestEvalAvg = ((x.WorkQuality ?? 0) +
                                         (x.Productivity ?? 0) +
                                         (x.Teamwork ?? 0) +
                                         (x.Attendance ?? 0) +
                                         (x.Communication ?? 0)) / 5f,
                        OverallRating = x.OverallRating
                    })
                    .FirstOrDefault()!)
                .ToListAsync();

            var latestEvalMap = latestEvalList
                .Where(x => x != null)
                .ToDictionary(x => x.UserId, x => x);

            EmployeeOptions = employees.Select(e => new SelectListItem
            {
                Value = e.Id.ToString(),
                Text = $"{e.EmployeeNumber} - {e.FirstName} {e.LastName}"
            }).ToList();

            EmployeeRows = new List<EmployeeRowVM>();

            foreach (var e in employees)
            {
                latestEvalMap.TryGetValue(e.Id, out var latestEval);

                float? latestPerformanceEvaluationScore = null;

                try
                {
                    var featureRow = _featureBuilder.BuildFeatureRow(e.Id);
                    var weightedResult = _weightedService.Calculate(featureRow);
                    latestPerformanceEvaluationScore = weightedResult.PerformanceScore;
                }
                catch
                {
                    latestPerformanceEvaluationScore = null;
                }

                EmployeeRows.Add(new EmployeeRowVM
                {
                    EmployeeNumber = e.EmployeeNumber ?? "",
                    FullName = $"{e.FirstName} {e.LastName}".Trim(),
                    TenureMonths = e.TenureMonths ?? CalculateMonths(e.StartDate, today),
                    LatestEvalAvg = latestEval?.LatestEvalAvg,
                    LatestPerformanceEvaluationScore = latestPerformanceEvaluationScore,
                    Rating = string.IsNullOrWhiteSpace(latestEval?.OverallRating) ? "—" : latestEval!.OverallRating!,
                    SearchText = $"{e.EmployeeNumber} {e.FirstName} {e.LastName}".Trim()
                });
            }
        }

        private static int CalculateMonths(DateTime startDate, DateTime endDate)
        {
            int months = (endDate.Year - startDate.Year) * 12 + endDate.Month - startDate.Month;

            if (endDate.Day < startDate.Day)
                months--;

            return Math.Max(months, 0);
        }
    }
}