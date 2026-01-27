using HRMS_System.Data;
using HRMS_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HRMS_System.Pages.Hrms.DailyLogsAttendance
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public AttendanceInput Input { get; set; } = new();

        public string CurrentTime { get; private set; } = "";
        public string? StatusMessage { get; private set; }

        public void OnGet()
        {
            CurrentTime = DateTime.Now.ToString("h:mm tt");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            CurrentTime = DateTime.Now.ToString("h:mm tt");

            if (!ModelState.IsValid)
            {
                StatusMessage = "Please complete the required fields.";
                return Page();
            }

            // 1) Find the employee by employee number (your Input.IdNumber holds EMP-000001)
            var user = await _context.UserInformation
                .AsNoTracking()
                .Where(u => u.EmployeeNumber == Input.IdNumber)
                .Select(u => new { u.id }) // UserInformationModel PK
                .FirstOrDefaultAsync();

            if (user == null)
            {
                ModelState.AddModelError("Input.IdNumber", "Employee number not found.");
                StatusMessage = "Employee number not found.";
                return Page();
            }

            // 2) Find or create today's attendance row for that user
            var today = DateTime.Today;

            var attendance = await _context.AttendanceTrackings
                .FirstOrDefaultAsync(a => a.UserId == user.id && a.AttendanceDate == today);

            if (attendance == null)
            {
                attendance = new AttendanceTrackingModel
                {
                    UserId = user.id,
                    AttendanceDate = today
                };
                _context.AttendanceTrackings.Add(attendance);
            }

            var now = DateTime.Now;

            // 3) Apply Time In / Time Out
            if (Input.Action == "Time In")
            {
                // prevent double time in (optional)
                if (attendance.TimeIn != null)
                {
                    StatusMessage = "Time In already recorded for today.";
                    return Page();
                }

                attendance.TimeIn = now;
                attendance.AttendanceStatus = "Present"; // you can adjust logic
            }
            else if (Input.Action == "Time Out")
            {
                if (attendance.TimeIn == null)
                {
                    StatusMessage = "You must Time In first.";
                    return Page();
                }

                // prevent double time out (optional)
                if (attendance.TimeOut != null)
                {
                    StatusMessage = "Time Out already recorded for today.";
                    return Page();
                }

                attendance.TimeOut = now;

                // sample status update
                attendance.AttendanceStatus = "Complete";
            }

            await _context.SaveChangesAsync();

            StatusMessage = $"Saved: {Input.Action} for {Input.DisplayName} ({Input.IdNumber}) at {CurrentTime}";

            // clear fields after save
            ModelState.Clear();
            Input = new AttendanceInput();

            return Page();
        }

        public class AttendanceInput
        {
            [Required(ErrorMessage = "ID Number is required.")]
            [StringLength(30)]
            public string? IdNumber { get; set; }

            [Required(ErrorMessage = "Display Name is required.")]
            [StringLength(60)]
            public string? DisplayName { get; set; }

            [Required(ErrorMessage = "Please select Time In or Time Out.")]
            public string? Action { get; set; }
        }

        // Your existing AJAX lookup (keep it)
        public async Task<IActionResult> OnGetEmployeeNameAsync(string employeeNumber)
        {
            if (string.IsNullOrWhiteSpace(employeeNumber))
                return new JsonResult(new { found = false });

            employeeNumber = employeeNumber.Trim();

            var emp = await _context.UserInformation
                .AsNoTracking()
                .Where(x => x.EmployeeNumber == employeeNumber)
                .Select(x => new { x.FirstName, x.MiddleName, x.LastName })
                .FirstOrDefaultAsync();

            if (emp == null)
                return new JsonResult(new { found = false });

            string middle = string.IsNullOrWhiteSpace(emp.MiddleName) ? "" : $" {emp.MiddleName}";
            string fullName = $"{emp.FirstName}{middle} {emp.LastName}".Trim();

            return new JsonResult(new { found = true, fullName });
        }
    }
}
