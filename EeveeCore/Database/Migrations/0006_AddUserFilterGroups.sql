-- Migration: Add User Filter Groups
-- Description: Creates tables for user-defined custom Pokemon filter groups and criteria
-- Author: Claude
-- Date: 2025-01-02

-- Create user_filter_groups table
CREATE TABLE IF NOT EXISTS "user_filter_groups" (
    "id" SERIAL PRIMARY KEY,
    "user_id" BIGINT NOT NULL,
    "name" VARCHAR(100) NOT NULL,
    "description" VARCHAR(500) NULL,
    "color" VARCHAR(7) NOT NULL DEFAULT '#3B82F6',
    "icon" VARCHAR(50) NOT NULL DEFAULT '📁',
    "is_favorite" BOOLEAN NOT NULL DEFAULT FALSE,
    "sort_order" INTEGER NOT NULL DEFAULT 0,
    "is_active" BOOLEAN NOT NULL DEFAULT TRUE,
    "created_at" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "updated_at" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create user_filter_criteria table
CREATE TABLE IF NOT EXISTS "user_filter_criteria" (
    "id" SERIAL PRIMARY KEY,
    "filter_group_id" INTEGER NOT NULL,
    "field_name" VARCHAR(50) NOT NULL,
    "operator" VARCHAR(20) NOT NULL,
    "value_text" VARCHAR(255) NULL,
    "value_numeric" INTEGER NULL,
    "value_numeric_max" INTEGER NULL,
    "value_boolean" BOOLEAN NULL,
    "logical_connector" VARCHAR(5) NULL,
    "criterion_order" INTEGER NOT NULL DEFAULT 0
);

-- Create indexes for efficient lookups
CREATE INDEX IF NOT EXISTS "IX_user_filter_groups_user_id_is_active" ON "user_filter_groups" ("user_id", "is_active");
CREATE INDEX IF NOT EXISTS "IX_user_filter_groups_user_id_name" ON "user_filter_groups" ("user_id", "name");
CREATE INDEX IF NOT EXISTS "IX_user_filter_groups_sort_order" ON "user_filter_groups" ("sort_order");
CREATE INDEX IF NOT EXISTS "IX_user_filter_criteria_filter_group_id" ON "user_filter_criteria" ("filter_group_id");
CREATE INDEX IF NOT EXISTS "IX_user_filter_criteria_criterion_order" ON "user_filter_criteria" ("criterion_order");

-- Add foreign key constraints
ALTER TABLE "user_filter_criteria" 
ADD CONSTRAINT "FK_user_filter_criteria_user_filter_groups_filter_group_id" 
FOREIGN KEY ("filter_group_id") REFERENCES "user_filter_groups" ("id") ON DELETE CASCADE;

-- Add unique constraint for user filter group names
CREATE UNIQUE INDEX IF NOT EXISTS "IX_user_filter_groups_user_id_name_unique" 
ON "user_filter_groups" ("user_id", "name") WHERE "is_active" = TRUE;

-- Add comments for documentation
COMMENT ON TABLE "user_filter_groups" IS 'User-defined custom filter groups for Pokemon collections';
COMMENT ON COLUMN "user_filter_groups"."user_id" IS 'Discord user ID who owns this filter group';
COMMENT ON COLUMN "user_filter_groups"."name" IS 'User-friendly name for the filter group';
COMMENT ON COLUMN "user_filter_groups"."description" IS 'Optional description of what this filter group contains';
COMMENT ON COLUMN "user_filter_groups"."color" IS 'Hex color code for UI display';
COMMENT ON COLUMN "user_filter_groups"."icon" IS 'Emoji or icon for UI display';
COMMENT ON COLUMN "user_filter_groups"."is_favorite" IS 'Whether this filter group is marked as favorite';
COMMENT ON COLUMN "user_filter_groups"."sort_order" IS 'Order for displaying filter groups';
COMMENT ON COLUMN "user_filter_groups"."is_active" IS 'Soft delete flag - false for deleted groups';

COMMENT ON TABLE "user_filter_criteria" IS 'Individual filter criteria within a filter group';
COMMENT ON COLUMN "user_filter_criteria"."filter_group_id" IS 'Foreign key to the parent filter group';
COMMENT ON COLUMN "user_filter_criteria"."field_name" IS 'Pokemon field to filter on (e.g., level, shiny, nature)';
COMMENT ON COLUMN "user_filter_criteria"."operator" IS 'Comparison operator (equals, greater_than, contains, etc.)';
COMMENT ON COLUMN "user_filter_criteria"."value_text" IS 'Text value for string comparisons';
COMMENT ON COLUMN "user_filter_criteria"."value_numeric" IS 'Numeric value for number comparisons';
COMMENT ON COLUMN "user_filter_criteria"."value_numeric_max" IS 'Upper bound for range comparisons';
COMMENT ON COLUMN "user_filter_criteria"."value_boolean" IS 'Boolean value for true/false comparisons';
COMMENT ON COLUMN "user_filter_criteria"."logical_connector" IS 'How to combine with next criterion (AND/OR)';
COMMENT ON COLUMN "user_filter_criteria"."criterion_order" IS 'Order for processing criteria within a group';