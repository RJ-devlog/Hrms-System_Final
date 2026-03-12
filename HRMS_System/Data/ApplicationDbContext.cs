using HRMS_System.Models;
using HRMS_System.Models.Evaluation;
using HRMS_System.Models.Reports;
using HRMS_System.Models.Training;
using Microsoft.EntityFrameworkCore;
namespace HRMS_System.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }

        public DbSet<LoginModel> Login { get; set; }
        public DbSet<UserInformationModel> UserInformation { get; set; }
        public DbSet<AttendanceTrackingModel> AttendanceTrackings { get; set; }
        public DbSet<TrainingSession> TrainingSessions { get; set; }
        public DbSet<TrainingRecord> TrainingRecords { get; set; }
        public DbSet<ReportFilterModel> ReportFilters { get; set; }
        public DbSet<EvaluationModel> Evaluations { get; set; } = null!;
        public DbSet<PromotionNotificationModel> PromotionNotifications { get; set; }
        /*        public DbSet<DepartmentSelectService> Departments { get; set; }*/
        public DbSet<Department> Departments { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserInformationModel>()
                .HasIndex(e => e.EmployeeNumber)
                .IsUnique();

            modelBuilder.Entity<LoginModel>()
                .HasIndex(l => l.EmployeeNumber)
                .IsUnique();

            modelBuilder.Entity<UserInformationModel>()
                .HasOne(u => u.Login)
                .WithOne(l => l.UserInformation)
                .HasForeignKey<LoginModel>(l => l.UserInformationId)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }
}
