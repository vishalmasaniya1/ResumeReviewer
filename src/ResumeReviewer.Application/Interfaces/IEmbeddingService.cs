namespace ResumeReviewer.Application.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text);
    double CalculateCosineSimilarity(float[] vectorA, float[] vectorB);
}
