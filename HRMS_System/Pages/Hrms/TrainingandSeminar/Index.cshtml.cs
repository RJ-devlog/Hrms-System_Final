using HRMS_System.Data;
using HRMS_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TrainingRecord = HRMS_System.Models.TrainingandSeminar;

namespace HRMS_System.Pages.Hrms.TrainingandSeminar
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<TrainingRecord> Trainings { get; set; } = new();
        public List<SelectListItem> EmployeeOptions { get; set; } = new();
        public List<EmployeeCertificateCountModel> CertificateCounts { get; set; } = new();

        [BindProperty]
        public TrainingRecord Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public async Task OnGetAsync()
        {
            await LoadPageDataAsync();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadPageDataAsync();
                return Page();
            }

            if (Input.Id == 0)
            {
                _context.TrainingandSeminar.Add(Input);
            }
            else
            {
                var existing = await _context.TrainingandSeminar
                    .FirstOrDefaultAsync(x => x.Id == Input.Id);

                if (existing == null)
                    return NotFound();

                existing.UserInformationId = Input.UserInformationId;
                existing.Title = Input.Title;
                existing.DateAccomplished = Input.DateAccomplished;
                existing.Points = Input.Points;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var record = await _context.TrainingandSeminar
                .FirstOrDefaultAsync(x => x.Id == id);

            if (record != null)
            {
                _context.TrainingandSeminar.Remove(record);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        private async Task LoadPageDataAsync()
        {
            var query = _context.TrainingandSeminar
                .Include(x => x.UserInfo)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var keyword = SearchTerm.Trim();

                query = query.Where(x =>
                    x.Title.Contains(keyword) ||
                    (
                        x.UserInfo != null &&
                        (
                            (x.UserInfo.FirstName ?? "").Contains(keyword) ||
                            (x.UserInfo.LastName ?? "").Contains(keyword) ||
                            (x.UserInfo.EmployeeNumber ?? "").Contains(keyword)
                        )
                    ));
            }

            Trainings = await query
                .OrderByDescending(x => x.DateAccomplished)
                .ToListAsync();

            var countsByEmployee = await _context.TrainingandSeminar
                .GroupBy(x => x.UserInformationId)
                .Select(g => new
                {
                    UserInformationId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.UserInformationId, x => x.Count);

            foreach (var item in Trainings)
            {
                item.CertificateCount = countsByEmployee.TryGetValue(item.UserInformationId, out var count)
                    ? count
                    : 0;
            }

            EmployeeOptions = await _context.UserInformation
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = (x.EmployeeNumber ?? "") + " - " + (x.FirstName ?? "") + " " + (x.LastName ?? "")
                })
                .ToListAsync();
        }
        private static string BuildEmployeeDisplay(string? employeeNumber, string? firstName, string? lastName)
        {
            var empNo = employeeNumber?.Trim() ?? "";
            var fullName = $"{firstName ?? ""} {lastName ?? ""}".Trim();

            if (!string.IsNullOrWhiteSpace(empNo) && !string.IsNullOrWhiteSpace(fullName))
                return $"{empNo} - {fullName}";

            if (!string.IsNullOrWhiteSpace(fullName))
                return fullName;

            if (!string.IsNullOrWhiteSpace(empNo))
                return empNo;

            return "Unknown Employee";
        }
    }

    public class EmployeeCertificateCountModel
    {
        public int UserInformationId { get; set; }
        public string EmployeeDisplay { get; set; } = string.Empty;
        public int CertificateCount { get; set; }
    }
}