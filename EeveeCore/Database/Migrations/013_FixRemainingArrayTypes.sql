-- Migration: Fix Remaining Array Type Mismatches
-- This migration ensures all array columns have the correct PostgreSQL data types
-- to match LinqToDB expectations and C# model definitions

DO $$
BEGIN
    -- Check and fix users table array columns
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'users') THEN
        
        -- Fix party column - ensure it's numeric(20,0)[] not integer[]
        IF EXISTS (SELECT 1 FROM information_schema.columns 
                  WHERE table_name = 'users' AND column_name = 'party' 
                  AND data_type = 'ARRAY' AND udt_name != '_numeric') THEN
            
            RAISE NOTICE 'Converting party column from integer[] to numeric(20,0)[]';
            
            -- Create temporary column
            ALTER TABLE users ADD COLUMN party_temp numeric(20,0)[];
            
            -- Convert data
            UPDATE users SET party_temp = CASE 
                WHEN party IS NULL THEN ARRAY[0,0,0,0,0,0]::numeric(20,0)[]
                ELSE (SELECT ARRAY(SELECT CAST(unnest(party) AS numeric(20,0))))
            END;
            
            -- Replace column
            ALTER TABLE users DROP COLUMN party;
            ALTER TABLE users RENAME COLUMN party_temp TO party;
            ALTER TABLE users ALTER COLUMN party SET DEFAULT ARRAY[0,0,0,0,0,0]::numeric(20,0)[];
        END IF;
        
        -- Fix type_tokens column - ensure it's numeric(20,0)[] not integer[]
        IF EXISTS (SELECT 1 FROM information_schema.columns 
                  WHERE table_name = 'users' AND column_name = 'type_tokens' 
                  AND data_type = 'ARRAY' AND udt_name != '_numeric') THEN
            
            RAISE NOTICE 'Converting type_tokens column from integer[] to numeric(20,0)[]';
            
            -- Create temporary column
            ALTER TABLE users ADD COLUMN type_tokens_temp numeric(20,0)[];
            
            -- Convert data
            UPDATE users SET type_tokens_temp = CASE 
                WHEN type_tokens IS NULL THEN ARRAY[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]::numeric(20,0)[]
                ELSE (SELECT ARRAY(SELECT CAST(unnest(type_tokens) AS numeric(20,0))))
            END;
            
            -- Replace column
            ALTER TABLE users DROP COLUMN type_tokens;
            ALTER TABLE users RENAME COLUMN type_tokens_temp TO type_tokens;
            ALTER TABLE users ALTER COLUMN type_tokens SET DEFAULT ARRAY[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]::numeric(20,0)[];
        END IF;
        
        -- Fix alt column - ensure it's numeric(20,0)[] not integer[]
        IF EXISTS (SELECT 1 FROM information_schema.columns 
                  WHERE table_name = 'users' AND column_name = 'alt' 
                  AND data_type = 'ARRAY' AND udt_name != '_numeric') THEN
            
            RAISE NOTICE 'Converting alt column from integer[] to numeric(20,0)[]';
            
            -- Create temporary column
            ALTER TABLE users ADD COLUMN alt_temp numeric(20,0)[];
            
            -- Convert data
            UPDATE users SET alt_temp = CASE 
                WHEN alt IS NULL THEN NULL
                ELSE (SELECT ARRAY(SELECT CAST(unnest(alt) AS numeric(20,0))))
            END;
            
            -- Replace column
            ALTER TABLE users DROP COLUMN alt;
            ALTER TABLE users RENAME COLUMN alt_temp TO alt;
        END IF;
        
        -- Fix females column - ensure proper nullable handling
        IF EXISTS (SELECT 1 FROM information_schema.columns 
                  WHERE table_name = 'users' AND column_name = 'females') THEN
            
            RAISE NOTICE 'Ensuring females column supports nullable elements';
            
            -- Check if there are any non-integer values that might cause issues
            UPDATE users SET females = NULL 
            WHERE females IS NOT NULL 
            AND EXISTS (
                SELECT 1 FROM unnest(females) AS elem 
                WHERE elem::text !~ '^-?[0-9]+$'
            );
            
            -- Ensure proper data type
            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                          WHERE table_name = 'users' AND column_name = 'females' 
                          AND data_type = 'ARRAY' AND udt_name = '_int4') THEN
                
                ALTER TABLE users ALTER COLUMN females TYPE integer[] 
                USING CASE 
                    WHEN females IS NULL THEN NULL
                    ELSE females::integer[]
                END;
            END IF;
        END IF;
        
        -- Fix titles column - ensure it's text[] not varchar[]
        IF EXISTS (SELECT 1 FROM information_schema.columns 
                  WHERE table_name = 'users' AND column_name = 'titles' 
                  AND data_type = 'ARRAY' AND udt_name != '_text') THEN
            
            RAISE NOTICE 'Converting titles column to text[]';
            
            ALTER TABLE users ALTER COLUMN titles TYPE text[] 
            USING CASE 
                WHEN titles IS NULL THEN ARRAY['Newcomer']::text[]
                ELSE titles::text[]
            END;
            
            ALTER TABLE users ALTER COLUMN titles SET DEFAULT ARRAY['Newcomer']::text[];
        END IF;
        
    END IF;
    
    -- Check and fix servers table array columns
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'servers') THEN
        
        -- Fix redirects column
        IF EXISTS (SELECT 1 FROM information_schema.columns 
                  WHERE table_name = 'servers' AND column_name = 'redirects' 
                  AND data_type = 'ARRAY' AND udt_name != '_numeric') THEN
            
            RAISE NOTICE 'Converting servers.redirects column to numeric(20,0)[]';
            
            ALTER TABLE servers ALTER COLUMN redirects TYPE numeric(20,0)[] 
            USING CASE 
                WHEN redirects IS NULL THEN NULL
                ELSE (SELECT ARRAY(SELECT CAST(unnest(redirects) AS numeric(20,0))))
            END;
        END IF;
        
        -- Fix disabled_channels column
        IF EXISTS (SELECT 1 FROM information_schema.columns 
                  WHERE table_name = 'servers' AND column_name = 'disabled_channels' 
                  AND data_type = 'ARRAY' AND udt_name != '_numeric') THEN
            
            RAISE NOTICE 'Converting servers.disabled_channels column to numeric(20,0)[]';
            
            ALTER TABLE servers ALTER COLUMN disabled_channels TYPE numeric(20,0)[] 
            USING CASE 
                WHEN disabled_channels IS NULL THEN NULL
                ELSE (SELECT ARRAY(SELECT CAST(unnest(disabled_channels) AS numeric(20,0))))
            END;
        END IF;
        
        -- Fix spawns_disabled column
        IF EXISTS (SELECT 1 FROM information_schema.columns 
                  WHERE table_name = 'servers' AND column_name = 'spawns_disabled' 
                  AND data_type = 'ARRAY' AND udt_name != '_numeric') THEN
            
            RAISE NOTICE 'Converting servers.spawns_disabled column to numeric(20,0)[]';
            
            ALTER TABLE servers ALTER COLUMN spawns_disabled TYPE numeric(20,0)[] 
            USING CASE 
                WHEN spawns_disabled IS NULL THEN NULL
                ELSE (SELECT ARRAY(SELECT CAST(unnest(spawns_disabled) AS numeric(20,0))))
            END;
        END IF;
        
    END IF;
    
    -- Clean up any remaining array data inconsistencies
    -- Remove any NULL elements from arrays that shouldn't have them
    UPDATE users SET party = ARRAY[0,0,0,0,0,0]::numeric(20,0)[] WHERE party IS NULL;
    UPDATE users SET type_tokens = ARRAY[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]::numeric(20,0)[] WHERE type_tokens IS NULL;
    UPDATE users SET titles = ARRAY['Newcomer']::text[] WHERE titles IS NULL;
    
    -- Ensure array lengths are correct
    UPDATE users SET party = party || ARRAY[0,0,0,0,0,0]::numeric(20,0)[] WHERE array_length(party, 1) < 6;
    UPDATE users SET party = party[1:6] WHERE array_length(party, 1) > 6;
    
    UPDATE users SET type_tokens = type_tokens || ARRAY[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]::numeric(20,0)[] WHERE array_length(type_tokens, 1) < 18;
    UPDATE users SET type_tokens = type_tokens[1:18] WHERE array_length(type_tokens, 1) > 18;
    
END $$;

-- Add helpful comments
COMMENT ON COLUMN users.party IS 'User party Pokemon IDs as PostgreSQL numeric(20,0) array - maps to C# long[]';
COMMENT ON COLUMN users.type_tokens IS 'User type effectiveness tokens as PostgreSQL numeric(20,0) array - maps to C# long[]';
COMMENT ON COLUMN users.females IS 'User female Pokemon statistics as PostgreSQL integer array - maps to C# int?[]';
COMMENT ON COLUMN users.titles IS 'User earned titles as PostgreSQL text array - maps to C# string[]';
COMMENT ON COLUMN users.alt IS 'User alternative Pokemon IDs as PostgreSQL numeric(20,0) array - maps to C# long[]';