-- Migration: Convert JSON array columns to PostgreSQL native arrays
-- This migration converts existing JSON string arrays to PostgreSQL native arrays

-- Step 1: Add temporary columns with proper array types
ALTER TABLE users ADD COLUMN party_new numeric(20,0)[];
ALTER TABLE users ADD COLUMN type_tokens_new numeric(20,0)[];
ALTER TABLE users ADD COLUMN females_new integer[];
ALTER TABLE users ADD COLUMN titles_new text[];
ALTER TABLE users ADD COLUMN alt_new numeric(20,0)[];

-- Step 2: Populate new columns with converted data
-- Convert party 
UPDATE users SET party_new = ARRAY[0,0,0,0,0,0]::numeric(20,0)[] 
WHERE party IS NULL OR party = '' OR party = '[]';

UPDATE users SET party_new = (
    SELECT ARRAY(
        SELECT CAST(value AS numeric(20,0))
        FROM json_array_elements_text(party::json) AS value
    )
) WHERE party IS NOT NULL AND party != '' AND party != '[]' AND party ~ '^\[.*\]$';

-- Convert type_tokens
UPDATE users SET type_tokens_new = ARRAY[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]::numeric(20,0)[] 
WHERE type_tokens IS NULL OR type_tokens = '' OR type_tokens = '[]';

UPDATE users SET type_tokens_new = (
    SELECT ARRAY(
        SELECT CAST(value AS numeric(20,0))
        FROM json_array_elements_text(type_tokens::json) AS value
    )
) WHERE type_tokens IS NOT NULL AND type_tokens != '' AND type_tokens != '[]' AND type_tokens ~ '^\[.*\]$';

-- Convert females (can be NULL)
UPDATE users SET females_new = NULL 
WHERE females IS NULL OR females = '' OR females = '[]';

UPDATE users SET females_new = (
    SELECT ARRAY(
        SELECT CASE 
            WHEN value = 'null' THEN NULL 
            ELSE CAST(value AS integer) 
        END
        FROM json_array_elements_text(females::json) AS value
    )
) WHERE females IS NOT NULL AND females != '' AND females != '[]' AND females ~ '^\[.*\]$';

-- Convert titles
UPDATE users SET titles_new = ARRAY['Newcomer']::text[] 
WHERE titles IS NULL OR titles = '' OR titles = '[]';

UPDATE users SET titles_new = (
    SELECT ARRAY(
        SELECT trim(value, '"')
        FROM json_array_elements_text(titles::json) AS value
    )
) WHERE titles IS NOT NULL AND titles != '' AND titles != '[]' AND titles ~ '^\[.*\]$';

-- Convert alt (can be NULL)
UPDATE users SET alt_new = NULL 
WHERE alt IS NULL OR alt = '' OR alt = '[]';

UPDATE users SET alt_new = (
    SELECT ARRAY(
        SELECT CAST(value AS numeric(20,0))
        FROM json_array_elements_text(alt::json) AS value
    )
) WHERE alt IS NOT NULL AND alt != '' AND alt != '[]' AND alt ~ '^\[.*\]$';

-- Step 3: Drop old columns and rename new ones
ALTER TABLE users DROP COLUMN party;
ALTER TABLE users RENAME COLUMN party_new TO party;

ALTER TABLE users DROP COLUMN type_tokens;
ALTER TABLE users RENAME COLUMN type_tokens_new TO type_tokens;

ALTER TABLE users DROP COLUMN females;
ALTER TABLE users RENAME COLUMN females_new TO females;

ALTER TABLE users DROP COLUMN titles;
ALTER TABLE users RENAME COLUMN titles_new TO titles;

ALTER TABLE users DROP COLUMN alt;
ALTER TABLE users RENAME COLUMN alt_new TO alt;