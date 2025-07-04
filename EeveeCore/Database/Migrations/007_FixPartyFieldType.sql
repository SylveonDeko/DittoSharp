-- Migration to fix Party field type from integer[] to bigint[]
-- This fixes the type mismatch between database (int[]) and C# model (ulong[])

ALTER TABLE public.users 
ALTER COLUMN party TYPE bigint[] 
USING party::bigint[];