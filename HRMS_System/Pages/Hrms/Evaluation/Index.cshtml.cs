using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HRMS_System.Models.Evaluation;
using System;

namespace HRMS_System.Pages.Hrms.Evaluation
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public EvaluationModel Input { get; set; } = new();

        public void OnGet()
        {
            if (Input.EvaluationDate == default)
            {
                Input.EvaluationDate = DateTime.Today;
            }
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // TODO:
            // 1. Calculate OverallRating (server-side)
            // 2. Save Evaluation entity to database
            // 3. Optionally create Notification

            TempData["Success"] = "Employee evaluation saved successfully.";
            return RedirectToPage();
        }
    }
}
