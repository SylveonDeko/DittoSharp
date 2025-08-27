-- Migration: 018_AddBreedingFemaleToOwnership
-- Description: Add is_breeding_female flag to user_pokemon_ownership table
-- This replaces the problematic females array in users table with a proper relational approach

DO $$
BEGIN
    -- Add the is_breeding_female column to user_pokemon_ownership table
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                  WHERE table_name = 'user_pokemon_ownership' AND column_name = 'is_breeding_female') THEN
        
        RAISE NOTICE 'Adding is_breeding_female column to user_pokemon_ownership table';
        
        ALTER TABLE user_pokemon_ownership 
        ADD COLUMN is_breeding_female boolean NOT NULL DEFAULT false;
        
        -- Create index for efficient breeding female queries
        CREATE INDEX IF NOT EXISTS idx_user_pokemon_ownership_breeding_female 
        ON user_pokemon_ownership (user_id, is_breeding_female) 
        WHERE is_breeding_female = true;
        
        RAISE NOTICE 'Added is_breeding_female column and index';
    END IF;
    
    -- Migrate existing females array data to the new column if it exists
    IF EXISTS (SELECT 1 FROM information_schema.columns 
              WHERE table_name = 'users' AND column_name = 'females') THEN
        
        RAISE NOTICE 'Migrating existing females array data to is_breeding_female flags';
        
        -- Update ownership records for users who have females set
        UPDATE user_pokemon_ownership 
        SET is_breeding_female = true
        WHERE (user_id, position) IN (
            SELECT u.u_id, unnest(u.females)::bigint
            FROM users u 
            WHERE u.females IS NOT NULL 
            AND array_length(u.females, 1) > 0
        );
        
        -- Get migration stats
        DECLARE
            migrated_count INTEGER;
        BEGIN
            SELECT COUNT(*) INTO migrated_count 
            FROM user_pokemon_ownership 
            WHERE is_breeding_female = true;
            
            RAISE NOTICE 'Migration completed - % Pokemon marked as breeding females', migrated_count;
        END;
        
        -- Drop the old females column since we no longer need it
        RAISE NOTICE 'Dropping old females column from users table';
        ALTER TABLE users DROP COLUMN females;
        
    ELSE
        RAISE NOTICE 'No females column found - migration not needed or already completed';
    END IF;
    
END $$;

-- Add helpful comments
COMMENT ON COLUMN user_pokemon_ownership.is_breeding_female IS 'Flag indicating if this Pokemon is in the users breeding female list';
COMMENT ON INDEX idx_user_pokemon_ownership_breeding_female IS 'Index for efficient breeding female queries';