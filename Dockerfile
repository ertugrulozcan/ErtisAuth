# ErtisAuth WebAPI
#
#   docker build -t ertisauth:latest .
#   docker run -p 9716:8080 -e Database__ConnectionString=mongodb://<host>:27017 ertisauth:latest
#
# Debian based images (not Alpine): they ship ICU and tzdata, so culture, date and time zone handling behave as on a
# development machine (Alpine images run in globalization invariant mode without them).

# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# The project files first: the restore layer is cached until a project file changes
COPY ["global.json", "./"]
COPY ["src/ErtisAuth.Abstractions/ErtisAuth.Abstractions.csproj", "src/ErtisAuth.Abstractions/"]
COPY ["src/ErtisAuth.Analyzers/ErtisAuth.Analyzers.csproj", "src/ErtisAuth.Analyzers/"]
COPY ["src/ErtisAuth.Core/ErtisAuth.Core.csproj", "src/ErtisAuth.Core/"]
COPY ["src/ErtisAuth.Dao/ErtisAuth.Dao.csproj", "src/ErtisAuth.Dao/"]
COPY ["src/ErtisAuth.Extensions.ApplicationInsights/ErtisAuth.Extensions.ApplicationInsights.csproj", "src/ErtisAuth.Extensions.ApplicationInsights/"]
COPY ["src/ErtisAuth.Extensions.AspNetCore/ErtisAuth.Extensions.AspNetCore.csproj", "src/ErtisAuth.Extensions.AspNetCore/"]
COPY ["src/ErtisAuth.Extensions.Authorization/ErtisAuth.Extensions.Authorization.csproj", "src/ErtisAuth.Extensions.Authorization/"]
COPY ["src/ErtisAuth.Extensions.Database/ErtisAuth.Extensions.Database.csproj", "src/ErtisAuth.Extensions.Database/"]
COPY ["src/ErtisAuth.Extensions.Mailing/ErtisAuth.Extensions.Mailing.csproj", "src/ErtisAuth.Extensions.Mailing/"]
COPY ["src/ErtisAuth.Extensions.Prometheus/ErtisAuth.Extensions.Prometheus.csproj", "src/ErtisAuth.Extensions.Prometheus/"]
COPY ["src/ErtisAuth.Infrastructure/ErtisAuth.Infrastructure.csproj", "src/ErtisAuth.Infrastructure/"]
COPY ["src/ErtisAuth.Integrations.OAuth/ErtisAuth.Integrations.OAuth.csproj", "src/ErtisAuth.Integrations.OAuth/"]
COPY ["src/ErtisAuth.Integrations.OAuth.Abstractions/ErtisAuth.Integrations.OAuth.Abstractions.csproj", "src/ErtisAuth.Integrations.OAuth.Abstractions/"]
COPY ["src/ErtisAuth.Integrations.OAuth.Apple/ErtisAuth.Integrations.OAuth.Apple.csproj", "src/ErtisAuth.Integrations.OAuth.Apple/"]
COPY ["src/ErtisAuth.Integrations.OAuth.Core/ErtisAuth.Integrations.OAuth.Core.csproj", "src/ErtisAuth.Integrations.OAuth.Core/"]
COPY ["src/ErtisAuth.Integrations.OAuth.Facebook/ErtisAuth.Integrations.OAuth.Facebook.csproj", "src/ErtisAuth.Integrations.OAuth.Facebook/"]
COPY ["src/ErtisAuth.Integrations.OAuth.Google/ErtisAuth.Integrations.OAuth.Google.csproj", "src/ErtisAuth.Integrations.OAuth.Google/"]
COPY ["src/ErtisAuth.Integrations.OAuth.Microsoft/ErtisAuth.Integrations.OAuth.Microsoft.csproj", "src/ErtisAuth.Integrations.OAuth.Microsoft/"]
COPY ["src/ErtisAuth.WebAPI/ErtisAuth.WebAPI.csproj", "src/ErtisAuth.WebAPI/"]
RUN dotnet restore "src/ErtisAuth.WebAPI/ErtisAuth.WebAPI.csproj"

COPY . .
RUN dotnet publish "src/ErtisAuth.WebAPI/ErtisAuth.WebAPI.csproj" -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# The non-root user of the .NET images; it can't bind ports below 1024, hence 8080
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ErtisAuth.WebAPI.dll"]
