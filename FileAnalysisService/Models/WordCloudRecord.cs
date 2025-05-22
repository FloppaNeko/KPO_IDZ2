namespace FileAnalysisService.Models
{
    public class WordCloudRecord
    {
        public int Id { get; set; }
        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
    }
}
