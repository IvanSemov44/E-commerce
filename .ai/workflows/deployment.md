# Workflow: Deployment

Updated: 2026-03-08
Owner: @ivans

## Purpose
Deploy the application safely with Docker while keeping secrets out of version control.

## Prerequisites
- Linux server/VPS with Docker and Docker Compose.
- Git installed on server.
- Domain pointed to server IP.

## Security-First Rules
- Never deploy with hardcoded secrets from development compose settings.
- Use environment variables from a server-local `.env` file.
- JWT secret must be random and at least 32 characters.

## Required `.env` Variables (Server)
```bash
POSTGRES_USER=ecommerce
POSTGRES_DB=ECommerceDb
POSTGRES_PASSWORD=YOUR_STRONG_RANDOM_PASSWORD
JWT_SECRET_KEY=YOUR_SUPER_SECRET_RANDOM_JWT_KEY_32_PLUS_CHARACTERS
ASPNETCORE_ENVIRONMENT=Production
```

## Deploy Steps
1. SSH to server and clone/pull repository.
2. Create/update `.env` on server (do not commit).
3. Ensure docker-compose service env sections use `${...}` variables.
4. Build and start containers:
```bash
docker-compose up --build -d
```
5. Verify services:
```bash
docker-compose ps
```
6. Check logs if needed:
```bash
docker-compose logs -f
docker-compose logs -f api
```

## Reverse Proxy (Recommended)
- Put Nginx/Caddy in front of containers.
- Terminate TLS/HTTPS at proxy.
- Route `/` to storefront and `/api/` to backend API.

## Update Procedure
```bash
git pull
docker-compose up --build -d
```

## Real Code References
- Compose file: `docker-compose.yml`
- Deployment source guide: `DEPLOYMENT.md`
- Backend config validation: `src/backend/ECommerce.API/Extensions/ConfigurationExtensions.cs`

## Common Failure Modes
- Secrets committed to git.
- Missing `.env` values causing startup failure.
- API unreachable behind proxy due to bad route config.
- Containers up but unhealthy due to DB/config mismatch.
