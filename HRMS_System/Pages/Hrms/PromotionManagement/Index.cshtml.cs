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
            var trainingRows = _featureBuilder.BuildTrainingRows();

            if (!trainingRows.Any())
            {
                TempData["Error"] = "No training data found. Add promotion history first.";
                return Page();
            }

            _mlService.TrainAndSaveModel(trainingRows);

            TempData["Success"] = "Model trained successfully!";
            return RedirectToPage();
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

                PredictionAvailable = true;
                PredictedPromoted = result.PredictedLabel;
                PredictionProbability = result.Probability;

                RecommendationText = PredictedPromoted
                    ? "Recommended for promotion review (strong indicators: performance, attendance, growth)."
                    : "Not yet recommended. Improve evaluation, reduce absences/lates, and attend more trainings/certifications.";

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

            // ✅ APPLY NEW ROLE (change 'JobRole' to your real column name if different)
            emp.JobRole = ProposedRole;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Employee role updated to: {ProposedRole}.";
            return RedirectToPage();
        }


        /* ===================== DB LOADING (DROPDOWN + TABLE) ===================== */
        private async Task LoadEmployeesAsync()
        {
            var employees = await _context.UserInformation
                .AsNoTracking()
                .OrderBy(e => e.EmployeeNumber)
                .Select(e => new
                {
                    e.Id,
                    e.EmployeeNumber,
                    e.FirstName,
                    e.LastName,
                    DepartmentName = string.IsNullOrWhiteSpace(e.Department) ? "—" : e.Department
                })
                .ToListAsync();

            EmployeeOptions = employees.Select(e => new SelectListItem
            {
                Value = e.Id.ToString(),
                Text = $"{e.EmployeeNumber} - {e.FirstName} {e.LastName}"
            }).ToList();

            EmployeeRows = employees.Select(e => new EmployeeRowVM
            {
                EmployeeNumber = e.EmployeeNumber ?? "",
                FullName = $"{e.FirstName} {e.LastName}",
                Department = e.DepartmentName,
                TenureMonths = 0,   // (optional, compute later if you have StartDate)
                LatestEvalAvg = 0f  // (optional, compute later)
            }).ToList();
        }

    }
}
