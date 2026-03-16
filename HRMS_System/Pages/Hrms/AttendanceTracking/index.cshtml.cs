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
            TimeInOutMonthAttendance = await _context.AttendanceTracking
                .AsNoTracking()
                .Include(a => a.User)
                .Where(a => a.AttendanceDate >= realMonthStart && a.AttendanceDate < realMonthEndExclusive)
                .OrderByDescending(a => a.AttendanceDate)
                .ThenByDescending(a => a.TimeIn)
                .ToListAsync();

            // -------------------- DATE TRACKING RANGE --------------------
            // -------------------- DATE TRACKING RANGE (SAFE) --------------------
            DateTime rangeStart;
            DateTime rangeEndExclusive;

            if (from.HasValue && to.HasValue)
            {
                rangeStart = from.Value.Date;
                rangeEndExclusive = to.Value.Date.AddDays(1);

                // optional: if user swapped them, fix automatically
                if (rangeEndExclusive <= rangeStart)
                {
                    // swap
                    var tmp = rangeStart;
                    rangeStart = to.Value.Date;
                    rangeEndExclusive = from.Value.Date.AddDays(1);

                    TempData["DateError"] = "Date To was earlier than Date From. Dates were adjusted.";
                }
            }
            else if (from.HasValue && !to.HasValue)
            {
                // from only -> up to today (or you can use DateTime.Today)
                rangeStart = from.Value.Date;
                rangeEndExclusive = DateTime.Today.AddDays(1);
            }
            else if (!from.HasValue && to.HasValue)
            {
                // to only -> from earliest records
                rangeStart = DateTime.MinValue.Date;
                rangeEndExclusive = to.Value.Date.AddDays(1);
            }
            else
            {
                // no filter
                rangeStart = DateTime.MinValue.Date;
                rangeEndExclusive = DateTime.Today.AddDays(1);
            }

            DateRangeAttendance = await _context.AttendanceTracking
                    .AsNoTracking()
                    .Include(a => a.User)
                    .Where(a => a.AttendanceDate >= rangeStart && a.AttendanceDate < rangeEndExclusive)
                    .OrderByDescending(a => a.AttendanceDate)
                    .ThenByDescending(a => a.TimeIn)
                    .ToListAsync();

                //Today attendance
                TodayAttendance = await _context.AttendanceTracking
                    .AsNoTracking()
                    .Where(a => a.AttendanceDate == today)
                    .ToListAsync();

                //Total workdays based on SUMMARY MONTH
                TotalWorkDays = Enumerable.Range(1, DateTime.DaysInMonth(summaryMonthStart.Year, summaryMonthStart.Month))
                    .Select(d => new DateTime(summaryMonthStart.Year, summaryMonthStart.Month, d))
                    .Count(d => d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday);

                //Attendance Summary based on SUMMARY MONTH
                AttendanceSummary = await _context.AttendanceTracking
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
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (emp == null) return NotFound();

                var query = _context.AttendanceTracking
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
            var cutoff = new TimeSpan(8, 0, 0); // 8:00 AM

            static string FormatLate(int minutes)
            {
                if (minutes <= 0) return "0m";
                if (minutes < 60) return $"{minutes}m";

                var h = minutes / 60;
                var m = minutes % 60;
                return m == 0 ? $"{h}h" : $"{h}h {m}m";
            }

            var logs = rawLogs.Select(a =>
            {
                int lateMinutes = 0;

                if (a.TimeIn.HasValue)
                {
                    var diff = a.TimeIn.Value.TimeOfDay - cutoff;
                    if (diff.TotalMinutes > 0)
                        lateMinutes = (int)Math.Round(diff.TotalMinutes);
                }

                return new
                {
                    date = a.AttendanceDate.ToString("MMMM d, yyyy"),
                    timeIn = a.TimeIn.HasValue ? a.TimeIn.Value.ToString("hh:mm tt") : null,
                    timeOut = a.TimeOut.HasValue ? a.TimeOut.Value.ToString("hh:mm tt") : null,
                    status = a.AttendanceStatus ?? "Absent",

                    // 👇 NEW FIELDS
                    lateMinutes = lateMinutes,
                    lateDisplay = lateMinutes > 0 ? FormatLate(lateMinutes) : "0m"
                };
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
