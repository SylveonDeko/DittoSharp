-- Migration: Fix ActiveSpawns table to use MessageId as primary key
-- This migration changes the primary key from Id to MessageId

-- Drop the existing primary key constraint
ALTER TABLE "ActiveSpawns" DROP CONSTRAINT "ActiveSpawns_pkey";

-- Drop the Id column (if it exists)
ALTER TABLE "ActiveSpawns" DROP COLUMN IF EXISTS "Id";

-- Add the new primary key constraint on MessageId
ALTER TABLE "ActiveSpawns" ADD CONSTRAINT "ActiveSpawns_pkey" PRIMARY KEY ("MessageId");