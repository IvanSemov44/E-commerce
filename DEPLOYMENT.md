# Deployment Guide

This document provides instructions for deploying the E-Commerce platform to a production environment using Docker and Docker Compose.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Step 1: Security Hardening (Crucial)](#step-1-security-hardening-crucial)
  - [Using Environment Variables](#using-environment-variables)
  - [Modifying `docker-compose.yml`](#modifying-docker-composeyml)
- [Step 2: Server Setup](#step-2-server-setup)
- [Step 3: Deploying the Application](#step-3-deploying-the-application)
- [Step 4: Setting up a Reverse Proxy (Recommended)](#step-4-setting-up-a-reverse-proxy-recommended)
  - [Example: Nginx Configuration](#example-nginx-configuration)
- [Maintenance](#maintenance)
  - [Updating the Application](#updating-the-application)
  - [Viewing Logs](#viewing-logs)

## Prerequisites

-   A server (e.g., a VPS from any cloud provider) running a modern Linux distribution.
-   **Docker** and **Docker Compose** installed on the server.
-   **Git** installed on the server.
-   A domain name pointed at your server's IP address.

---

## Step 1: Security Hardening (Crucial)

The default `docker-compose.yml` file is designed for development and is **NOT secure for production**. It contains hardcoded secrets. We will modify it to use a `.env` file, which should **never** be committed to version control.

### Using Environment Variables

Create a file named `.env` in the root of the project on your production server. This file will hold all your secrets.

**`.env` file template:**
```bash
# PostgreSQL Credentials
# Use a strong, randomly generated password
POSTGRES_USER=ecommerce
POSTGRES_DB=ECommerceDb
POSTGRES_PASSWORD=YOUR_STRONG_RANDOM_PASSWORD

# JWT Secret
# Use a long, random string (e.g., from a password generator)
# Must be at least 32 characters long
JWT_SECRET_KEY=YOUR_SUPER_SECRET_RANDOM_JWT_KEY_32_PLUS_CHARACTERS

# ASP.NET Core Environment
ASPNETCORE_ENVIRONMENT=Production
```
Fill this file with your own secure, randomly generated values.

### Modifying `docker-compose.yml`

On your server, you need to modify the `docker-compose.yml` file to read its secrets from the `.env` file you just created. Replace the `environment` sections for the `postgres` and `api` services with the following:

**For the `postgres` service:**
```yaml
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${POSTGRES_DB}
```

**For the `api` service:**
```yaml
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
      Jwt__SecretKey: "${JWT_SECRET_KEY}"
      Jwt__Issuer: "ecommerce-api"
      Jwt__Audience: "ecommerce-client"
      Jwt__ExpireMinutes: "60"
      ASPNETCORE_ENVIRONMENT: "${ASPNETCORE_ENVIRONMENT}"
      ASPNETCORE_URLS: "http://+:5000"
```

---

## Step 2: Server Setup

1.  SSH into your production server.
2.  Clone the repository:
    ```sh
    git clone https://github.com/your-repo/E-commerce.git
    cd E-commerce
    ```
3.  Create the `.env` file as described in the security section above.
4.  Modify the `docker-compose.yml` file as described above.

## Step 3: Deploying the Application

With the configuration complete, you can now launch the application.

1.  **Build and run the containers in detached mode:**
    ```sh
    docker-compose up --build -d
    ```
2.  **Verify the containers are running:**
    ```sh
    docker-compose ps
    ```
    All services should show a status of `Up` or `running`. The application is now running, but it's only accessible directly via IP and port, which is not ideal for production.

## Step 4: Setting up a Reverse Proxy (Recommended)

A reverse proxy (like Nginx) is essential for a production environment. It sits in front of your application and provides several benefits:
-   **SSL Termination:** Manages HTTPS, encrypting traffic between users and your server.
-   **Load Balancing:** Can distribute traffic if you scale services horizontally.
-   **Routing:** Directs traffic to the correct application container based on the domain name.

### Example: Nginx Configuration

This is a basic example of how to configure Nginx to route traffic to your storefront and API. You would also use a tool like **Certbot** to get a free SSL certificate from Let's Encrypt.

Create a file at `/etc/nginx/sites-available/ecommerce`:
```nginx
# Redirect HTTP to HTTPS
server {
    listen 80;
    server_name yourdomain.com;
    return 301 https://$host$request_uri;
}

# Main server block for the storefront
server {
    listen 443 ssl;
    server_name yourdomain.com;

    # SSL configuration (paths from Certbot)
    ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;

    location / {
        proxy_pass http://localhost:5173; # Route to storefront container
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Route API traffic
    location /api/ {
        proxy_pass http://localhost:5000; # Route to backend API container
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```
*Note: The Admin app is not exposed in this example, as it's often kept internal or placed on a separate subdomain with restricted access.*

## Maintenance

### Updating the Application

To update your application with the latest changes from your Git repository:
```sh
# Pull the latest code
git pull

# Rebuild and restart the containers
docker-compose up --build -d
```

### Viewing Logs

To view the logs from all running services:
```sh
docker-compose logs -f
```
To view logs for a specific service (e.g., the `api`):
```sh
docker-compose logs -f api
```
