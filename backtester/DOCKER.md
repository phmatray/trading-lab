# Docker Deployment Guide

This guide explains how to deploy the TradingStrat web application using Docker.

## Prerequisites

- Docker Engine 20.10 or later
- Docker Compose v2.0 or later

## Quick Start

### Using Docker Compose (Recommended)

1. **Set your Anthropic API key** (required for AI Trading Assistant):

```bash
# Option 1: Export as environment variable (recommended)
export ANTHROPIC_API_KEY="sk-ant-api03-YOUR_KEY_HERE"

# Option 2: Create a .env file in the project root
echo "ANTHROPIC_API_KEY=sk-ant-api03-YOUR_KEY_HERE" > .env
```

2. **Build and run** the application:

```bash
docker-compose up -d
```

The application will be available at `http://localhost:8080`

**Note:** The `.env` file is already included in `.gitignore` to prevent accidentally committing your API key.

### Using Docker CLI

Build the image:

```bash
docker build -t tradingstrat-web:latest .
```

Run the container:

```bash
docker run -d \
  --name tradingstrat-web \
  -p 8080:8080 \
  -v tradingstrat-data:/app/data \
  -v tradingstrat-logs:/app/logs \
  -v tradingstrat-exports:/app/exports \
  tradingstrat-web:latest
```

## Architecture

The Dockerfile uses a **multi-stage build** for optimal image size and build performance:

1. **Node.js Stage** - Builds Tailwind CSS v4 (configured via CSS imports, no tailwind.config.js)
2. **Build Stage** - Compiles the .NET application
3. **Runtime Stage** - Creates minimal runtime image (~200MB)

**Note:** The project uses Tailwind CSS v4, which doesn't require a traditional `tailwind.config.js` file. Configuration is embedded in the CSS file itself (`Styles/app.css`).

## Data Persistence

The application uses three Docker volumes for data persistence:

| Volume | Purpose | Container Path |
|--------|---------|----------------|
| `tradingstrat-data` | SQLite database | `/app/data` |
| `tradingstrat-logs` | Application logs | `/app/logs` |
| `tradingstrat-exports` | CSV/JSON exports | `/app/exports` |

### Accessing Persisted Data

View volume contents:

```bash
# List all volumes
docker volume ls

# Inspect volume location
docker volume inspect tradingstrat-data

# Access data directly
docker run --rm -v tradingstrat-data:/data alpine ls -la /data
```

### Backup Data

Backup the database:

```bash
docker run --rm \
  -v tradingstrat-data:/source:ro \
  -v $(pwd):/backup \
  alpine tar czf /backup/tradingstrat-data-backup.tar.gz -C /source .
```

Restore from backup:

```bash
docker run --rm \
  -v tradingstrat-data:/target \
  -v $(pwd):/backup \
  alpine sh -c "cd /target && tar xzf /backup/tradingstrat-data-backup.tar.gz"
```

## Configuration

### Environment Variables

Override configuration via environment variables:

```bash
docker run -d \
  -e Trading__DefaultTicker=AAPL \
  -e Trading__Backtest__InitialCapital=50000 \
  -e Trading__Assistant__ApiKey=sk-ant-api03-... \
  -e ASPNETCORE_ENVIRONMENT=Development \
  tradingstrat-web:latest
```

**Important Environment Variables:**

| Variable | Description | Default |
|----------|-------------|---------|
| `Trading__Assistant__ApiKey` | Anthropic API key for AI Trading Assistant | Required |
| `Trading__Database__ConnectionString` | SQLite database path | `Data Source=/app/data/trading.db` |
| `Trading__DefaultTicker` | Default stock ticker | `AAPL` |
| `Trading__Backtest__InitialCapital` | Starting capital for backtests | `100000` |
| `ASPNETCORE_ENVIRONMENT` | Environment mode | `Production` |
| `ASPNETCORE_URLS` | Listening URLs | `http://+:8080` |

### Custom Configuration File

Mount a custom `appsettings.Production.json`:

1. Create `appsettings.Production.json` with your settings
2. Uncomment the volume mount in `docker-compose.yml`:
   ```yaml
   volumes:
     - ./appsettings.Production.json:/app/appsettings.Production.json:ro
   ```
3. Restart: `docker-compose up -d`

## Common Operations

### View Logs

```bash
# Real-time logs
docker-compose logs -f

# Last 100 lines
docker-compose logs --tail=100

# Specific service
docker logs tradingstrat-web
```

### Stop/Start

```bash
# Stop
docker-compose stop

# Start
docker-compose start

# Restart
docker-compose restart
```

### Update to Latest Version

```bash
# Pull latest code, rebuild, and restart
git pull
docker-compose up -d --build
```

### Clean Up

```bash
# Stop and remove containers
docker-compose down

# Remove containers and volumes (⚠️ deletes data)
docker-compose down -v

# Remove unused images
docker image prune -a
```

## Troubleshooting

### Container Won't Start

Check logs:
```bash
docker-compose logs tradingstrat-web
```

Common issues:
- Port 8080 already in use → Change port in `docker-compose.yml`
- Database migration failed → Check logs and volume permissions

### Health Check Failing

**Note:** The current Dockerfile includes a health check endpoint at `/health`. If this endpoint doesn't exist in your application, you have two options:

1. **Remove health check** from Dockerfile and docker-compose.yml
2. **Add health check endpoint** to Program.cs:

```csharp
// Add before app.Run()
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
```

### Performance Issues

Increase container resources in Docker Desktop settings or add resource limits:

```yaml
services:
  tradingstrat-web:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          cpus: '1'
          memory: 512M
```

### Database Locked

SQLite doesn't handle concurrent writes well. If you see "database is locked" errors:
- Ensure only one container instance is running
- Consider PostgreSQL for production (requires Infrastructure changes)

## Production Deployment

### HTTPS/TLS

For production, use a reverse proxy (nginx/Traefik) for TLS termination:

```yaml
services:
  tradingstrat-web:
    expose:
      - 8080
    # Don't expose ports directly

  nginx:
    image: nginx:alpine
    ports:
      - "443:443"
      - "80:80"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./certs:/etc/nginx/certs:ro
```

### Security Best Practices

1. **Run as non-root user** (add to Dockerfile):
   ```dockerfile
   RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
   USER appuser
   ```

2. **Use secrets for sensitive data** instead of environment variables
3. **Enable HTTPS** via reverse proxy
4. **Scan images** for vulnerabilities: `docker scan tradingstrat-web`
5. **Keep base images updated**: Rebuild regularly with latest .NET runtime

### Monitoring

Add monitoring with Prometheus/Grafana or use Docker stats:

```bash
# Real-time resource usage
docker stats tradingstrat-web

# Resource usage over time
docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}"
```

## Building for Different Platforms

Build for ARM64 (e.g., Raspberry Pi, AWS Graviton):

```bash
docker buildx build --platform linux/arm64 -t tradingstrat-web:arm64 .
```

Build multi-platform image:

```bash
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -t tradingstrat-web:latest \
  --push \
  .
```

## Next Steps

- Set up CI/CD pipeline (GitHub Actions, GitLab CI)
- Deploy to cloud (Azure Container Instances, AWS ECS, Google Cloud Run)
- Add Redis for session management in multi-instance deployments
- Implement distributed caching for better performance
- Set up log aggregation (ELK stack, Grafana Loki)