using Microsoft.EntityFrameworkCore;

namespace FileStoringService.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<FileRecord> FileRecords => Set<FileRecord>();
    }
}
