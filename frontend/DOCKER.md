# Docker Setup Guide

This project includes Docker configuration for both production and development environments.

## Quick Start

### Production Build

Build and run the production image:

```bash
# Build the image
docker build -t swp-se1907-frontend:latest .

# Run the container
docker run -p 3000:3000 \
  -e NEXT_PUBLIC_API_URL=http://localhost:8000/api \
  swp-se1907-frontend:latest
```

Or using docker-compose:

```bash
# Start production service
docker-compose up -d frontend

# View logs
docker-compose logs -f frontend

# Stop service
docker-compose down frontend
```

### Development Build

Start development environment with hot reload:

```bash
# Using docker-compose with dev profile
docker-compose --profile dev up -d frontend-dev

# View logs
docker-compose logs -f frontend-dev

# Stop service
docker-compose down
```

Or manual Docker command:

```bash
docker build -f Dockerfile.dev -t swp-se1907-frontend:dev .
docker run -p 3001:3000 \
  -v $(pwd):/app \
  -v /app/node_modules \
  -v /app/.next \
  -e NODE_ENV=development \
  -e NEXT_PUBLIC_API_URL=http://localhost:8000/api \
  swp-se1907-frontend:dev
```

## Environment Variables

Set environment variables in docker-compose.yml or when running containers:

```bash
# Frontend API URL (required)
NEXT_PUBLIC_API_URL=http://localhost:8000/api

# Node environment
NODE_ENV=production|development

# Port (default: 3000)
PORT=3000
```

## Available Services

### `frontend` (Production)
- Image: Built from Dockerfile (multi-stage)
- Port: 3000
- Environment: production
- Auto-restart: unless-stopped
- Healthcheck: Enabled

### `frontend-dev` (Development - Optional)
- Image: Built from Dockerfile.dev
- Port: 3001
- Environment: development
- Volume mounts: For hot reload
- Profile: dev (use `--profile dev` to enable)

## Docker Commands

```bash
# Build all services
docker-compose build

# Start services
docker-compose up -d

# View logs
docker-compose logs -f

# Check service status
docker-compose ps

# Stop services
docker-compose stop

# Remove services and volumes
docker-compose down -v

# Execute command in running container
docker-compose exec frontend npm run build

# View service logs
docker-compose logs frontend

# Rebuild specific service
docker-compose build --no-cache frontend
```

## Troubleshooting

### Port already in use
If port 3000 is already in use:

```bash
# Change port in docker-compose.yml
ports:
  - "3001:3000"  # Maps host 3001 to container 3000
```

### Clear Docker cache
```bash
docker system prune -a
docker-compose build --no-cache
```

### Check image size
```bash
docker images swp-se1907-frontend
```

Production image is optimized with multi-stage build (typically ~200-300MB).

### Network connectivity
If container can't reach backend API:

```bash
# Ensure both services are on same network
docker-compose ps

# Check network
docker network ls
docker network inspect swp-network
```

## Production Deployment

For production deployment:

1. Update `NEXT_PUBLIC_API_URL` to your production API URL
2. Use secrets management for sensitive variables:

```bash
docker run -p 3000:3000 \
  --env-file .env.production \
  swp-se1907-frontend:latest
```

3. Use Docker Compose with environment files:

```yaml
frontend:
  env_file: .env.production
```

## Healthcheck

The container includes a healthcheck that verifies the service is running:

```bash
# View healthcheck status
docker-compose ps

# Manual healthcheck
curl http://localhost:3000

# Container health: (healthy|unhealthy|starting)
```

## Multi-architecture Builds

Build for multiple architectures (arm64, amd64):

```bash
docker buildx build --platform linux/amd64,linux/arm64 \
  -t swp-se1907-frontend:latest \
  --push .
```

## Notes

- Production image uses multi-stage build for small size
- Development image includes volume mounts for hot reload
- Both images use Node 18 Alpine (lightweight)
- Network is bridged for inter-service communication
- Use `.dockerignore` to exclude unnecessary files
