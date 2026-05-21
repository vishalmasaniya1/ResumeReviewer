using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ResumeReviewer.Application.DTOs;
using ResumeReviewer.Application.Interfaces;

namespace ResumeReviewer.Infrastructure.AI;

public class LLMService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly AIOptions _options;

    public LLMService(HttpClient httpClient, IOptions<AIOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<ParsedResumeData> ParseResumeStructuredAsync(string resumeText)
    {
        if (_options.Provider.Equals("Mock", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Mock provider selected; using application fallbacks.");
        }

        var systemPrompt = @"You are an expert resume parser. You will extract structured candidate details from the provided resume text.
Your response MUST be a valid JSON object matching this schema. Do not write any conversational text or markdown outside the JSON.

JSON Schema:
{
  ""FirstName"": ""Candidate's first name"",
  ""LastName"": ""Candidate's last name"",
  ""Email"": ""Candidate's email address"",
  ""Phone"": ""Candidate's phone number"",
  ""Skills"": [""Skill1"", ""Skill2""],
  ""ExperienceYears"": 5,
  ""Education"": ""[ { \\""degree\\"": \\""Bachelor of Science\\"", \\""field\\"": \\""Computer Science\\"", \\""institution\\"": \\""Stanford University\\"", \\""year\\"": \\""2020\\"" } ]"",
  ""Projects"": ""[ { \\""title\\"": \\""E-Commerce Platform\\"", \\""description\\"": \\""Built a microservices shop.\\"", \\""technologies\\"": [\\""C#\\"", \\""Docker\\""] } ]"",
  ""Certifications"": [""AWS Certified Solution Architect""]
}

Important notes:
1. 'Education' and 'Projects' MUST be valid JSON-serialized strings (double-encoded JSON arrays of objects) containing their respective details, NOT raw objects.
2. Keep 'Skills' and 'Certifications' as clean string arrays.
3. Keep 'ExperienceYears' as an integer. Summarize overall tenure.";

        var prompt = $"Resume text to parse:\n\n{resumeText}";

        var jsonResponse = await ExecuteLLMQueryAsync(systemPrompt, prompt);
        var parsed = CleanAndDeserialize<ParsedResumeData>(jsonResponse);
        
        if (parsed == null)
            throw new InvalidOperationException("Failed to deserialize parsed resume details from AI response.");

        return parsed;
    }

    public async Task<AIAnalysisResult> AnalyzeResumeAlignmentAsync(string resumeText, string jobDescriptionText)
    {
        if (_options.Provider.Equals("Mock", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Mock provider selected; using application fallbacks.");
        }

        var systemPrompt = @"You are a senior recruiter screening candidates for a job.
You will compare the candidate's resume text against the job description.
Assess skills alignment, experience gaps, strengths, and areas where their resume could be improved for ATS compliance.
Also, generate 3 specific, tough interview questions based on their profile.
Your response MUST be a valid JSON object matching this schema. Do not write any conversational text or markdown outside the JSON.

JSON Schema:
{
  ""MatchScore"": 85.5,
  ""MissingSkills"": [""Kubernetes"", ""Redis""],
  ""Strengths"": [""5+ years of C# development"", ""Solid cloud experience with AWS""],
  ""ATSImprovements"": [""Add 'Docker' explicitly to the skills summary since it is mentioned in project descriptions but not parsed as a core skill."", ""Increase keyword density for '.NET Core'""],
  ""InterviewQuestions"": [""Can you explain how you structured the microservices on AWS in your E-commerce project?"", ""What is your experience with Docker?""],
  ""FeedbackText"": ""Candidate is a strong fit...""
}

Notes:
1. 'MatchScore' is a number between 0 and 100. Be critical and objective.
2. Ensure 'FeedbackText' is a detailed 2-3 sentence recruiter review.";

        var prompt = $"Job Description:\n{jobDescriptionText}\n\nCandidate Resume:\n{resumeText}";

        var jsonResponse = await ExecuteLLMQueryAsync(systemPrompt, prompt);
        var parsed = CleanAndDeserialize<AIAnalysisResult>(jsonResponse);

        if (parsed == null)
            throw new InvalidOperationException("Failed to deserialize screening feedback from AI response.");

        return parsed;
    }

    // ----------------------------------------------------------------
    // Core API request dispatchers
    // ----------------------------------------------------------------

    private async Task<string> ExecuteLLMQueryAsync(string systemPrompt, string prompt)
    {
        if (_options.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            var response = await _httpClient.PostAsJsonAsync($"{_options.OllamaUrl}/api/generate", new
            {
                model = _options.OllamaModel,
                prompt = $"{systemPrompt}\n\n{prompt}",
                stream = false,
                format = "json",
                options = new { temperature = 0.1 }
            });

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Ollama API returned error: {response.StatusCode}");

            var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>();
            return result?.Response ?? string.Empty;
        }
        else if (_options.Provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(_options.ApiKey))
                throw new InvalidOperationException("Gemini API key is not configured.");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.GeminiModel}:generateContent?key={_options.ApiKey}";
            var payload = new
            {
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = $"{systemPrompt}\n\n{prompt}" } } }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    responseMimeType = "application/json"
                }
            };

            var response = await _httpClient.PostAsJsonAsync(url, payload);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Gemini API returned error: {response.StatusCode} - {errorText}");
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiGenerateResponse>();
            var text = result?.Candidates?[0]?.Content?.Parts?[0]?.Text;
            return text ?? string.Empty;
        }

        throw new NotSupportedException($"AI Provider '{_options.Provider}' is not supported.");
    }

    // Helper: extracts JSON from Markdown blocks and parses it safely
    private static T? CleanAndDeserialize<T>(string rawResponse) where T : class
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
            return null;

        var clean = rawResponse.Trim();

        // Strip markdown code blocks if the LLM wrapped it despite instructions
        if (clean.StartsWith("```"))
        {
            var firstLineEnd = clean.IndexOf('\n');
            var lastCodeBlock = clean.LastIndexOf("```");
            if (firstLineEnd != -1 && lastCodeBlock > firstLineEnd)
            {
                clean = clean.Substring(firstLineEnd + 1, lastCodeBlock - firstLineEnd - 1).Trim();
            }
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            return JsonSerializer.Deserialize<T>(clean, options);
        }
        catch (JsonException)
        {
            // Try fallback clean: find the first '{' and the last '}'
            var firstBrace = clean.IndexOf('{');
            var lastBrace = clean.LastIndexOf('}');
            if (firstBrace != -1 && lastBrace > firstBrace)
            {
                try
                {
                    var partialClean = clean.Substring(firstBrace, lastBrace - firstBrace + 1);
                    return JsonSerializer.Deserialize<T>(partialClean, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    // Fail gracefully
                }
            }
            throw;
        }
    }
}

// Ollama Deserializer Model
public class OllamaGenerateResponse
{
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;
}

// Gemini Deserializer Models
public class GeminiGenerateResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate>? Candidates { get; set; }
}

public class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }
}

public class GeminiContent
{
    [JsonPropertyName("parts")]
    public List<GeminiPart>? Parts { get; set; }
}

public class GeminiPart
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
