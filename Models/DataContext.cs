using Microsoft.EntityFrameworkCore;

namespace WebAppsMoodle.Models
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }
        public DbSet<Teacher> Teachers { get; set; }

    }
}
