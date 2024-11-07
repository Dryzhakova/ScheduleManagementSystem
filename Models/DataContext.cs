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
        public DbSet<RecurringClassDate> RecurringClasses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Classes>()
           .HasOne(c => c.Teacher)
           .WithMany(t => t.Classes)
           .HasForeignKey(c => c.TeacherId);

            modelBuilder.Entity<Classes>()
           .HasOne(c => c.Room)
           .WithMany(r => r.Classes)
           .HasForeignKey(c => c.RoomId);

            modelBuilder.Entity<Classes>()
           .HasOne(c => c.ClassesDescription)
           .WithMany(d => d.Classes)
           .HasForeignKey(c => c.ClassesDescriptionId);

            // Связь Classes и OneTimeClassDate
            modelBuilder.Entity<Classes>()
            .HasMany(c => c.OneTimeClassDates) // Одно занятие может иметь много дат
            .WithOne() // Указывает, что OneTimeClassDate не имеет навигационного свойства обратно к Classes
            .HasForeignKey(с => с.ClassesId);



        }
    }
}
