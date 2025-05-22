using Microsoft.EntityFrameworkCore;

namespace FileAnalysisService.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<WordCloudRecord> WordCloudRecords => Set<WordCloudRecord>();
    }
}