# Multi-stage Dockerfile for TradingStrat.Web
# Stage 1: Node.js for Tailwind CSS build
FROM node:24-alpine AS node-builder

WORKDIR /src

# Copy package files and install dependencies
COPY src/TradingStrat.Web/package*.json ./TradingStrat.Web/
WORKDIR /src/TradingStrat.Web
RUN npm install

# Copy Tailwind source files (v4 doesn't use tailwind.config.js)
COPY src/TradingStrat.Web/Styles ./Styles

# Copy Razor components so Tailwind can scan for utility classes
COPY src/TradingStrat.Web/Components ./Components

# Create wwwroot/css directory for output
RUN mkdir -p ./wwwroot/css

# Build Tailwind CSS (now with access to all component files)
RUN npm run build:css

# Stage 2: .NET SDK for building the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy solution file and project files
COPY TradingStrat.slnx ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./

# Copy all project files for restore
COPY src/TradingStrat.Domain/TradingStrat.Domain.csproj ./src/TradingStrat.Domain/
COPY src/TradingStrat.Application/TradingStrat.Application.csproj ./src/TradingStrat.Application/
COPY src/TradingStrat.Infrastructure/TradingStrat.Infrastructure.csproj ./src/TradingStrat.Infrastructure/
COPY src/TradingStrat.Web/TradingStrat.Web.csproj ./src/TradingStrat.Web/

# Restore dependencies
RUN dotnet restore ./src/TradingStrat.Web/TradingStrat.Web.csproj

# Copy all source code
COPY src/ ./src/

# Copy the built Tailwind CSS from node-builder stage
COPY --from=node-builder /src/TradingStrat.Web/wwwroot/css/app.css ./src/TradingStrat.Web/wwwroot/css/

# Build and publish the application
WORKDIR /src/src/TradingStrat.Web
RUN dotnet publish -c Release -o /app/publish

# Stage 3: .NET Runtime for running the application
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Create directories for data persistence
RUN mkdir -p /app/data /app/logs /app/exports

# Copy published application
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TradingStrat.Web.dll"]