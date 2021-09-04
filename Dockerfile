FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

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
 