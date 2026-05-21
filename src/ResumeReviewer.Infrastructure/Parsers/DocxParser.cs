using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ResumeReviewer.Application.Interfaces;

namespace ResumeReviewer.Infrastructure.Parsers;

public class DocxParser : IDocxParser
{
    public async Task<string> ParseAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("DOCX file not found", filePath);

        var sb = new StringBuilder();
        try
        {
            using (var wordDoc = WordprocessingDocument.Open(filePath, false))
            {
                var body = wordDoc.MainDocumentPart?.Document.Body;
                if (body != null)
                {
                    foreach (var paragraph in body.Descendants<Paragraph>())
                    {
                        var text = paragraph.InnerText;
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            sb.AppendLine(text);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to extract text from DOCX: {ex.Message}", ex);
        }

        return await Task.FromResult(sb.ToString());
    }
}
