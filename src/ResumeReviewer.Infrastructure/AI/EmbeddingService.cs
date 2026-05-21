using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ResumeReviewer.Application.Interfaces;

namespace ResumeReviewer.Infrastructure.AI;

public class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly AIOptions _options;

    public EmbeddingService(HttpClient httpClient, IOptions<AIOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new float[384];

        if (_options.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_options.OllamaUrl}/api/embeddings", new
                {
                    model = _options.OllamaEmbeddingModel,
                    prompt = text
                });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();
                    if (result?.Embedding != null)
                        return result.Embedding;
                }
            }
            catch
            {
                // Fallback to Mock if Ollama call fails
            }
        }
        else if (_options.Provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(_options.ApiKey))
        {
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent?key={_options.ApiKey}";
                var response = await _httpClient.PostAsJsonAsync(url, new
                {
                    model = "models/text-embedding-004",
                    content = new
                    {
                        parts = new[] { new { text = text } }
                    }
                });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GeminiEmbeddingResponse>();
                    if (result?.Embedding?.Values != null)
                        return result.Embedding.Values;
                }
            }
            catch
            {
                // Fallback to Mock if Gemini call fails
            }
        }

        // Mock / Fallback bag-of-words vectorizer (384 dimensions)
        return GenerateKeywordVector(text);
    }

    public double CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA == null || vectorB == null || vectorA.Length != vectorB.Length)
            return 0.0;

        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }

        if (normA == 0.0 || normB == 0.0)
            return 0.0;

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    // Helper: bag-of-words keyword frequency vector mapping
    private static float[] GenerateKeywordVector(string text)
    {
        var vector = new float[384];
        var words = text.ToLowerInvariant()
            .Split(new[] { ' ', '.', ',', ';', ':', '-', '_', '(', ')', '[', ']', '{', '}', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        // Predefined list of key technical terms to seed dimensions
        var vocabulary = new[]
        {
            "c#", "dotnet", ".net", "asp", "core", "mvc", "api", "rest", "graphql", "grpc", "microservices",
            "ef", "entity", "framework", "sql", "server", "postgresql", "mysql", "sqlite", "mongodb",
            "redis", "nosql", "vector", "search", "docker", "kubernetes", "k8s", "aws", "azure", "gcp",
            "ci", "cd", "pipelines", "git", "github", "gitlab", "devops", "jenkins", "terraform", "ansible",
            "javascript", "typescript", "react", "angular", "vue", "node", "express", "next", "html", "css",
            "tailwind", "sass", "bootstrap", "jquery", "python", "django", "flask", "fastapi", "numpy", "pandas",
            "java", "spring", "boot", "hibernate", "c++", "c", "rust", "go", "golang", "ruby", "rails", "php",
            "laravel", "security", "jwt", "oauth", "auth", "login", "encryption", "https", "ssl", "tls",
            "testing", "unit", "integration", "nunit", "xunit", "mstest", "mock", "jest", "cypress", "playwright",
            "agile", "scrum", "kanban", "jira", "confluence", "trello", "architecture", "clean", "onion", "ddd",
            "cqrs", "mediator", "mediatr", "solid", "dry", "kiss", "yagni", "pattern", "repository", "unitofwork",
            "dependency", "injection", "di", "ioc", "logging", "serilog", "nlog", "log4net", "monitoring",
            "prometheus", "grafana", "elk", "kibana", "splunk", "analytics", "dashboard", "metrics", "kpi",
            "cloud", "lambda", "functions", "serverless", "s3", "blob", "storage", "sqs", "sns", "rabbit",
            "mq", "kafka", "event", "driven", "bus", "masstransit", "hangfire", "quartz", "scheduler",
            "pdf", "docx", "word", "excel", "parse", "extract", "ocr", "ai", "llm", "gpt", "openai", "claude",
            "gemini", "ollama", "llama", "mistral", "gemma", "embedding", "similarity", "cosine", "match",
            "rank", "score", "candidate", "resume", "cv", "job", "description", "recruiter", "screening",
            "interview", "questions", "feedback", "strengths", "missing", "skills", "ats", "improvements",
            "experience", "years", "education", "bachelor", "master", "phd", "university", "college", "degree",
            "project", "certification", "developer", "engineer", "architect", "lead", "senior", "junior", "mid"
        };

        var wordCounts = new Dictionary<string, int>();
        foreach (var word in words)
        {
            if (wordCounts.ContainsKey(word))
                wordCounts[word]++;
            else
                wordCounts[word] = 1;
        }

        // Fill vector dimensions
        for (int i = 0; i < vector.Length; i++)
        {
            if (i < vocabulary.Length)
            {
                var term = vocabulary[i];
                wordCounts.TryGetValue(term, out var count);
                vector[i] = count * 10f; // Give higher weight to matches in vocabulary
            }
            else
            {
                // Fill remainder of 384 dimensions with character code hashes to ensure uniqueness for non-vocab words
                vector[i] = (float)Math.Abs(text.GetHashCode() % (i + 1)) / (i + 1);
            }
        }

        // Normalize vector
        double sum = vector.Sum(v => v * v);
        if (sum > 0.0)
        {
            float norm = (float)Math.Sqrt(sum);
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] /= norm;
            }
        }

        return vector;
    }
}

// Ollama deserializer models
public class OllamaEmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public float[]? Embedding { get; set; }
}

// Gemini deserializer models
public class GeminiEmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public GeminiEmbeddingValue? Embedding { get; set; }
}

public class GeminiEmbeddingValue
{
    [JsonPropertyName("values")]
    public float[]? Values { get; set; }
}
