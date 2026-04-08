-- Phase 11 PR2 - Catalog dedicated database bootstrap
-- Run against target PostgreSQL instance as a privileged user.

CREATE DATABASE "ECommerceCatalogDb";

-- Optional role/bootstrap examples (adapt to environment policy)
-- CREATE USER ecommerce_catalog WITH PASSWORD '<strong-password>';
-- GRANT ALL PRIVILEGES ON DATABASE "ECommerceCatalogDb" TO ecommerce_catalog;
