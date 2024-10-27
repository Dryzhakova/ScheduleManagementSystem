using Microsoft.EntityFrameworkCore;

namespace WebAppsMoodle.Models
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Classes> Classes { get; set; } 
        public DbSet<ClassesDescription> ClassesDescription { get; set; }
        public DbSet<OneTimeClassDate> OneTimeClasses { get; set; }
        public DbSet<RecurringClassDate> RecurringClasses { get; set;}

    }
}
