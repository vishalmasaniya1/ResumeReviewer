using System.ComponentModel.DataAnnotations;

namespace ResumeReviewer.Application.DTOs;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record RegisterRequest(
    [Required] string Username,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password
);

public record AuthResponse(
    string Token,
    string Username,
    string Email,
    string Role
);
