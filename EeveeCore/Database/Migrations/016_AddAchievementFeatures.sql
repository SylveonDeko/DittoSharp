-- Migration: Add achievement system features
-- Date: 2024
-- Description: Adds missing achievement columns and milestone tracking support

-- Add missing game achievement columns to achievements table
ALTER TABLE achievements 
ADD COLUMN IF NOT EXISTS game_wordsearch INTEGER DEFAULT 0,
ADD COLUMN IF NOT EXISTS game_slots INTEGER DEFAULT 0,
ADD COLUMN IF NOT EXISTS game_slots_win INTEGER DEFAULT 0;

-- Create milestone_progress table for tracking completed milestones
CREATE TABLE IF NOT EXISTS milestone_progress (
    u_id BIGINT NOT NULL,
    achievement_type VARCHAR(50) NOT NULL,
    milestone_value INTEGER NOT NULL,
    completed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (u_id, achievement_type, milestone_value),
    FOREIGN KEY (u_id) REFERENCES users(u_id) ON DELETE CASCADE
);

-- Create user_loyalty table for loyalty points and daily streaks
CREATE TABLE IF NOT EXISTS user_loyalty (
    u_id BIGINT PRIMARY KEY,
    loyalty_points INTEGER DEFAULT 0,
    daily_streak INTEGER DEFAULT 0,
    last_login DATE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (u_id) REFERENCES users(u_id) ON DELETE CASCADE
);

-- Add indexes for performance
CREATE INDEX IF NOT EXISTS idx_milestone_progress_user_achievement 
ON milestone_progress(u_id, achievement_type);

CREATE INDEX IF NOT EXISTS idx_milestone_progress_completed_at 
ON milestone_progress(completed_at);

CREATE INDEX IF NOT EXISTS idx_user_loyalty_last_login 
ON user_loyalty(last_login);

-- Add comments for documentation
COMMENT ON TABLE milestone_progress IS 'Tracks which achievement milestones users have completed';
COMMENT ON TABLE user_loyalty IS 'Tracks user loyalty points and daily login streaks';

COMMENT ON COLUMN achievements.game_wordsearch IS 'Number of word search games completed';
COMMENT ON COLUMN achievements.game_slots IS 'Number of slot machine games played';
COMMENT ON COLUMN achievements.game_slots_win IS 'Number of slot machine games won';

COMMENT ON COLUMN milestone_progress.achievement_type IS 'Type of achievement (e.g., pokemon_caught, breed_hexa)';
COMMENT ON COLUMN milestone_progress.milestone_value IS 'The milestone threshold that was reached';
COMMENT ON COLUMN milestone_progress.completed_at IS 'When the milestone was first completed';

COMMENT ON COLUMN user_loyalty.loyalty_points IS 'Total loyalty points accumulated';
COMMENT ON COLUMN user_loyalty.daily_streak IS 'Current consecutive daily login streak';
COMMENT ON COLUMN user_loyalty.last_login IS 'Date of last login for streak calculation';