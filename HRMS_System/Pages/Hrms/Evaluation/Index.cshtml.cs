using HRMS_System.Data;
using HRMS_System.Models;
using HRMS_System.Models.Evaluation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HRMS_System.Infrastructure;

namespace HRMS_System.Pages.Hrms.Evaluation
{
    [RoleAuthorize(UserRole.Supervisor)]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public EvaluationModel Input { get; set; } = new();

        // dropdown source
        public List<SelectListItem> EmployeeOptions { get; set; } = new();

        // records tab
        public List<EvaluationModel> Records { get; set; } = new();

        public async Task OnGetAsync()
        {
            if (Input.EvaluationDate == default)
                Input.EvaluationDate = DateTime.Today;

            await LoadEmployeesAsync();
            await LoadRecordsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadEmployeesAsync(); // important if validation fails
            await LoadRecordsAsync();


            if (!ModelState.IsValid)
                return Page();

            if (!Input.WorkQuality.HasValue ||!Input.Productivity.HasValue || !Input.Teamwork.HasValue ||!Input.Attendance.HasValue || !Input.Communication.HasValue)
            {
                ModelState.AddModelError("", "Please complete all ratings before saving.");
                return Page();
            }

            // COMPUTE OVERALL
            Input.OverallRating = Math.Round(
                (Input.WorkQuality.Value +
                 Input.Productivity.Value +
                 Input.Teamwork.Value +
                 Input.Attendance.Value +
                 Input.Communication.Value) / 5m, 2).ToString("0.00");

            // validate employee exists
            var userExists = await _context.UserInformation
                .AnyAsync(u => u.id == Input.UserId);

            // ✅ compute overall
            Input.OverallRating = Math.Round( (decimal)(Input.WorkQuality + Input.Productivity + Input.Teamwork + Input.Attendance + Input.Communication) / 5m, 2).ToString("0.00");

/*            // Optional: validate employee exists
            var userExists = await _context.UserInformation.AnyAsync(u => u.id == Input.UserId);*/
            if (!userExists)
            {
                ModelState.AddModelError("Input.UserId", "Employee not found.");
                return Page();
            }

            _context.Evaluations.Add(Input);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Employee evaluation saved successfully.";
            return RedirectToPage(); // refresh
        }

        private async Task LoadEmployeesAsync()
        {
            EmployeeOptions = await _context.UserInformation
                .AsNoTracking()
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Select(u => new SelectListItem
                {
                    Value = u.id.ToString(),
                    Text = $"{u.EmployeeNumber} - {u.FirstName} {u.LastName}"
                })
                .ToListAsync();
        }

        private async Task LoadRecordsAsync()
        {
            Records = await _context.Evaluations
                .AsNoTracking()
                .Include(e => e.User)
                .OrderByDescending(e => e.EvaluationDate)
                .OrderByDescending(e => e.EvaluationCUrrentYear)
                .ThenByDescending(e => e.Id)
                .Take(200) // limit for UI
                .ToListAsync();
        }
        public async Task<IActionResult> OnGetEvalDetailsAsync(int id)
        {
            var eval = await _context.Evaluations
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
                period = eval.Period ?? "",
                date = eval.EvaluationDate.ToString("MMMM d, yyyy"),

                workQuality = eval.WorkQuality,
                productivity = eval.Productivity,
                teamwork = eval.Teamwork,
                attendance = eval.Attendance,
                communication = eval.Communication,

                strengths = eval.Strengths ?? "",
                improvements = eval.Improvements ?? "",
                comments = eval.Comments ?? "",
                overall = string.IsNullOrWhiteSpace(eval.OverallRating) ? "" : eval.OverallRating
            });
        }

    }
}
