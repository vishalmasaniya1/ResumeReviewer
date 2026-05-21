namespace ResumeReviewer.Infrastructure.Security;

public class JwtOptions
{
    public const string Position = "Jwt";

    public string Secret { get; set; } = "SuperSecretKeyForAuthenticationChangeMeInProductionResumeReviewer";
    public string Issuer { get; set; } = "ResumeReviewerAPI";
    public string Audience { get; set; } = "ResumeReviewerClient";
    public int ExpiryMinutes { get; set; } = 120;
}
