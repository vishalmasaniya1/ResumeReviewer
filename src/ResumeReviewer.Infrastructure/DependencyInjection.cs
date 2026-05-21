using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResumeReviewer.Application.Interfaces;
using ResumeReviewer.Domain.Interfaces;
using ResumeReviewer.Infrastructure.AI;
using ResumeReviewer.Infrastructure.BackgroundJobs;
using ResumeReviewer.Infrastructure.Parsers;
using ResumeReviewer.Infrastructure.Persistence;
using ResumeReviewer.Infrastructure.Persistence.Repositories;
using ResumeReviewer.Infrastructure.Security;

namespace ResumeReviewer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Options Bindings
        services.Configure<AIOptions>(configuration.GetSection(AIOptions.Position));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Position));

        // 2. Database Context Setup (PostgreSQL vs SqlServer vs SQLite)
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var provider = configuration.GetValue<string>("DatabaseProvider") ?? "SQLite";

        if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            connectionString = ConvertPostgreSqlUriToConnectionString(connectionString);
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString, b => b.MigrationsAssembly("ResumeReviewer.WebAPI")));
        }
        else if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString, b => b.MigrationsAssembly("ResumeReviewer.WebAPI")));
        }
        else
        {
            // Fallback to SQLite (portable, zero-configuration)
            var sqliteConnection = connectionString ?? "Data Source=resumereviewer.db";
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(sqliteConnection, b => b.MigrationsAssembly("ResumeReviewer.WebAPI")));
        }

        // 3. Repositories Registration
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<ICandidateRepository, CandidateRepository>();
        services.AddScoped<IResumeRepository, ResumeRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();

        // 4. Parsers Registration
        services.AddSingleton<IPdfParser, PdfParser>();
        services.AddSingleton<IDocxParser, DocxParser>();

        // 5. Security Services
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // 6. AI Services
        services.AddHttpClient<IEmbeddingService, EmbeddingService>();
        services.AddHttpClient<ILLMService, LLMService>();

        // 7. Hangfire Background Jobs setup
        if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(connectionString))
        {
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));
        }
        else if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(connectionString))
        {
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(connectionString));
        }
        else
        {
            // Default to portable memory storage for SQLite fallback
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseMemoryStorage());
        }

        services.AddHangfireServer();
        services.AddScoped<IBackgroundJobDispatcher, BackgroundJobDispatcher>();

        return services;
    }

    private static string? ConvertPostgreSqlUriToConnectionString(string? uriString)
    {
        if (string.IsNullOrEmpty(uriString) || (!uriString.StartsWith("postgres://") && !uriString.StartsWith("postgresql://")))
        {
            return uriString;
        }

        try
        {
            var uri = new System.Uri(uriString);
            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo[0];
            var password = userInfo.Length > 1 ? userInfo[1] : "";
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');

            return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
        }
        catch
        {
            return uriString;
        }
    }
}
