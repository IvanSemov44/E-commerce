-- Phase 11 PR2 - Shopping dedicated database bootstrap
-- Run against target PostgreSQL instance as a privileged user.

CREATE DATABASE "ECommerceShoppingDb";

-- Optional role/bootstrap examples (adapt to environment policy)
-- CREATE USER ecommerce_shopping WITH PASSWORD '<strong-password>';
-- GRANT ALL PRIVILEGES ON DATABASE "ECommerceShoppingDb" TO ecommerce_shopping;
