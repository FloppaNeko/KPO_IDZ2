namespace FileAnalysisService.Utilities
{
    public class TextFileAnalysis
    {
        public int ParagraphCount { get; set; }
        public int WordCount { get; set; }
        public int SymbolCount { get; set; }
    }

    public static class TextAnalyzer
    {
        public static TextFileAnalysis Analyze(string text)
        {
            // Normalize line endings
            text = text.Replace("\r\n", "\n").Trim();

            // Count paragraphs: split by double newlines
            var paragraphs = text.Split(["\n\n"], StringSplitOptions.RemoveEmptyEntries);

            // Count words: split by whitespace (including tabs, newlines, etc.)
            var words = text.Split(null as char[], StringSplitOptions.RemoveEmptyEntries); // null = default whitespace

            // Count symbols: all non-whitespace characters (optionally skip \n, \r)
            var symbols = text.Count(c => !char.IsWhiteSpace(c));

            return new TextFileAnalysis
            {
                ParagraphCount = paragraphs.Length,
                WordCount = words.Length,
                SymbolCount = symbols
            };
        }
    }

}
