FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY ["ErtisAuth.WebAPI/ErtisAuth.WebAPI.csproj", "ErtisAuth.WebAPI/"]
RUN dotnet restore "ErtisAuth.WebAPI/ErtisAuth.WebAPI.csproj"

COPY . .

WORKDIR "/src/ErtisAuth.WebAPI"
RUN dotnet build "ErtisAuth.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ErtisAuth.WebAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ErtisAuth.WebAPI.dll"]