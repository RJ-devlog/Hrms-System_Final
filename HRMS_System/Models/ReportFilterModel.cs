using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;

namespace HRMS_System.Models.Reports
{
    public class ReportFilterModel
    {
        [Key]
        public int Id { get; set; } 
        // Tabs
        [BindProperty(SupportsGet = true)]
        public string ActiveTab { get; set; } = "performance";

        // Shared Filters
        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        // Performance filters
        [BindProperty(SupportsGet = true)]
        public string? Period { get; set; }

        // Attendance filters
        [BindProperty(SupportsGet = true)]
        public string? Department { get; set; }

        // Training filters
        [BindProperty(SupportsGet = true)]
        public string? Provider { get; set; }
    }
}
