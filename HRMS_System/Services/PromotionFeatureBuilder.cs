using HRMS_System.Data;
using HRMS_System.Models;
using HRMS_System.Models.Evaluation;
using HRMS_System.Models.PromotionML;
using Microsoft.EntityFrameworkCore;

namespace HRMS_System.Services
{
    public class PromotionFeatureBuilder
    {
        private readonly ApplicationDbContext _context;

        private const int AttendanceWindowDays = 180;
        private const int EvaluationWindowDays = 365;
        private const int TrainingWindowDays = 365;

        // Change this if your company considers late after a different time
        private static readonly TimeSpan LateCutoff = new(8, 15, 0);

        public PromotionFeatureBuilder(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<PromotionTrainingRow> BuildTrainingRows()
        {
            var employeeIds = _context.UserInformation
                .AsNoTracking()
                .Select(x => x.Id)
                .ToList();

            var promotedIds = _context.PromotionRecords
                .AsNoTracking()
                .Select(x => x.EmployeeId)
                .Distinct()
                .ToHashSet();

            var rows = new List<PromotionTrainingRow>();

            foreach (var userId in employeeIds)
            {
                var row = BuildFeatureRow(userId);
                row.WasPromoted = promotedIds.Contains(userId);

                // optional filter: skip rows with zero source data
                if (row.TenureMonths == 0 &&
                    row.AbsenceRate == 0 &&
                    row.LateRate == 0 &&
                    row.TrainingCount == 0 &&
                    row.CertificationCount == 0 &&
                    row.AvgEvaluationScore == 0)
                {
                    continue;
                }

                rows.Add(row);
            }

            return rows;
        }

        public PromotionTrainingRow BuildFeatureRow(int userId)
        {
            var employee = _context.UserInformation
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == userId);

            if (employee == null)
                throw new Exception("Employee not found.");

            var today = DateTime.Today;

            var attendanceFrom = today.AddDays(-AttendanceWindowDays);
            var evaluationFrom = today.AddDays(-EvaluationWindowDays);
            var trainingFrom = today.AddDays(-TrainingWindowDays);

            var attendanceRows = _context.AttendanceTracking
                .AsNoTracking()
                .Where(x => x.UserId == userId
                         && x.AttendanceDate >= attendanceFrom
                         && x.AttendanceDate <= today)
                .ToList();

            var evaluationRows = _context.Evaluation
                .AsNoTracking()
                .Where(x => x.UserId == userId
                         && x.EvaluationDate >= evaluationFrom
                         && x.EvaluationDate <= today)
                .ToList();

            var trainingRows = _context.TrainingandSeminar
                .AsNoTracking()
                .Where(x => x.UserInformationId == userId
                         && x.DateAccomplished >= trainingFrom
                         && x.DateAccomplished <= today)
                .ToList();

            float tenureMonths = employee.TenureMonths.HasValue
                ? employee.TenureMonths.Value
                : CalculateMonths(employee.StartDate, today);

            float absenceRate = 0f;
            float lateRate = 0f;

            if (attendanceRows.Count > 0)
            {
                var absentCount = attendanceRows.Count(x =>
                    !string.IsNullOrWhiteSpace(x.AttendanceStatus) &&
                    x.AttendanceStatus.Trim().Equals("Absent", StringComparison.OrdinalIgnoreCase));

                var presentRows = attendanceRows
                    .Where(x =>
                        string.IsNullOrWhiteSpace(x.AttendanceStatus) ||
                        !x.AttendanceStatus.Trim().Equals("Absent", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var lateCount = presentRows.Count(x =>
                    x.TimeIn.HasValue &&
                    x.TimeIn.Value.TimeOfDay > LateCutoff);

                absenceRate = (float)absentCount / attendanceRows.Count;
                lateRate = presentRows.Count > 0
                    ? (float)lateCount / presentRows.Count
                    : 0f;
            }

            float avgEvaluationScore = 0f;

            if (evaluationRows.Count > 0)
            {
                avgEvaluationScore = evaluationRows
                    .Average(x =>
                        ((x.WorkQuality ?? 0) +
                         (x.Productivity ?? 0) +
                         (x.Teamwork ?? 0) +
                         (x.Attendance ?? 0) +
                         (x.Communication ?? 0)) / 5f);
            }

            float trainingCount = trainingRows.Count;
            float certificationCount = trainingRows.Sum(x => (float)x.CertificateCount);

            return new PromotionTrainingRow
            {
                TenureMonths = tenureMonths,
                AbsenceRate = absenceRate,
                LateRate = lateRate,
                TrainingCount = trainingCount,
                CertificationCount = certificationCount,
                AvgEvaluationScore = avgEvaluationScore,
                WasPromoted = false
            };
        }

        private static int CalculateMonths(DateTime startDate, DateTime endDate)
        {
            int months = (endDate.Year - startDate.Year) * 12 + endDate.Month - startDate.Month;

            if (endDate.Day < startDate.Day)
                months--;

            return Math.Max(months, 0);
        }
    }
}