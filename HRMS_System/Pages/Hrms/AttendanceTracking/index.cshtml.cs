    using HRMS_System.Data;
    using HRMS_System.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    namespace HRMS_System.Pages.Hrms.AttendanceTracking
    {
        public class AttendanceSummaryRow
        {
            public int UserId { get; set; }
            public string EmployeeNumber { get; set; } = "";
            public string FullName { get; set; } = "";
            public int OnTime { get; set; }
            public int Late { get; set; }
            public int Absent { get; set; }
        }

        public class AttendanceTrackingPageModel : PageModel
        {
            public string? FromValue { get; set; }
            public string? ToValue { get; set; }

            private readonly ApplicationDbContext _context;

            public AttendanceTrackingPageModel(ApplicationDbContext context)
            {
                _context = context;
            }

            public List<UserInformationModel> Employees { get; set; } = new();

            // for Time In/Out tab (today)
            public List<AttendanceTrackingModel> TodayAttendance { get; set; } = new();

            //  for Date Tracking tab (range)
            public List<AttendanceTrackingModel> DateRangeAttendance { get; set; } = new();
            public List<AttendanceSummaryRow> AttendanceSummary { get; set; } = new();

            //Time In/Out based on real time Date Month
            public List<AttendanceTrackingModel> TimeInOutMonthAttendance { get; set; } = new();

            public int TotalWorkDays { get; set; }
            public DateTime SummaryMonth { get; set; }
            public async Task OnGetAsync(string? summaryMonth, DateTime? from, DateTime? to)
            {
                FromValue = from?.ToString("yyyy-MM-dd");
                ToValue = to?.ToString("yyyy-MM-dd");

                var today = DateTime.Today;

                //SummaryMonth for Attendance Summary (UI month picker)
                if (!string.IsNullOrWhiteSpace(summaryMonth) && DateTime.TryParse(summaryMonth + "-01", out var parsed))
                    SummaryMonth = new DateTime(parsed.Year, parsed.Month, 1);
                else
                    SummaryMonth = new DateTime(today.Year, today.Month, 1);

                // Month used for Attendance Summary tab only
                var summaryMonthStart = SummaryMonth.Date;
                var summaryMonthEndExclusive = summaryMonthStart.AddMonths(1);

                //REAL current month for Time In/Out tab (dynamic, not affected by summaryMonth picker)
                var realMonthStart = new DateTime(today.Year, today.Month, 1);
                var realMonthEndExclusive = realMonthStart.AddMonths(1);

                //Load Employees (you removed this, add it back)
                Employees = await _context.UserInformation
                    .AsNoTracking()
                    .OrderBy(u => u.LastName)
                    .ToListAsync();

                //Time In/Out tab data (REAL current month)
                TimeInOutMonthAttendance = await _context.AttendanceTrackings
                    .AsNoTracking()
                    .Include(a => a.User) // optional but safe
                    .Where(a => a.AttendanceDate >= realMonthStart && a.AttendanceDate < realMonthEndExclusive)
                    .OrderByDescending(a => a.AttendanceDate)
                    .ThenByDescending(a => a.TimeIn)
                    .ToListAsync();

                // -------------------- DATE TRACKING RANGE --------------------
                DateTime rangeStart;
                DateTime rangeEndExclusive;

                if (from.HasValue || to.HasValue)
                {
                    rangeStart = (from ?? DateTime.MinValue).Date;
                    rangeEndExclusive = (to ?? DateTime.MaxValue).Date.AddDays(1);
                }
                else
                {
                    // default = summary month (or you can use real month, your choice)
                    rangeStart = summaryMonthStart;
                    rangeEndExclusive = summaryMonthEndExclusive;
                }

                DateRangeAttendance = await _context.AttendanceTrackings
                    .AsNoTracking()
                    .Include(a => a.User)
                    .Where(a => a.AttendanceDate >= rangeStart && a.AttendanceDate < rangeEndExclusive)
                    .OrderByDescending(a => a.AttendanceDate)
                    .ThenByDescending(a => a.TimeIn)
                    .ToListAsync();

                //Today attendance
                TodayAttendance = await _context.AttendanceTrackings
                    .AsNoTracking()
                    .Where(a => a.AttendanceDate == today)
                    .ToListAsync();

                //Total workdays based on SUMMARY MONTH
                TotalWorkDays = Enumerable.Range(1, DateTime.DaysInMonth(summaryMonthStart.Year, summaryMonthStart.Month))
                    .Select(d => new DateTime(summaryMonthStart.Year, summaryMonthStart.Month, d))
                    .Count(d => d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday);

                //Attendance Summary based on SUMMARY MONTH
                AttendanceSummary = await _context.AttendanceTrackings
                    .AsNoTracking()
                    .Include(a => a.User)
                    .Where(a => a.AttendanceDate >= summaryMonthStart && a.AttendanceDate < summaryMonthEndExclusive)
                    .GroupBy(a => new
                    {
                        a.UserId,
                        a.User.EmployeeNumber,
                        a.User.FirstName,
                        a.User.LastName
                    })
                    .Select(g => new AttendanceSummaryRow
                    {
                        UserId = g.Key.UserId,
                        EmployeeNumber = g.Key.EmployeeNumber.ToString(),
                        FullName = g.Key.FirstName + " " + g.Key.LastName,
                        OnTime = g.Count(x => x.AttendanceStatus == "On-Time"),
                        Late = g.Count(x => x.AttendanceStatus == "Late"),
                        Absent = TotalWorkDays - g.Select(x => x.AttendanceDate.Date).Distinct().Count()
                    })
                    .OrderBy(x => x.FullName)
                    .ToListAsync();
            }

            public async Task<IActionResult> OnGetEmployeeLogsAsync(int id, DateTime? from, DateTime? to)
            {
                var emp = await _context.UserInformation
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.id == id);

                if (emp == null) return NotFound();

                var query = _context.AttendanceTrackings
                    .AsNoTracking()
                    .Where(a => a.UserId == id);

                if (from.HasValue)
                    query = query.Where(a => a.AttendanceDate >= from.Value.Date);

                if (to.HasValue)
                {
                    // include the whole "to" day
                    var endExclusive = to.Value.Date.AddDays(1);
                    query = query.Where(a => a.AttendanceDate < endExclusive);
                }

                // Get raw data first (SQL-safe)
                var rawLogs = await query
                    .OrderBy(a => a.AttendanceDate)
                    .Select(a => new
                    {
                        a.AttendanceDate,
                        a.TimeIn,
                        a.TimeOut,
                        a.AttendanceStatus
                    })
                    .ToListAsync();

                // Format in memory (C# safe) 
                var logs = rawLogs.Select(a => new
                {
                    date = a.AttendanceDate.ToString("MMMM d, yyyy"),
                    timeIn = a.TimeIn.HasValue ? a.TimeIn.Value.ToString("hh:mm tt") : null,
                    timeOut = a.TimeOut.HasValue ? a.TimeOut.Value.ToString("hh:mm tt") : null,
                    status = a.AttendanceStatus ?? "Absent"
                });

                return new JsonResult(new
                {
                    EmployeeNumber = emp.EmployeeNumber,
                    fullName = emp.FirstName + " " + emp.LastName,
                    logs
                });
            }

        }
    }
