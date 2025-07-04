-- Migration: Fix Individual Party Array Issues
-- This migration handles specific party array data that wasn't properly converted

DO $$
BEGIN
    -- Fix party arrays that don't have exactly 6 elements
    UPDATE users 
    SET party = CASE
        WHEN array_length(party, 1) < 6 THEN 
            party || (SELECT ARRAY(SELECT 0::numeric(20,0) FROM generate_series(1, 6-array_length(party, 1))))
        WHEN array_length(party, 1) > 6 THEN 
            party[1:6]
        ELSE 
            party
    END
    WHERE party IS NOT NULL 
    AND (array_length(party, 1) != 6 OR array_length(party, 1) IS NULL);

    -- Fix type_tokens arrays that don't have exactly 18 elements  
    UPDATE users 
    SET type_tokens = CASE
        WHEN array_length(type_tokens, 1) < 18 THEN 
            type_tokens || (SELECT ARRAY(SELECT 0::numeric(20,0) FROM generate_series(1, 18-array_length(type_tokens, 1))))
        WHEN array_length(type_tokens, 1) > 18 THEN 
            type_tokens[1:18]
        ELSE 
            type_tokens
    END
    WHERE type_tokens IS NOT NULL 
    AND (array_length(type_tokens, 1) != 18 OR array_length(type_tokens, 1) IS NULL);

    -- Ensure party arrays are not NULL and have default values
    UPDATE users 
    SET party = ARRAY[0,0,0,0,0,0]::numeric(20,0)[]
    WHERE party IS NULL;

    -- Ensure type_tokens arrays are not NULL and have default values
    UPDATE users 
    SET type_tokens = ARRAY[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]::numeric(20,0)[]
    WHERE type_tokens IS NULL;

    -- Fix any remaining array data type issues for specific problematic users
    -- Handle cases where the array might still be integer[] instead of numeric(20,0)[]
    
    -- Check if there are any remaining integer[] columns and convert them
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'users' 
        AND column_name = 'party' 
        AND data_type = 'ARRAY' 
        AND udt_name = '_int4'
    ) THEN
        -- Convert remaining integer[] to numeric(20,0)[]
        ALTER TABLE users ALTER COLUMN party TYPE numeric(20,0)[] 
        USING (
            SELECT ARRAY(
                SELECT CAST(unnest(party) AS numeric(20,0))
            )
        );
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'users' 
        AND column_name = 'type_tokens' 
        AND data_type = 'ARRAY' 
        AND udt_name = '_int4'  
    ) THEN
        -- Convert remaining integer[] to numeric(20,0)[]
        ALTER TABLE users ALTER COLUMN type_tokens TYPE numeric(20,0)[] 
        USING (
            SELECT ARRAY(
                SELECT CAST(unnest(type_tokens) AS numeric(20,0))
            )
        );
    END IF;

    -- Log the fix
    RAISE NOTICE 'Fixed party and type_tokens arrays for users with incorrect array lengths or types';

END $$;

-- Add constraints to prevent future issues
ALTER TABLE users ADD CONSTRAINT party_length_check 
CHECK (array_length(party, 1) = 6);

ALTER TABLE users ADD CONSTRAINT type_tokens_length_check 
CHECK (array_length(type_tokens, 1) = 18);

COMMENT ON CONSTRAINT party_length_check ON users IS 'Ensures party array always has exactly 6 elements';
COMMENT ON CONSTRAINT type_tokens_length_check ON users IS 'Ensures type_tokens array always has exactly 18 elements';