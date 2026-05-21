using System.Text;
using ResumeReviewer.Application.Interfaces;
using UglyToad.PdfPig;

namespace ResumeReviewer.Infrastructure.Parsers;

public class PdfParser : IPdfParser
{
    public async Task<string> ParseAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("PDF file not found", filePath);

        var sb = new StringBuilder();
        try
        {
            using (var pdf = PdfDocument.Open(filePath))
            {
                foreach (var page in pdf.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to extract text from PDF: {ex.Message}", ex);
        }

        return await Task.FromResult(sb.ToString());
    }
}
