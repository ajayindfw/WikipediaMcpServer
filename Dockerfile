# Use the official .NET 8 runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Use the official .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy global.json for SDK version consistency
COPY global.json ./

# Copy project file and restore dependencies
COPY ["src/WikipediaMcpServer/WikipediaMcpServer.csproj", "src/WikipediaMcpServer/"]
RUN dotnet restore "src/WikipediaMcpServer/WikipediaMcpServer.csproj"

# Copy the entire source code
COPY . .
WORKDIR "/src/src/WikipediaMcpServer"

# Build the application
RUN dotnet build "WikipediaMcpServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WikipediaMcpServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage - create the runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# List files for debugging (Railway logs will show this)
RUN ls -la /app/

# Set environment variables for Railway production deployment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT
ENV DOTNET_ENVIRONMENT=Production
ENV MCP_MODE=false
ENV ASPNETCORE_HTTPS_PORT=""
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Create a non-root user for security
RUN adduser --disabled-password --gecos '' --shell /bin/bash appuser && chown -R appuser /app
USER appuser

# Railway handles health checks at platform level - no Docker HEALTHCHECK needed
# The /health endpoint is available and Railway will check it directly

ENTRYPOINT ["dotnet", "WikipediaMcpServer.dll"]