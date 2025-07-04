-- Migration: Add Active Spawn Tracking
-- Description: Creates table for tracking active Pokemon spawns across bot restarts
-- Author: Claude
-- Date: 2025-01-01

-- Create ActiveSpawns table
CREATE TABLE IF NOT EXISTS "ActiveSpawns" (
    "Id" BIGSERIAL PRIMARY KEY,
    "MessageId" BIGINT NOT NULL UNIQUE,
    "ChannelId" BIGINT NOT NULL,
    "GuildId" BIGINT NOT NULL,
    "PokemonName" VARCHAR(255) NOT NULL,
    "IsShiny" BOOLEAN NOT NULL DEFAULT FALSE,
    "LegendaryChance" INTEGER NOT NULL DEFAULT 0,
    "UltraBeastChance" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsCaught" BOOLEAN NOT NULL DEFAULT FALSE,
    "CaughtByUserId" BIGINT NULL,
    "CaughtAt" TIMESTAMP WITH TIME ZONE NULL
);

-- Create indexes for efficient lookups
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ActiveSpawns_MessageId" ON "ActiveSpawns" ("MessageId");
CREATE INDEX IF NOT EXISTS "IX_ActiveSpawns_ChannelId_IsCaught" ON "ActiveSpawns" ("ChannelId", "IsCaught");
CREATE INDEX IF NOT EXISTS "IX_ActiveSpawns_CreatedAt" ON "ActiveSpawns" ("CreatedAt");

-- Add comments for documentation
COMMENT ON TABLE "ActiveSpawns" IS 'Tracks active Pokemon spawns for race condition prevention and persistence across restarts';
COMMENT ON COLUMN "ActiveSpawns"."MessageId" IS 'Discord message ID of the spawn message';
COMMENT ON COLUMN "ActiveSpawns"."ChannelId" IS 'Discord channel ID where spawn occurred';
COMMENT ON COLUMN "ActiveSpawns"."GuildId" IS 'Discord guild ID where spawn occurred';
COMMENT ON COLUMN "ActiveSpawns"."PokemonName" IS 'Name of the spawned Pokemon';
COMMENT ON COLUMN "ActiveSpawns"."IsShiny" IS 'Whether the spawned Pokemon is shiny';
COMMENT ON COLUMN "ActiveSpawns"."LegendaryChance" IS 'Legendary spawn chance value used';
COMMENT ON COLUMN "ActiveSpawns"."UltraBeastChance" IS 'Ultra Beast spawn chance value used';
COMMENT ON COLUMN "ActiveSpawns"."IsCaught" IS 'Whether this spawn has been caught';
COMMENT ON COLUMN "ActiveSpawns"."CaughtByUserId" IS 'Discord user ID who caught this Pokemon';