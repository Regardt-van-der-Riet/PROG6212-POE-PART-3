using Microsoft.EntityFrameworkCore;

namespace CMCS
{
    public class ClaimsData : DbContext
    {
        public DbSet<ClaimsRecord> ClaimsRecord { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=DataFile3.db");
            }
        }
    }
}
