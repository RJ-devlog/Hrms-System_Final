using HRMS_System.Models;
using Microsoft.EntityFrameworkCore;

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


    }
}
