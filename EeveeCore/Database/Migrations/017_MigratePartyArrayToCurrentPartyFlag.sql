-- Migration: 017_MigratePartyArrayToCurrentPartyFlag
-- Description: Migrate users.party array data to parties table with IsCurrentParty flag
-- This migration identifies which saved party matches the user's current party array
-- and marks it as IsCurrentParty = true, or creates a new current party if no match exists

DO $$
BEGIN
    -- Check if the party column still exists in users table
    IF EXISTS (SELECT 1 FROM information_schema.columns 
              WHERE table_name = 'users' AND column_name = 'party') THEN
        
        RAISE NOTICE 'Starting migration of users.party array to parties.is_current_party flag';
        
        -- First, ensure all users have the is_current_party column in parties table
        -- (This should already exist from previous migrations, but just to be safe)
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                      WHERE table_name = 'partys' AND column_name = 'is_current_party') THEN
            ALTER TABLE partys ADD COLUMN is_current_party boolean NOT NULL DEFAULT false;
        END IF;
        
        -- Process each user's party data
        DECLARE
            user_record RECORD;
            matching_party_id INTEGER;
            party_slot_count INTEGER;
        BEGIN
            FOR user_record IN 
                SELECT u_id as user_id, party 
                FROM users 
                WHERE party IS NOT NULL 
                AND array_length(party, 1) > 0
            LOOP
                -- Reset variables
                matching_party_id := NULL;
                
                -- Try to find an existing saved party that matches this user's current party array
                -- We'll compare all 6 slots to find an exact match
                SELECT p_id INTO matching_party_id
                FROM partys 
                WHERE u_id = user_record.user_id
                AND is_current_party = false  -- Only check saved parties, not already current ones
                AND (
                    -- Check if all slots match (handling nulls and zeros)
                    COALESCE(slot1, 0) = COALESCE(user_record.party[1], 0) AND
                    COALESCE(slot2, 0) = COALESCE(user_record.party[2], 0) AND
                    COALESCE(slot3, 0) = COALESCE(user_record.party[3], 0) AND
                    COALESCE(slot4, 0) = COALESCE(user_record.party[4], 0) AND
                    COALESCE(slot5, 0) = COALESCE(user_record.party[5], 0) AND
                    COALESCE(slot6, 0) = COALESCE(user_record.party[6], 0)
                )
                LIMIT 1;
                
                IF matching_party_id IS NOT NULL THEN
                    -- Found a matching saved party, mark it as current
                    UPDATE partys 
                    SET is_current_party = true 
                    WHERE p_id = matching_party_id;
                    
                    RAISE NOTICE 'User % - Found matching saved party (ID: %), marked as current', 
                        user_record.user_id, matching_party_id;
                ELSE
                    -- No matching saved party found, check if user has any non-zero Pokemon in party
                    -- Count non-zero values in the party array
                    party_slot_count := (
                        CASE WHEN COALESCE(user_record.party[1], 0) > 0 THEN 1 ELSE 0 END +
                        CASE WHEN COALESCE(user_record.party[2], 0) > 0 THEN 1 ELSE 0 END +
                        CASE WHEN COALESCE(user_record.party[3], 0) > 0 THEN 1 ELSE 0 END +
                        CASE WHEN COALESCE(user_record.party[4], 0) > 0 THEN 1 ELSE 0 END +
                        CASE WHEN COALESCE(user_record.party[5], 0) > 0 THEN 1 ELSE 0 END +
                        CASE WHEN COALESCE(user_record.party[6], 0) > 0 THEN 1 ELSE 0 END
                    );
                    
                    IF party_slot_count > 0 THEN
                        -- Create a new current party from the user's party array data
                        INSERT INTO partys (
                            u_id, name, 
                            slot1, slot2, slot3, slot4, slot5, slot6,
                            quick, is_current_party
                        ) VALUES (
                            user_record.user_id,
                            'Current Party',
                            CASE WHEN user_record.party[1] > 0 THEN user_record.party[1] ELSE NULL END,
                            CASE WHEN user_record.party[2] > 0 THEN user_record.party[2] ELSE NULL END,
                            CASE WHEN user_record.party[3] > 0 THEN user_record.party[3] ELSE NULL END,
                            CASE WHEN user_record.party[4] > 0 THEN user_record.party[4] ELSE NULL END,
                            CASE WHEN user_record.party[5] > 0 THEN user_record.party[5] ELSE NULL END,
                            CASE WHEN user_record.party[6] > 0 THEN user_record.party[6] ELSE NULL END,
                            false,
                            true
                        );
                        
                        RAISE NOTICE 'User % - Created new current party from array data (% Pokemon)', 
                            user_record.user_id, party_slot_count;
                    ELSE
                        -- User has empty party, create empty current party placeholder
                        INSERT INTO partys (
                            u_id, name, 
                            slot1, slot2, slot3, slot4, slot5, slot6,
                            quick, is_current_party
                        ) VALUES (
                            user_record.user_id,
                            'Current Party',
                            NULL, NULL, NULL, NULL, NULL, NULL,
                            false,
                            true
                        );
                        
                        RAISE NOTICE 'User % - Created empty current party placeholder', 
                            user_record.user_id;
                    END IF;
                END IF;
            END LOOP;
        END;
        
        -- Ensure users without any party data get an empty current party
        INSERT INTO partys (u_id, name, slot1, slot2, slot3, slot4, slot5, slot6, quick, is_current_party)
        SELECT 
            u_id,
            'Current Party',
            NULL, NULL, NULL, NULL, NULL, NULL,
            false,
            true
        FROM users 
        WHERE u_id NOT IN (
            SELECT DISTINCT u_id FROM partys WHERE is_current_party = true
        );
        
        RAISE NOTICE 'Created empty current parties for users without party data';
        
        -- Verify migration - count results
        DECLARE
            total_users INTEGER;
            users_with_current_party INTEGER;
            users_with_multiple_current_parties INTEGER;
        BEGIN
            SELECT COUNT(*) INTO total_users FROM users;
            SELECT COUNT(DISTINCT u_id) INTO users_with_current_party FROM partys WHERE is_current_party = true;
            SELECT COUNT(*) INTO users_with_multiple_current_parties FROM (
                SELECT u_id FROM partys WHERE is_current_party = true GROUP BY u_id HAVING COUNT(*) > 1
            ) sub;
            
            RAISE NOTICE 'Migration verification:';
            RAISE NOTICE '  Total users: %', total_users;
            RAISE NOTICE '  Users with current party: %', users_with_current_party;
            RAISE NOTICE '  Users with multiple current parties (ERROR): %', users_with_multiple_current_parties;
            
            IF users_with_multiple_current_parties > 0 THEN
                RAISE WARNING 'Some users have multiple current parties - this should not happen!';
            END IF;
        END;
        
        -- Now drop the party column from users table
        RAISE NOTICE 'Dropping party column from users table';
        ALTER TABLE users DROP COLUMN party;
        
        RAISE NOTICE 'Migration completed successfully - users.party array migrated to parties.is_current_party flag';
        
    ELSE
        RAISE NOTICE 'users.party column does not exist - migration already completed or not needed';
    END IF;
END $$;