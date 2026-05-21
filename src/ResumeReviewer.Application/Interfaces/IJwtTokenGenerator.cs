using ResumeReviewer.Domain.Entities;

namespace ResumeReviewer.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
