# Builds EventHub into a small production image.
# Normally used through docker-compose.yml:  docker compose up --build

# ---------- 1) Build: full .NET SDK ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore NuGet packages first, so this layer is cached until the .csproj changes.
COPY EventHub.csproj ./
RUN dotnet restore EventHub.csproj

# Copy the source and publish a Release build.
COPY . .
RUN dotnet publish EventHub.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- 2) Run: small ASP.NET Core runtime only ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# The app listens on port 8080 inside the container (docker-compose.yml maps it to localhost:5025).
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# Folder for the data-protection keys (a Docker volume is mounted here); owned by the non-root user.
RUN mkdir -p /app/keys && chown "$APP_UID" /app/keys

# Run as the image's built-in non-root user, not as root.
USER $APP_UID

ENTRYPOINT ["dotnet", "EventHub.dll"]
