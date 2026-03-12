using HRMS_System.Data;
using HRMS_System.Models;
using HRMS_System.Models.PromotionML;

namespace HRMS_System.Services
{
    public class PromotionFeatureBuilder
    {
        private readonly ApplicationDbContext _context;

        public PromotionFeatureBuilder(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<PromotionTrainingRow> BuildTrainingRows()
        {
            return new List<PromotionTrainingRow>
            {
                new PromotionTrainingRow
                {
                    TenureMonths = 18,
                    AbsenceRate = 0.02f,
                    LateRate = 0.05f,
                    TrainingCount = 4,
                    CertificationCount = 2,
                    AvgEvaluationScore = 4.3f,
                    WasPromoted = true
                }
            };
        }

        public PromotionTrainingRow BuildFeatureRow(int userId)
        {
            return new PromotionTrainingRow
            {
                TenureMonths = 18,
                AbsenceRate = 0.02f,
                LateRate = 0.05f,
                TrainingCount = 4,
                CertificationCount = 2,
                AvgEvaluationScore = 4.3f
            };
        }
    }
}