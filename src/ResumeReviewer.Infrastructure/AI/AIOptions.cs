namespace ResumeReviewer.Infrastructure.AI;

public class AIOptions
{
    public const string Position = "AI";

    public string Provider { get; set; } = "Mock"; // Ollama, Gemini, Mock
    public string OllamaUrl { get; set; } = "http://localhost:11434";
    public string OllamaModel { get; set; } = "llama3";
    public string OllamaEmbeddingModel { get; set; } = "all-minilm";
    public string ApiKey { get; set; } = string.Empty;
    public string GeminiModel { get; set; } = "gemini-1.5-flash";
}
