# Docker Setup Guide

This guide will help you run the entire e-commerce application in Docker containers.

## Prerequisites

- Docker Desktop installed and running
- Docker Compose (usually included with Docker Desktop)
- At least 4GB of free disk space

## Quick Start

### Option 1: Start All Services

From the project root directory, run:

```bash
docker-compose up -d
```

This will:
1. Pull the necessary base images
2. Build the backend and frontend images
3. Create and start all containers (SQL Server, API, Frontend)
4. Apply database migrations automatically

### Option 2: Start with Logs (Recommended for First Time)

```bash
docker-compose up
```

This shows real-time logs from all services, helpful for debugging.

## Access the Application

Once all services are running:

- **Frontend (Storefront)**: http://localhost:5173
- **Backend API**: http://localhost:5000
- **API Swagger/OpenAPI**: http://localhost:5000/swagger
- **Database**: `localhost:1433` (SQL Server)

## Database Credentials

- **Server**: localhost
- **User ID**: sa
- **Password**: YourPassword123!
- **Database**: ECommerceDb

Connect with tools like:
- SQL Server Management Studio (SSMS)
- Azure Data Studio
- DBeaver

## Useful Commands

### View Running Containers

```bash
docker-compose ps
```

### View Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api
docker-compose logs -f frontend
docker-compose logs -f sqlserver
```

### Stop Services

```bash
docker-compose down
```

### Stop and Remove Data (Clean Slate)

```bash
docker-compose down -v
```

This removes volumes, so the database will be recreated on next start.

### Rebuild Images

If you make code changes:

```bash
docker-compose up --build
```

## Troubleshooting

### Database Connection Fails

1. Wait a few seconds - SQL Server takes time to start
2. Check database logs: `docker-compose logs sqlserver`
3. Ensure port 1433 is not in use by another service

### Frontend Not Loading

1. Wait 30+ seconds for build to complete
2. Clear browser cache (Ctrl+Shift+Delete)
3. Check frontend logs: `docker-compose logs frontend`

### API Not Responding

1. Check API logs: `docker-compose logs api`
2. Verify database is healthy: `docker-compose logs sqlserver`
3. Ensure port 5000 is available

### Port Already in Use

If ports are already in use, edit `docker-compose.yml` and change:

```yaml
ports:
  - "5173:5173"  # Change first 5173 to another port
  - "5000:5000"  # Change first 5000 to another port
  - "1433:1433"  # Change first 1433 to another port
```

## Architecture

The Docker setup includes:

1. **SQL Server** (Port 1433)
   - Database for the application
   - Auto-created on first run
   - Data persisted in Docker volume

2. **Backend API** (Port 5000)
   - ASP.NET Core application
   - Automatically runs database migrations
   - Depends on SQL Server being healthy

3. **Frontend** (Port 5173)
   - React + Vite application
   - Built and served as static files
   - Depends on API being available

## Environment Variables

All critical variables are set in `docker-compose.yml`. To customize:

1. Edit `docker-compose.yml`
2. Rebuild: `docker-compose up --build`

## Performance Tips

- **First Start**: May take 2-3 minutes (building images, SQL Server initialization)
- **Subsequent Starts**: ~10-15 seconds
- **Memory**: Allocate at least 4GB to Docker
- **Disk Space**: Ensure 5GB free for images and data

## Production Deployment

This Docker setup is for **local development only**.

For production:
- Use Render.com as documented in [ARCHITECTURE_PLAN.md](ARCHITECTURE_PLAN.md)
- Implement proper secrets management
- Use environment-specific configurations
- Add monitoring and logging
- Configure proper backup strategies

## Next Steps

1. Verify all services are running: `docker-compose ps`
2. Check API health: `curl http://localhost:5000/health`
3. Visit frontend: `http://localhost:5173`
4. Try logging in or viewing products

For more details, see:
- [README.md](README.md) - Project overview
- [ARCHITECTURE_PLAN.md](ARCHITECTURE_PLAN.md) - Complete architecture
- [IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md) - Current progress
