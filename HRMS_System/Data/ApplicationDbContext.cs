using HRMS_System.Models;
using HRMS_System.Models.Evaluation;
using HRMS_System.Models.Reports;
using Microsoft.EntityFrameworkCore;

namespace HRMS_System.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<LoginModel> Login { get; set; } = null!;
        public DbSet<UserInformationModel> UserInformation { get; set; } = null!;
        public DbSet<AttendanceTrackingModel> AttendanceTracking { get; set; } = null!;
        public DbSet<EvaluationModel> Evaluation { get; set; } = null!;
        public DbSet<TrainingandSeminar> TrainingandSeminar { get; set; } = null!;
        public DbSet<PromotionRecord> PromotionRecords { get; set; } = null!;
        public DbSet<PromotionNotificationModel> PromotionNotifications { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
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

            modelBuilder.Entity<TrainingandSeminar>()
                .ToTable("TrainingandSeminar");

            modelBuilder.Entity<TrainingandSeminar>()
                .HasOne(t => t.UserInfo)
                .WithMany(u => u.TrainingandSeminar)
                .HasForeignKey(t => t.UserInformationId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}