# syntax=docker/dockerfile:1

# ---- build -------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the NuGet layer stays cached until a project file changes.
COPY global.json Directory.Build.props ./
COPY src/Seatline.Api/Seatline.Api.csproj src/Seatline.Api/
RUN dotnet restore src/Seatline.Api/Seatline.Api.csproj

COPY src/Seatline.Api/ src/Seatline.Api/
RUN dotnet publish src/Seatline.Api/Seatline.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---- runtime -----------------------------------------------------------------
# Chiseled = distroless Ubuntu: no shell, no package manager, runs as non-root.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Seatline.Api.dll"]
