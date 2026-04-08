-- Phase 11 PR2 - Row-count verification (source shared DB vs target dedicated DBs)
-- Execute each section in the corresponding database and compare results.

-- Shared source DB (legacy)
SELECT 'source.public.Categories' AS table_name, COUNT(*) AS row_count FROM public."Categories";
SELECT 'source.public.Products' AS table_name, COUNT(*) AS row_count FROM public."Products";
SELECT 'source.public.ProductImages' AS table_name, COUNT(*) AS row_count FROM public."ProductImages";
SELECT 'source.shopping.Carts' AS table_name, COUNT(*) AS row_count FROM shopping."Carts";
SELECT 'source.shopping.CartItems' AS table_name, COUNT(*) AS row_count FROM shopping."CartItems";
SELECT 'source.shopping.Wishlists' AS table_name, COUNT(*) AS row_count FROM shopping."Wishlists";

-- Catalog target DB
SELECT 'target.catalog.public.Categories' AS table_name, COUNT(*) AS row_count FROM public."Categories";
SELECT 'target.catalog.public.Products' AS table_name, COUNT(*) AS row_count FROM public."Products";
SELECT 'target.catalog.public.ProductImages' AS table_name, COUNT(*) AS row_count FROM public."ProductImages";

-- Shopping target DB
SELECT 'target.shopping.shopping.Carts' AS table_name, COUNT(*) AS row_count FROM shopping."Carts";
SELECT 'target.shopping.shopping.CartItems' AS table_name, COUNT(*) AS row_count FROM shopping."CartItems";
SELECT 'target.shopping.shopping.Wishlists' AS table_name, COUNT(*) AS row_count FROM shopping."Wishlists";
