# Build stage
FROM node:22-alpine AS build
WORKDIR /app

# Copy admin project files and shared types (maintaining directory structure)
COPY admin ./admin
COPY shared ./shared

# Set working directory to admin
WORKDIR /app/admin

# Install dependencies
RUN npm ci

# Build the application
RUN npm run build

# Production stage
FROM node:22-alpine
WORKDIR /app

# Install serve to run the static build
RUN npm install -g serve

# Copy built app from build stage
COPY --from=build /app/admin/dist ./dist

# Expose port
EXPOSE 5177

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://localhost:5177/ || exit 1

# Start the app
CMD ["serve", "-s", "dist", "-l", "5177"]
