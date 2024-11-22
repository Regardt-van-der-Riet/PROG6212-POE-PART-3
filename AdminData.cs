using Microsoft.EntityFrameworkCore;

namespace CMCS
{
    public class AdminData : DbContext
    {
        public DbSet<Admin> Admin { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=DataFile2.db");
            }
        }
    }
}
