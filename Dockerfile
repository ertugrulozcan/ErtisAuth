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
COPY ["ErtisAuth.Abstractions/ErtisAuth.Abstractions.csproj", "ErtisAuth.Abstractions/"]
COPY ["ErtisAuth.Analyzers/ErtisAuth.Analyzers.csproj", "ErtisAuth.Analyzers/"]
COPY ["ErtisAuth.Core/ErtisAuth.Core.csproj", "ErtisAuth.Core/"]
COPY ["ErtisAuth.Dao/ErtisAuth.Dao.csproj", "ErtisAuth.Dao/"]
COPY ["ErtisAuth.Extensions.ApplicationInsights/ErtisAuth.Extensions.ApplicationInsights.csproj", "ErtisAuth.Extensions.ApplicationInsights/"]
COPY ["ErtisAuth.Extensions.AspNetCore/ErtisAuth.Extensions.AspNetCore.csproj", "ErtisAuth.Extensions.AspNetCore/"]
COPY ["ErtisAuth.Extensions.Authorization/ErtisAuth.Extensions.Authorization.csproj", "ErtisAuth.Extensions.Authorization/"]
COPY ["ErtisAuth.Extensions.Database/ErtisAuth.Extensions.Database.csproj", "ErtisAuth.Extensions.Database/"]
COPY ["ErtisAuth.Extensions.Mailing/ErtisAuth.Extensions.Mailing.csproj", "ErtisAuth.Extensions.Mailing/"]
COPY ["ErtisAuth.Extensions.Prometheus/ErtisAuth.Extensions.Prometheus.csproj", "ErtisAuth.Extensions.Prometheus/"]
COPY ["ErtisAuth.Infrastructure/ErtisAuth.Infrastructure.csproj", "ErtisAuth.Infrastructure/"]
COPY ["ErtisAuth.Integrations.OAuth/ErtisAuth.Integrations.OAuth.csproj", "ErtisAuth.Integrations.OAuth/"]
COPY ["ErtisAuth.Integrations.OAuth.Abstractions/ErtisAuth.Integrations.OAuth.Abstractions.csproj", "ErtisAuth.Integrations.OAuth.Abstractions/"]
COPY ["ErtisAuth.Integrations.OAuth.Apple/ErtisAuth.Integrations.OAuth.Apple.csproj", "ErtisAuth.Integrations.OAuth.Apple/"]
COPY ["ErtisAuth.Integrations.OAuth.Core/ErtisAuth.Integrations.OAuth.Core.csproj", "ErtisAuth.Integrations.OAuth.Core/"]
COPY ["ErtisAuth.Integrations.OAuth.Facebook/ErtisAuth.Integrations.OAuth.Facebook.csproj", "ErtisAuth.Integrations.OAuth.Facebook/"]
COPY ["ErtisAuth.Integrations.OAuth.Google/ErtisAuth.Integrations.OAuth.Google.csproj", "ErtisAuth.Integrations.OAuth.Google/"]
COPY ["ErtisAuth.Integrations.OAuth.Microsoft/ErtisAuth.Integrations.OAuth.Microsoft.csproj", "ErtisAuth.Integrations.OAuth.Microsoft/"]
COPY ["ErtisAuth.WebAPI/ErtisAuth.WebAPI.csproj", "ErtisAuth.WebAPI/"]
RUN dotnet restore "ErtisAuth.WebAPI/ErtisAuth.WebAPI.csproj"

COPY . .
RUN dotnet publish "ErtisAuth.WebAPI/ErtisAuth.WebAPI.csproj" -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# The non-root user of the .NET images; it can't bind ports below 1024, hence 8080
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ErtisAuth.WebAPI.dll"]
