namespace FileStoringService.Models
{
    public class FileRecord
    {
        public int Id { get; set; }
        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public string Hash { get; set; } = null!;
    }
}
