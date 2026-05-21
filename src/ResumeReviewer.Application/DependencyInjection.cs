using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ResumeReviewer.Application.Interfaces;
using ResumeReviewer.Application.Services;
using ResumeReviewer.Application.Validators;

namespace ResumeReviewer.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register business services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IResumeService, ResumeService>();
        services.AddScoped<IMatchService, MatchService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        // Register FluentValidation validators
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

        return services;
    }
}
