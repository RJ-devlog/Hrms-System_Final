using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;

namespace HRMS_System.Pages.Hrms.Notification
{
    public class IndexModel : PageModel
    {
        // The list of promotion notifications shown on the UI
        public List<PromotionNotifVM> PromotionNotifications { get; set; } = new();

        // Used by MarkRead/Archive actions
        [BindProperty]
        public int? SelectedId { get; set; }

        public void OnGet()
        {
            // Demo data (replace with DB later)
            PromotionNotifications = new()
            {
                new PromotionNotifVM(1, "Juan Dela Cruz", "Promotion Prediction: HIGH", "Predicted promotable with 82% confidence.", DateTime.Now.AddMinutes(-15), false, "predicted_yes"),
                new PromotionNotifVM(2, "Maria Santos", "Promotion Prediction: LOW", "Predicted not promotable (35%). Improve evaluations and attendance.", DateTime.Now.AddHours(-2), true, "predicted_no"),
                new PromotionNotifVM(3, "Juan Dela Cruz", "Promotion Record Created", "A promotion record was created for review: Team Lead.", DateTime.Now.AddDays(-1), true, "created"),
                new PromotionNotifVM(4, "Maria Santos", "Promotion Approved", "Promotion approved. New role: Senior Staff.", DateTime.Now.AddDays(-4), true, "approved"),
                new PromotionNotifVM(5, "Employee EMP-010", "Promotion Rejected", "Promotion rejected due to insufficient evaluation score.", DateTime.Now.AddDays(-7), true, "rejected")
            };
        }

        // Mark all notifications as read (placeholder)
        public IActionResult OnPostMarkAllRead()
        {
            TempData["Success"] = "All promotion notifications marked as read (placeholder).";
            return RedirectToPage();
        }

        // Mark selected notification as read (placeholder)
        public IActionResult OnPostMarkRead()
        {
            TempData["Success"] = $"Notification #{SelectedId} marked as read (placeholder).";
            return RedirectToPage();
        }

        // Archive selected notification (placeholder)
        public IActionResult OnPostArchive()
        {
            TempData["Success"] = $"Notification #{SelectedId} archived (placeholder).";
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

            // Used by filter dropdown and CSS status pill
            public string StatusKey { get; set; } = "created";

            public string CreatedAtText => CreatedAt.ToString("yyyy-MM-dd hh:mm tt");

            // Friendly label shown on UI
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

            public PromotionNotifVM() { }

            public PromotionNotifVM(int id, string emp, string title, string msg, DateTime created, bool isRead, string statusKey)
            {
                Id = id;
                EmployeeName = emp;
                Title = title;
                Message = msg;
                CreatedAt = created;
                IsRead = isRead;
                StatusKey = statusKey;
            }
        }
    }
}
