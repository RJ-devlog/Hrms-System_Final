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

        public IndexModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;

            var modelPath = Path.Combine(env.ContentRootPath, "App_Data", "ml", "promotion-model.zip");
            _mlService = new PromotionPredictionService(modelPath);

            _featureBuilder = new PromotionFeatureBuilder(context);
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
        private void LoadRoleOptions()
        {
            // Uses your static catalog
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

            // Show selected employee text on the right panel
            PredictionEmployeeName =
                EmployeeOptions.FirstOrDefault(x => x.Value == SelectedUserId.Value.ToString())?.Text
                ?? "Selected Employee";

            var featureRow = _featureBuilder.BuildFeatureRow(SelectedUserId.Value);
            try
            {
                var result = _mlService.Predict(featureRow);
                var reasons = new List<string>();

                if (featureRow.AvgEvaluationScore < 3.5f)
                    reasons.Add("performance evaluation results may still need improvement");

                if (featureRow.AbsenceRate > 0.10f)
                    reasons.Add("attendance records show a higher number of absences");

                if (featureRow.LateRate > 0.10f)
                    reasons.Add("attendance records show several late arrivals");

                if (featureRow.TrainingCount < 2)
                    reasons.Add("additional training participation may be beneficial");

                if (featureRow.CertificationCount < 1)
                    reasons.Add("gaining more certifications may help strengthen readiness");

                if (featureRow.TenureMonths < 12)
                    reasons.Add("more time and experience in the current role may still be needed");

                PredictionAvailable = true;
                PredictedPromoted = result.PredictedLabel;
                PredictionProbability = result.Probability;

                if (PredictedPromoted)
                {
                    RecommendationText =
                        $"Based on the current records, this employee shows positive indicators for promotion consideration. " +
                        $"The model suggests readiness for promotion review with {PredictionProbability:P2} confidence.";

                    TempData["Success"] =
                        $"Based on the current records, this employee is recommended for promotion review. " +
                        $"Model confidence: {PredictionProbability:P2}.";
                }
                else
                {
                    var reasonText = reasons.Any()
                        ? string.Join(", ", reasons)
                        : "the current overall records may not yet fully support promotion consideration";

                    RecommendationText =
                        $"Based on the current records, this employee may not yet be ready for promotion review at this time. " +
                        $"Some areas that may still need attention include {reasonText}. " +
                        $"With continued improvement and development, the employee may become a stronger candidate in the future.";

                    TempData["Error"] =
                        $"Based on the current records, this employee is not yet recommended for promotion review. " +
                        $"Areas for improvement may include {reasonText}. ";
                }
                /* ===================== CREATE NOTIFICATION ===================== */
                _context.PromotionNotifications.Add(new PromotionNotificationModel
                {
                    EmployeeId = SelectedUserId.Value,
                    EmployeeName = PredictionEmployeeName,
                    Title = PredictedPromoted
                        ? "Promotion Prediction: HIGH"
                        : "Promotion Prediction: LOW",
                    Message = PredictedPromoted
                        ? $"Predicted promotable with {PredictionProbability:P0} confidence."
                        : $"Predicted not promotable ({PredictionProbability:P0}). Improve evaluations and attendance.",
                    StatusKey = PredictedPromoted ? "predicted_yes" : "predicted_no",
                    IsRead = false,
                    IsArchived = false,
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
                TempData["NewNotif"] = true;
                /* =============================================================== */
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

            var latestEvalMap = await _context.Evaluation
                .AsNoTracking()
                .Where(e => e.UserId.HasValue)
                .GroupBy(e => e.UserId!.Value)
                .Select(g => new
                {
                    UserId = g.Key,
                    LatestEvalAvg = g.OrderByDescending(x => x.EvaluationDate)
                        .Select(x => ((x.WorkQuality ?? 0) +
                                      (x.Productivity ?? 0) +
                                      (x.Teamwork ?? 0) +
                                      (x.Attendance ?? 0) +
                                      (x.Communication ?? 0)) / 5f)
                        .FirstOrDefault()
                })
                .ToDictionaryAsync(x => x.UserId, x => x.LatestEvalAvg);

            EmployeeOptions = employees.Select(e => new SelectListItem
            {
                Value = e.Id.ToString(),
                Text = $"{e.EmployeeNumber} - {e.FirstName} {e.LastName}"
            }).ToList();

            EmployeeRows = employees.Select(e => new EmployeeRowVM
            {
                EmployeeNumber = e.EmployeeNumber ?? "",
                FullName = $"{e.FirstName} {e.LastName}".Trim(),
                Department = string.IsNullOrWhiteSpace(e.Department) ? "—" : e.Department,
                TenureMonths = e.TenureMonths ?? CalculateMonths(e.StartDate, today),
                LatestEvalAvg = latestEvalMap.TryGetValue(e.Id, out var avg) ? avg : 0f,
                SearchText = $"{e.EmployeeNumber} {e.FirstName} {e.LastName}".Trim()
            }).ToList();
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
