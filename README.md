# AI-Powered Resume Review and Candidate Screening System

A production-grade AI candidate screening and resume ranking platform built with **.NET 8 (Clean Architecture)**, **SQL Server / PostgreSQL / SQLite**, **Hangfire background queues**, and live **Google Gemini AI**. 

Features a modern, responsive Single Page Application (SPA) dashboard styled with a translucent glassmorphic dark UI, animated SVG charts, and interactive candidate profiling.

---

## 🚀 Key Features

*   **Dual AI Mode Parser**: Uses Google Gemini API (`gemini-2.5-flash`) for deep semantic profiling, candidate feedback, missing skills extraction, and personalized interview questions. Degrades gracefully to an offline regex-based ATS parser if API keys are not supplied.
*   **Asynchronous Background Worker**: Offloads resume extraction and LLM comparison onto Hangfire queues to maintain zero-latency user interactions.
*   **Clean Architecture Solution**: Separate projects for `Domain`, `Application`, `Infrastructure`, and `WebAPI` to enforce clean separation of concerns.
*   **Multi-Format Resume Support**: Parses text out of `.pdf`, `.docx`, and `.txt` files out-of-the-box.
*   **Vector Space Scoring**: Employs cosine similarity mapping to grade candidates semantically against target job descriptions.
*   **Interactive Recruiter UI**: Dark glassmorphic recruiter console featuring interactive analytics, drag-and-drop resume uploading, dynamic ranking pipelines, and side-by-side screening summaries.

---

## 🛠️ Technology Stack

*   **Backend**: .NET 8 Web API
*   **Core Libraries**: Entity Framework Core, FluentValidation, Serilog
*   **Background Jobs**: Hangfire (configured with SqlServer persistence / SQLite fallback)
*   **Database Providers**: SQL Server (primary), PostgreSQL, or SQLite
*   **Text Parsers**: PdfPig (PDF parsing), OpenXml (Word DOCX parsing)
*   **Frontend**: Vanilla HTML5, CSS3 (translucent HSL gradients, custom animations), and modular ES6 Javascript client

---

## ⚙️ Local Setup Guide

### 1. Prerequisites
- **.NET 8 SDK** installed.
- **SQL Server** instance running locally (e.g., `LAPTOP-S46DRE9C\SQLEXPRESS`).

### 2. Configuration (`appsettings.json`)
Configure your SQL Server connection details and Gemini API Key in `src/ResumeReviewer.WebAPI/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=LAPTOP-S46DRE9C\\SQLEXPRESS;Database=ResumeReviewerDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "DatabaseProvider": "SqlServer",
  "AI": {
    "Provider": "Gemini",
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "GeminiModel": "gemini-2.5-flash"
  }
}
```

### 3. Run the Application
Run the project using the CLI:
```bash
dotnet run --project src/ResumeReviewer.WebAPI/ResumeReviewer.WebAPI.csproj
```
The database tables will be automatically created on startup, and a default recruiter account along with job descriptions will be seeded.

### 4. Access the UI
Open your browser and navigate to:
- **Dashboard**: `http://localhost:5000/index.html` (or `http://localhost:5000`)
- **Interactive API Docs (Swagger)**: `http://localhost:5000/swagger`
- **Hangfire Worker Dashboard**: `http://localhost:5000/hangfire`

**Default Credentials**:
- **Email**: `recruiter@recruiter.com`
- **Password**: `Password123`

---

## 🐳 Cloud Deployment (Docker)

A multi-stage `Dockerfile` is included in the project root. You can deploy this entire application (frontend + Web API) to container hosting services like **Render** or **Railway**.

### Deployment Environment Variables:
- `DatabaseProvider` = `PostgreSQL` or `SqlServer`
- `ConnectionStrings__DefaultConnection` = `Your database url`
- `AI__ApiKey` = `Your Google Gemini API Key`
- `AI__Provider` = `Gemini`
- `AI__GeminiModel` = `gemini-2.5-flash`
