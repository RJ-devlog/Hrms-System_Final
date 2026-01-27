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

        public async Task<List<SelectListItem>> GetDepartmentOptionsAsync(bool includeAddNew = true)
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

            if (includeAddNew)
                items.Add(new SelectListItem { Value = AddNewValue.ToString(), Text = "Add new department..." });

            return items;
        }

        public async Task<int> GetOrCreateDepartmentIdAsync(string newDepartmentName)
        {
            var name = (newDepartmentName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Department name is required.");

            var existing = await _context.Departments
                .FirstOrDefaultAsync(d => d.Name == name);

            if (existing != null)
                return existing.Id;

            var dept = new Department { Name = name };
            _context.Departments.Add(dept);
            await _context.SaveChangesAsync();

            return dept.Id;
        }
    }
}
