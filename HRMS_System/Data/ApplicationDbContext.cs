using HRMS_System.Models;
using Microsoft.EntityFrameworkCore;
using HRMS_System.Models.Training;
namespace HRMS_System.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserInformationModel> UserInformation { get; set; }
        public DbSet<AttendanceTrackingModel> AttendanceTrackings { get; set; }
        public DbSet<TrainingSession> TrainingSessions { get; set; }
        public DbSet<TrainingRecord> TrainingRecords { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserInformationModel>()
                .HasIndex(e => e.EmployeeNumber)
                .IsUnique();
        }

    }
}
