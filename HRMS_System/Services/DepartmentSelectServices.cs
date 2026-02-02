using HRMS_System.Data;
using HRMS_System.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HRMS_System.Services
{
    public class DepartmentSelectService
    {
        private readonly ApplicationDbContext _context;

        public const int AddNewValue = -1;

        public DepartmentSelectService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ Ensure EmployeeCatalog.Departments exists in DB (so dropdown shows them)
        public async Task EnsureCatalogSeededAsync()
        {
            // Build a clean list from catalog
            var catalogNames = EmployeeCatalog.Departments
                .Where(x => !string.IsNullOrWhiteSpace(x.Value) &&
                            x.Value != "...." &&
                            x.Value != "-- Select Department --")
                .Select(x => x.Value.Trim())
                .Distinct()
                .ToList();

            // Get existing names from DB
            var existingNames = await _context.Departments
                .Select(d => d.Name)
                .ToListAsync();

            var existingSet = existingNames
                .Select(n => n.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Insert missing ones
            var toAdd = catalogNames
                .Where(n => !existingSet.Contains(n))
                .Select(n => new Department { Name = n })
                .ToList();

            if (toAdd.Count > 0)
            {
                _context.Departments.AddRange(toAdd);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<SelectListItem>> GetDepartmentOptionsAsync()
        {
            var items = await _context.Departments
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name
                })
                .ToListAsync();

            items.Insert(0, new SelectListItem { Value = "", Text = "-- Select Department --" });
            items.Add(new SelectListItem { Value = AddNewValue.ToString(), Text = "+ Add new department..." });
            return items;
        }

        public async Task<int> GetOrCreateDepartmentIdAsync(string departmentName)
        {
            var name = departmentName.Trim();

            var existing = await _context.Departments
                .FirstOrDefaultAsync(d => d.Name.ToLower() == name.ToLower());

            if (existing != null)
                return existing.Id;

            var dept = new Department { Name = name };
            _context.Departments.Add(dept);
            await _context.SaveChangesAsync();

            return dept.Id;
        }
    }
}
