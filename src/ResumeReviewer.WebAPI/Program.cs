using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ResumeReviewer.Application;
using ResumeReviewer.Application.Interfaces;
using ResumeReviewer.Domain.Entities;
using ResumeReviewer.Domain.Enums;
using ResumeReviewer.Domain.Interfaces;
using ResumeReviewer.Infrastructure;
using ResumeReviewer.Infrastructure.Persistence;
using ResumeReviewer.Infrastructure.Security;
using ResumeReviewer.WebAPI.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Setup Serilog structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Add services to the container
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 3. Add Authentication & JWT Bearer
var jwtSection = builder.Configuration.GetSection(JwtOptions.Position);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
var key = Encoding.UTF8.GetBytes(jwtOptions.Secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 4. Setup Swagger UI with JWT Authorize padlock
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Resume Review and Screening API",
        Version = "v1",
        Description = "An AI-powered candidate screening and resume review system."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 5. Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 6. Configure Middlewares & Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Resume Review API v1");
    c.RoutePrefix = "swagger"; // Keep Swagger under /swagger
});

app.UseCors("AllowAll");

// Serve wwwroot static files for recruiter dashboard
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Setup Hangfire Dashboard (Allow anonymous for portfolio/demo convenience)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = Array.Empty<Hangfire.Dashboard.IDashboardAuthorizationFilter>()
});

app.MapControllers();

// 7. Initialize Database and Seed Default Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        
        // Ensure Database is created (works out-of-the-box for PostgreSQL, SqlServer, and SQLite)
        if (dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var creator = dbContext.Database.GetService<IRelationalDatabaseCreator>();
            if (creator != null)
            {
                if (!await creator.ExistsAsync())
                {
                    await creator.CreateAsync();
                }
                try
                {
                    await creator.CreateTablesAsync();
                }
                catch (Exception tableEx)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning(tableEx, "CreateTablesAsync failed; tables might already exist or there was a schema error.");
                }
            }
        }
        else
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

        // Seed Data
        await SeedDataAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database migration or seeding.");
    }
}

app.Run();

// Seeding Helper Method
async Task SeedDataAsync(IServiceProvider services)
{
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = services.GetRequiredService<IPasswordHasher>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    // 1. Seed Recruiter User if empty
    if (!await dbContext.Users.AnyAsync())
    {
        var defaultRecruiter = new User
        {
            Username = "recruiter",
            Email = "recruiter@recruiter.com",
            PasswordHash = passwordHasher.HashPassword("Password123"),
            Role = UserRole.Recruiter
        };
        await dbContext.Users.AddAsync(defaultRecruiter);
        logger.LogInformation("Seeded default recruiter user: recruiter@recruiter.com / Password123");
    }

    // 2. Seed Jobs if empty
    if (!await dbContext.JobDescriptions.AnyAsync())
    {
        var embeddingService = services.GetRequiredService<IEmbeddingService>();

        var jobs = new List<JobDescription>
        {
            new()
            {
                Title = "Senior .NET Core Developer",
                Department = "Engineering",
                Location = "Remote / Boston, MA",
                Content = "We are looking for a Senior .NET Core Developer to join our team. The candidate will design and build highly scalable web applications and microservices using .NET 8, C#, EF Core, PostgreSQL, and Docker. Experience with JWT authentication, Hangfire background tasks, and Unit Testing using xUnit is highly desired. Knowledge of Azure/AWS is a plus.",
                RequiredSkills = new List<string> { "C#", ".NET", "ASP.NET Core", "EF Core", "PostgreSQL", "Docker", "Microservices", "JWT", "Hangfire" },
                ExperienceYearsMin = 5
            },
            new()
            {
                Title = "Full-Stack React Developer",
                Department = "Engineering",
                Location = "Hybrid / New York, NY",
                Content = "We are seeking a Full-Stack React Developer with experience in frontend architectures and backend REST APIs. The ideal candidate has deep expertise in JavaScript, TypeScript, React, HTML5, CSS3, TailwindCSS, and Node.js/Express or .NET Web APIs. Strong understanding of responsive designs, UI/UX aesthetics, and Redux/Context state management is required.",
                RequiredSkills = new List<string> { "React", "JavaScript", "TypeScript", "HTML", "CSS", "TailwindCSS", "Node.js", "REST APIs" },
                ExperienceYearsMin = 3
            }
        };

        foreach (var job in jobs)
        {
            try
            {
                // Generate embeddings for jobs during seed
                job.Embedding = await embeddingService.GetEmbeddingAsync(job.Title + "\n" + job.Content);
            }
            catch
            {
                // Degrade gracefully
            }
            await dbContext.JobDescriptions.AddAsync(job);
        }

        logger.LogInformation("Seeded default Job Descriptions.");
    }

    await dbContext.SaveChangesAsync();
}
