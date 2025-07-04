-- Migration to use TEXT columns for array storage to avoid type conversion issues
-- Since PostgreSQL doesn't have unsigned integers, we'll store arrays as JSON text
-- This allows proper handling of ulong[] values without precision loss

-- Convert users table array columns to text with JSON format
DO $$
BEGIN
    -- Check if party column is not already text
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'party' AND data_type != 'text') THEN
        ALTER TABLE public.users 
        ALTER COLUMN party TYPE text 
        USING CASE 
            WHEN party IS NULL THEN '[0,0,0,0,0,0]'
            ELSE '[' || array_to_string(party, ',') || ']'
        END;
    END IF;
    
    -- Check if type_tokens column is not already text
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'type_tokens' AND data_type != 'text') THEN
        ALTER TABLE public.users 
        ALTER COLUMN type_tokens TYPE text 
        USING CASE 
            WHEN type_tokens IS NULL THEN '[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]'
            ELSE '[' || array_to_string(type_tokens, ',') || ']'
        END;
    END IF;
END $$;

-- Handle females column (int[] array)
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'females' AND data_type != 'text') THEN
        ALTER TABLE public.users 
        ALTER COLUMN females TYPE text 
        USING CASE 
            WHEN females IS NULL THEN '[]'
            ELSE '[' || array_to_string(females, ',') || ']'
        END;
    END IF;
END $$;

-- Handle titles column (string[] array) - needs proper JSON escaping
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'titles' AND data_type != 'text') THEN
        ALTER TABLE public.users 
        ALTER COLUMN titles TYPE text 
        USING CASE 
            WHEN titles IS NULL THEN '["Newcomer"]'
            WHEN array_length(titles, 1) IS NULL THEN '["Newcomer"]'
            ELSE '["' || array_to_string(titles, '","') || '"]'
        END;
    END IF;
END $$;

-- Handle alt column if it exists
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'alt' AND data_type != 'text') THEN
        ALTER TABLE public.users 
        ALTER COLUMN alt TYPE text 
        USING CASE 
            WHEN alt IS NULL THEN '[]'
            ELSE '[' || array_to_string(alt, ',') || ']'
        END;
    END IF;
END $$;

-- Convert servers table array columns to text
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'servers' AND column_name = 'redirects' AND data_type != 'text') THEN
        ALTER TABLE public.servers 
        ALTER COLUMN redirects TYPE text 
        USING CASE 
            WHEN redirects IS NULL THEN '[]'
            ELSE '[' || array_to_string(redirects, ',') || ']'
        END;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'servers' AND column_name = 'disabled_channels' AND data_type != 'text') THEN
        ALTER TABLE public.servers 
        ALTER COLUMN disabled_channels TYPE text 
        USING CASE 
            WHEN disabled_channels IS NULL THEN '[]'
            ELSE '[' || array_to_string(disabled_channels, ',') || ']'
        END;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'servers' AND column_name = 'spawns_disabled' AND data_type != 'text') THEN
        ALTER TABLE public.servers 
        ALTER COLUMN spawns_disabled TYPE text 
        USING CASE 
            WHEN spawns_disabled IS NULL THEN '[]'
            ELSE '[' || array_to_string(spawns_disabled, ',') || ']'
        END;
    END IF;
END $$;

-- Convert trade_logs table if it has ulong arrays
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'trade_logs' AND column_name = 'sender_pokemon_ids') THEN
        ALTER TABLE public.trade_logs 
        ALTER COLUMN sender_pokemon_ids TYPE numeric(20,0)[] 
        USING sender_pokemon_ids::numeric(20,0)[];
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'trade_logs' AND column_name = 'receiver_pokemon_ids') THEN
        ALTER TABLE public.trade_logs 
        ALTER COLUMN receiver_pokemon_ids TYPE numeric(20,0)[] 
        USING receiver_pokemon_ids::numeric(20,0)[];
    END IF;
END $$;

-- Convert egg_hatchery table if it has ulong arrays
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'egg_hatchery' AND column_name = 'pokemon_ids') THEN
        ALTER TABLE public.egg_hatchery 
        ALTER COLUMN pokemon_ids TYPE numeric(20,0)[] 
        USING pokemon_ids::numeric(20,0)[];
    END IF;
END $$;

-- Convert gifts table if it has ulong arrays
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'gifts' AND column_name = 'pokemon_ids') THEN
        ALTER TABLE public.gifts 
        ALTER COLUMN pokemon_ids TYPE numeric(20,0)[] 
        USING pokemon_ids::numeric(20,0)[];
    END IF;
END $$;

-- Convert active_users table if it has ulong arrays
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'active_users' AND column_name = 'user_ids') THEN
        ALTER TABLE public.active_users 
        ALTER COLUMN user_ids TYPE numeric(20,0)[] 
        USING user_ids::numeric(20,0)[];
    END IF;
END $$;

-- Convert inactive_users table if it has ulong arrays
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'inactive_users' AND column_name = 'user_ids') THEN
        ALTER TABLE public.inactive_users 
        ALTER COLUMN user_ids TYPE numeric(20,0)[] 
        USING user_ids::numeric(20,0)[];
    END IF;
END $$;

-- Convert community table if it has ulong arrays
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'community' AND column_name = 'member_ids') THEN
        ALTER TABLE public.community 
        ALTER COLUMN member_ids TYPE numeric(20,0)[] 
        USING member_ids::numeric(20,0)[];
    END IF;
END $$;

-- Convert bot_bans table if it has ulong arrays
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'bot_bans' AND column_name = 'banned_user_ids') THEN
        ALTER TABLE public.bot_bans 
        ALTER COLUMN banned_user_ids TYPE numeric(20,0)[] 
        USING banned_user_ids::numeric(20,0)[];
    END IF;
END $$;