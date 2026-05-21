# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the solution file and restore project dependencies
COPY *.sln ./
COPY src/ResumeReviewer.Domain/*.csproj ./src/ResumeReviewer.Domain/
COPY src/ResumeReviewer.Application/*.csproj ./src/ResumeReviewer.Application/
COPY src/ResumeReviewer.Infrastructure/*.csproj ./src/ResumeReviewer.Infrastructure/
COPY src/ResumeReviewer.WebAPI/*.csproj ./src/ResumeReviewer.WebAPI/

RUN dotnet restore

# Copy all source files and publish the release build
COPY . ./
WORKDIR /app/src/ResumeReviewer.WebAPI
RUN dotnet publish -c Release -o /app/out

# Use the official .NET 8 runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Create the uploads folder inside the runtime container
RUN mkdir -p uploads

# Expose ports
EXPOSE 8080
EXPOSE 80

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "ResumeReviewer.WebAPI.dll"]
