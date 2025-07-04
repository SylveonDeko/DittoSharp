-- Migration: Fix Array Conversion Conflict

-- First, ensure we have a clean slate by dropping and recreating problematic columns
DO $$
BEGIN
    -- Check if users table exists
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'users') THEN
        -- Drop temporary columns if they exist (from failed migration 008)
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'party_new') THEN
            ALTER TABLE users DROP COLUMN party_new;
        END IF;
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'type_tokens_new') THEN
            ALTER TABLE users DROP COLUMN type_tokens_new;
        END IF;
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'females_new') THEN
            ALTER TABLE users DROP COLUMN females_new;
        END IF;
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'titles_new') THEN
            ALTER TABLE users DROP COLUMN titles_new;
        END IF;
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'alt_new') THEN
            ALTER TABLE users DROP COLUMN alt_new;
        END IF;
        
        -- Convert party column properly
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'party') THEN
            -- First, ensure it's text type
            IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'party' AND data_type != 'text') THEN
                ALTER TABLE users ALTER COLUMN party TYPE text USING party::text;
            END IF;
            
            -- Add new column with proper type
            ALTER TABLE users ADD COLUMN party_fixed numeric(20,0)[];
            
            -- Convert data safely
            UPDATE users SET party_fixed = CASE 
                WHEN party IS NULL OR party = '' OR party = '[]' THEN ARRAY[0,0,0,0,0,0]::numeric(20,0)[]
                WHEN party ~ '^\[[\d,\s]*\]$' THEN (
                    SELECT ARRAY(
                        SELECT CAST(trim(value) AS numeric(20,0))
                        FROM unnest(string_to_array(trim(party, '[]'), ',')) AS value
                        WHERE trim(value) != ''
                    )
                )
                ELSE ARRAY[0,0,0,0,0,0]::numeric(20,0)[]
            END;
            
            -- Drop old column and rename new one
            ALTER TABLE users DROP COLUMN party;
            ALTER TABLE users RENAME COLUMN party_fixed TO party;
        END IF;
        
        -- Convert type_tokens column properly
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'type_tokens') THEN
            -- First, ensure it's text type
            IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'type_tokens' AND data_type != 'text') THEN
                ALTER TABLE users ALTER COLUMN type_tokens TYPE text USING type_tokens::text;
            END IF;
            
            -- Add new column with proper type
            ALTER TABLE users ADD COLUMN type_tokens_fixed numeric(20,0)[];
            
            -- Convert data safely
            UPDATE users SET type_tokens_fixed = CASE 
                WHEN type_tokens IS NULL OR type_tokens = '' OR type_tokens = '[]' THEN ARRAY[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]::numeric(20,0)[]
                WHEN type_tokens ~ '^\[[\d,\s]*\]$' THEN (
                    SELECT ARRAY(
                        SELECT CAST(trim(value) AS numeric(20,0))
                        FROM unnest(string_to_array(trim(type_tokens, '[]'), ',')) AS value
                        WHERE trim(value) != ''
                    )
                )
                ELSE ARRAY[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]::numeric(20,0)[]
            END;
            
            -- Drop old column and rename new one
            ALTER TABLE users DROP COLUMN type_tokens;
            ALTER TABLE users RENAME COLUMN type_tokens_fixed TO type_tokens;
        END IF;
        
        -- Convert females column properly
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'females') THEN
            -- First, ensure it's text type
            IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'females' AND data_type != 'text') THEN
                ALTER TABLE users ALTER COLUMN females TYPE text USING females::text;
            END IF;
            
            -- Add new column with proper type
            ALTER TABLE users ADD COLUMN females_fixed integer[];
            
            -- Convert data safely
            UPDATE users SET females_fixed = CASE 
                WHEN females IS NULL OR females = '' OR females = '[]' THEN NULL
                WHEN females ~ '^\[[\d,\s]*\]$' THEN (
                    SELECT ARRAY(
                        SELECT CAST(trim(value) AS integer)
                        FROM unnest(string_to_array(trim(females, '[]'), ',')) AS value
                        WHERE trim(value) != '' AND trim(value) != 'null'
                    )
                )
                ELSE NULL
            END;
            
            -- Drop old column and rename new one
            ALTER TABLE users DROP COLUMN females;
            ALTER TABLE users RENAME COLUMN females_fixed TO females;
        END IF;
        
        -- Convert titles column properly
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'titles') THEN
            -- First, ensure it's text type
            IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'titles' AND data_type != 'text') THEN
                ALTER TABLE users ALTER COLUMN titles TYPE text USING titles::text;
            END IF;
            
            -- Add new column with proper type
            ALTER TABLE users ADD COLUMN titles_fixed text[];
            
            -- Convert data safely
            UPDATE users SET titles_fixed = CASE 
                WHEN titles IS NULL OR titles = '' OR titles = '[]' THEN ARRAY['Newcomer']::text[]
                WHEN titles ~ '^\[.*\]$' THEN (
                    SELECT ARRAY(
                        SELECT trim(trim(value, '"'), '''')
                        FROM unnest(string_to_array(trim(titles, '[]'), ',')) AS value
                        WHERE trim(value) != ''
                    )
                )
                ELSE ARRAY['Newcomer']::text[]
            END;
            
            -- Drop old column and rename new one
            ALTER TABLE users DROP COLUMN titles;
            ALTER TABLE users RENAME COLUMN titles_fixed TO titles;
        END IF;
        
        -- Convert alt column properly
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'alt') THEN
            -- First, ensure it's text type
            IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'alt' AND data_type != 'text') THEN
                ALTER TABLE users ALTER COLUMN alt TYPE text USING alt::text;
            END IF;
            
            -- Add new column with proper type
            ALTER TABLE users ADD COLUMN alt_fixed numeric(20,0)[];
            
            -- Convert data safely
            UPDATE users SET alt_fixed = CASE 
                WHEN alt IS NULL OR alt = '' OR alt = '[]' THEN NULL
                WHEN alt ~ '^\[[\d,\s]*\]$' THEN (
                    SELECT ARRAY(
                        SELECT CAST(trim(value) AS numeric(20,0))
                        FROM unnest(string_to_array(trim(alt, '[]'), ',')) AS value
                        WHERE trim(value) != '' AND trim(value) != 'null'
                    )
                )
                ELSE NULL
            END;
            
            -- Drop old column and rename new one
            ALTER TABLE users DROP COLUMN alt;
            ALTER TABLE users RENAME COLUMN alt_fixed TO alt;
        END IF;
    END IF;
END $$;

-- Handle servers table array columns
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'servers') THEN
        -- Convert redirects column
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'servers' AND column_name = 'redirects') THEN
            IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'servers' AND column_name = 'redirects' AND data_type != 'text') THEN
                ALTER TABLE servers ALTER COLUMN redirects TYPE text USING redirects::text;
            END IF;
            
            ALTER TABLE servers ADD COLUMN redirects_fixed numeric(20,0)[];
            
            UPDATE servers SET redirects_fixed = CASE 
                WHEN redirects IS NULL OR redirects = '' OR redirects = '[]' THEN NULL
                WHEN redirects ~ '^\[[\d,\s]*\]$' THEN (
                    SELECT ARRAY(
                        SELECT CAST(trim(value) AS numeric(20,0))
                        FROM unnest(string_to_array(trim(redirects, '[]'), ',')) AS value
                        WHERE trim(value) != ''
                    )
                )
                ELSE NULL
            END;
            
            ALTER TABLE servers DROP COLUMN redirects;
            ALTER TABLE servers RENAME COLUMN redirects_fixed TO redirects;
        END IF;
        
        -- Convert disabled_channels column
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'servers' AND column_name = 'disabled_channels') THEN
            IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'servers' AND column_name = 'disabled_channels' AND data_type != 'text') THEN
                ALTER TABLE servers ALTER COLUMN disabled_channels TYPE text USING disabled_channels::text;
            END IF;
            
            ALTER TABLE servers ADD COLUMN disabled_channels_fixed numeric(20,0)[];
            
            UPDATE servers SET disabled_channels_fixed = CASE 
                WHEN disabled_channels IS NULL OR disabled_channels = '' OR disabled_channels = '[]' THEN NULL
                WHEN disabled_channels ~ '^\[[\d,\s]*\]$' THEN (
                    SELECT ARRAY(
                        SELECT CAST(trim(value) AS numeric(20,0))
                        FROM unnest(string_to_array(trim(disabled_channels, '[]'), ',')) AS value
                        WHERE trim(value) != ''
                    )
                )
                ELSE NULL
            END;
            
            ALTER TABLE servers DROP COLUMN disabled_channels;
            ALTER TABLE servers RENAME COLUMN disabled_channels_fixed TO disabled_channels;
        END IF;
        
        -- Convert spawns_disabled column
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'servers' AND column_name = 'spawns_disabled') THEN
            IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'servers' AND column_name = 'spawns_disabled' AND data_type != 'text') THEN
                ALTER TABLE servers ALTER COLUMN spawns_disabled TYPE text USING spawns_disabled::text;
            END IF;
            
            ALTER TABLE servers ADD COLUMN spawns_disabled_fixed numeric(20,0)[];
            
            UPDATE servers SET spawns_disabled_fixed = CASE 
                WHEN spawns_disabled IS NULL OR spawns_disabled = '' OR spawns_disabled = '[]' THEN NULL
                WHEN spawns_disabled ~ '^\[[\d,\s]*\]$' THEN (
                    SELECT ARRAY(
                        SELECT CAST(trim(value) AS numeric(20,0))
                        FROM unnest(string_to_array(trim(spawns_disabled, '[]'), ',')) AS value
                        WHERE trim(value) != ''
                    )
                )
                ELSE NULL
            END;
            
            ALTER TABLE servers DROP COLUMN spawns_disabled;
            ALTER TABLE servers RENAME COLUMN spawns_disabled_fixed TO spawns_disabled;
        END IF;
    END IF;
END $$;

-- Add comments
COMMENT ON COLUMN users.party IS 'User party Pokemon IDs as PostgreSQL array';
COMMENT ON COLUMN users.type_tokens IS 'User type effectiveness tokens as PostgreSQL array';
COMMENT ON COLUMN users.females IS 'User female Pokemon statistics as PostgreSQL array';
COMMENT ON COLUMN users.titles IS 'User earned titles as PostgreSQL array';
COMMENT ON COLUMN users.alt IS 'User alternative Pokemon IDs as PostgreSQL array';