using HRMS_System.Data;
using HRMS_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HRMS_System.Pages.Hrms.Notification
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // The list of promotion notifications shown on the UI
        public List<PromotionNotifVM> PromotionNotifications { get; set; } = new();

        // Used by MarkRead/Archive actions
        [BindProperty]
        public int? SelectedId { get; set; }

        public async Task OnGetAsync()
        {
            // Load from DB (not demo)
            var items = await _context.PromotionNotifications
                .AsNoTracking()
                .Where(n => !n.IsArchived)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            PromotionNotifications = items.Select(n => new PromotionNotifVM
            {
                Id = n.Id,
                EmployeeName = n.EmployeeName,
                Title = n.Title,
                Message = n.Message,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead,
                StatusKey = n.StatusKey
            }).ToList();
        }

        // Mark all notifications as read
        public async Task<IActionResult> OnPostMarkAllReadAsync()
        {
            var notifs = await _context.PromotionNotifications
                .Where(n => !n.IsArchived && !n.IsRead)
                .ToListAsync();

            foreach (var n in notifs)
                n.IsRead = true;

            await _context.SaveChangesAsync();

            TempData["Success"] = "All promotion notifications marked as read.";
            return RedirectToPage();
        }

        // Mark selected notification as read
        public async Task<IActionResult> OnPostMarkReadAsync()
        {
            if (!SelectedId.HasValue)
            {
                TempData["Error"] = "Select a notification first.";
                return RedirectToPage();
            }

            var notif = await _context.PromotionNotifications
                .FirstOrDefaultAsync(n => n.Id == SelectedId.Value);

            if (notif == null)
            {
                TempData["Error"] = "Notification not found.";
                return RedirectToPage();
            }

            notif.IsRead = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Notification #{SelectedId} marked as read.";
            return RedirectToPage();
        }

        // Archive selected notification
        public async Task<IActionResult> OnPostArchiveAsync()
        {
            if (!SelectedId.HasValue)
            {
                TempData["Error"] = "Select a notification first.";
                return RedirectToPage();
            }

            var notif = await _context.PromotionNotifications
                .FirstOrDefaultAsync(n => n.Id == SelectedId.Value);

            if (notif == null)
            {
                TempData["Error"] = "Notification not found.";
                return RedirectToPage();
            }

            notif.IsArchived = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Notification #{SelectedId} archived.";
            return RedirectToPage();
        }

        public class PromotionNotifVM
        {
            public int Id { get; set; }
            public string EmployeeName { get; set; } = "";
            public string Title { get; set; } = "";
            public string Message { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public bool IsRead { get; set; }

            public string StatusKey { get; set; } = "created";

            public string CreatedAtText => CreatedAt.ToString("yyyy-MM-dd hh:mm tt");

            public string StatusLabel =>
                StatusKey switch
                {
                    "predicted_yes" => "Predicted YES",
                    "predicted_no" => "Predicted NO",
                    "created" => "Record Created",
                    "approved" => "Approved",
                    "rejected" => "Rejected",
                    _ => "Update"
                };
        }
    }
}
