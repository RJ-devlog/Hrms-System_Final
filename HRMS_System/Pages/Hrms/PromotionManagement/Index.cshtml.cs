using HRMS_System.Data;
using HRMS_System.Models.PromotionML;
using HRMS_System.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HRMS_System.Pages.Hrms.PromotionManagement
{
    public class IndexModel : PageModel
    {
        /* ===================== DB + SERVICES ===================== */
        private readonly ApplicationDbContext _context;
        private readonly PromotionPredictionService _mlService;

        public IndexModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;

            // App_Data/ml/promotion-model.zip (absolute path)
            var modelPath = Path.Combine(env.ContentRootPath, "App_Data", "ml", "promotion-model.zip");
            _mlService = new PromotionPredictionService(modelPath);
        }

        /* ===================== UI BINDINGS (INPUTS FROM FORM) ===================== */
        [BindProperty]
        public int? SelectedUserId { get; set; }

        [BindProperty]
        public string? ProposedRole { get; set; }

        /* ===================== DATA FOR UI DISPLAY ===================== */
        public List<SelectListItem> EmployeeOptions { get; set; } = new();
        public List<EmployeeRowVM> EmployeeRows { get; set; } = new();

        /* ===================== PREDICTION OUTPUT (RIGHT SIDE PANEL) ===================== */
        public bool PredictionAvailable { get; set; }
        public bool PredictedPromoted { get; set; }
        public float PredictionProbability { get; set; }
        public string PredictionEmployeeName { get; set; } = "—";
        public string RecommendationText { get; set; } = "—";

        /* ===================== GET (PAGE LOAD) ===================== */
        public async Task OnGetAsync()
        {
            await LoadEmployeesAsync();
        }

        /* ===================== POST: TRAIN MODEL ===================== */
        public async Task<IActionResult> OnPostTrainModelAsync()
        {
            await LoadEmployeesAsync();

            var trainingRows = BuildTrainingRowsFromDb(); // still mock (replace later)

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

            if (!SelectedUserId.HasValue)
            {
                TempData["Error"] = "Please select an employee first.";
                return Page();
            }

            // Show selected employee text on the right panel
            PredictionEmployeeName =
                EmployeeOptions.FirstOrDefault(x => x.Value == SelectedUserId.Value.ToString())?.Text
                ?? "Selected Employee";

            var featureRow = BuildFeatureRowForUser(SelectedUserId.Value); // still mock (replace later)

            try
            {
                var result = _mlService.Predict(featureRow);

                PredictionAvailable = true;
                PredictedPromoted = result.PredictedLabel;
                PredictionProbability = result.Probability;

                RecommendationText = PredictedPromoted
                    ? "Recommended for promotion review (strong indicators: performance, attendance, growth)."
                    : "Not yet recommended. Improve evaluation, reduce absences/lates, and attend more trainings/certifications.";
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

            if (!SelectedUserId.HasValue)
            {
                TempData["Error"] = "Select an employee first.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(ProposedRole))
            {
                TempData["Error"] = "Please enter the proposed role.";
                return Page();
            }

            // TODO: Save promotion record into database here (PromotionRecord table)
            TempData["Success"] = "Promotion record created (placeholder).";
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
                    e.id,
                    e.EmployeeNumber,
                    e.FirstName,
                    e.LastName,
                    DepartmentName = string.IsNullOrWhiteSpace(e.Department) ? "—" : e.Department
                })
                .ToListAsync();

            EmployeeOptions = employees.Select(e => new SelectListItem
            {
                Value = e.id.ToString(),
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



        /* ===================== MOCK DATA (REPLACE WITH DB QUERIES LATER) ===================== */
        private List<PromotionTrainingRow> BuildTrainingRowsFromDb()
        {
            return new List<PromotionTrainingRow>
            {
                new PromotionTrainingRow { TenureMonths=18, AbsenceRate=0.02f, LateRate=0.05f, TrainingCount=4, CertificationCount=2, AvgEvaluationScore=4.3f, WasPromoted=true },
                new PromotionTrainingRow { TenureMonths=8,  AbsenceRate=0.12f, LateRate=0.20f, TrainingCount=1, CertificationCount=0, AvgEvaluationScore=3.0f, WasPromoted=false }
            };
        }

        private PromotionTrainingRow BuildFeatureRowForUser(int userId)
        {
            return new PromotionTrainingRow
            {
                TenureMonths = 18,
                AbsenceRate = 0.02f,
                LateRate = 0.05f,
                TrainingCount = 4,
                CertificationCount = 2,
                AvgEvaluationScore = 4.3f
            };
        }

        /* ===================== VIEWMODEL FOR TABLE ===================== */
        public class EmployeeRowVM
        {
            public string EmployeeNumber { get; set; } = "";
            public string FullName { get; set; } = "";
            public string? Department { get; set; }
            public int TenureMonths { get; set; }
            public float LatestEvalAvg { get; set; }
            public string SearchText => $"{EmployeeNumber} {FullName} {Department}";
        }
    }
}
