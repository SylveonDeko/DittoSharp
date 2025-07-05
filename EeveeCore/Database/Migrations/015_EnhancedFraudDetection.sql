-- Migration: Fraud Detection System
-- This migration adds tables and indexes for advanced fraud detection capabilities

-- Add indexes for efficient fraud network analysis
CREATE INDEX IF NOT EXISTS idx_trade_logs_sender_time ON trade_logs(sender, time);
CREATE INDEX IF NOT EXISTS idx_trade_logs_receiver_time ON trade_logs(receiver, time);
CREATE INDEX IF NOT EXISTS idx_trade_logs_time ON trade_logs(time);

-- Add composite index for user relationship queries
CREATE INDEX IF NOT EXISTS idx_user_trade_relationships_users ON user_trade_relationships(user1_id, user2_id);

-- Add index for Pokemon ownership tracking
CREATE INDEX IF NOT EXISTS idx_user_pokemon_ownership_pokemon ON user_pokemon_ownership(pokemon_id);

-- Add columns to track advanced fraud patterns
ALTER TABLE user_trade_relationships 
ADD COLUMN IF NOT EXISTS chain_trading_score DOUBLE PRECISION DEFAULT 0,
ADD COLUMN IF NOT EXISTS burst_trading_incidents INTEGER DEFAULT 0,
ADD COLUMN IF NOT EXISTS network_connection_strength DOUBLE PRECISION DEFAULT 0,
ADD COLUMN IF NOT EXISTS last_chain_analysis TIMESTAMP,
ADD COLUMN IF NOT EXISTS last_burst_analysis TIMESTAMP;

-- Add table for tracking fraud networks
CREATE TABLE IF NOT EXISTS fraud_networks (
    id SERIAL PRIMARY KEY,
    network_id UUID NOT NULL,
    discovered_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    core_user_ids BIGINT[] NOT NULL,
    network_type VARCHAR(50) NOT NULL,
    estimated_size INTEGER NOT NULL,
    risk_score DOUBLE PRECISION NOT NULL,
    last_activity TIMESTAMP NOT NULL,
    admin_notes TEXT,
    status VARCHAR(50) NOT NULL DEFAULT 'active'
);

CREATE INDEX IF NOT EXISTS idx_fraud_networks_network_id ON fraud_networks(network_id);
CREATE INDEX IF NOT EXISTS idx_fraud_networks_core_users ON fraud_networks USING GIN(core_user_ids);

-- Add table for Pokemon laundering tracking
CREATE TABLE IF NOT EXISTS pokemon_laundering_history (
    id SERIAL PRIMARY KEY,
    pokemon_id BIGINT NOT NULL,
    analysis_timestamp TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ownership_chain_length INTEGER NOT NULL,
    rapid_transfer_count INTEGER NOT NULL,
    circular_path BOOLEAN NOT NULL DEFAULT FALSE,
    estimated_value DECIMAL(20,2),
    risk_score DOUBLE PRECISION NOT NULL,
    flagged BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT fk_pokemon_laundering_pokemon FOREIGN KEY (pokemon_id) REFERENCES pokes(id)
);

CREATE INDEX IF NOT EXISTS idx_pokemon_laundering_pokemon ON pokemon_laundering_history(pokemon_id);
CREATE INDEX IF NOT EXISTS idx_pokemon_laundering_risk ON pokemon_laundering_history(risk_score) WHERE flagged = true;

-- Add table for market manipulation tracking
CREATE TABLE IF NOT EXISTS market_manipulation_events (
    id SERIAL PRIMARY KEY,
    detected_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    manipulation_type VARCHAR(50) NOT NULL, -- 'price_fixing', 'pump_dump', 'wash_trading'
    pokemon_species VARCHAR(100) NOT NULL,
    involved_user_ids BIGINT[] NOT NULL,
    price_data JSONB NOT NULL,
    confidence_score DOUBLE PRECISION NOT NULL,
    admin_reviewed BOOLEAN NOT NULL DEFAULT FALSE,
    action_taken VARCHAR(100)
);

CREATE INDEX IF NOT EXISTS idx_market_manipulation_type ON market_manipulation_events(manipulation_type);
CREATE INDEX IF NOT EXISTS idx_market_manipulation_species ON market_manipulation_events(pokemon_species);
CREATE INDEX IF NOT EXISTS idx_market_manipulation_users ON market_manipulation_events USING GIN(involved_user_ids);

-- Add table for burst trading patterns
CREATE TABLE IF NOT EXISTS burst_trading_patterns (
    id SERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL,
    detected_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    burst_start TIMESTAMP NOT NULL,
    burst_end TIMESTAMP NOT NULL,
    trade_count INTEGER NOT NULL,
    unique_partners INTEGER NOT NULL,
    average_interval_seconds DOUBLE PRECISION NOT NULL,
    risk_score DOUBLE PRECISION NOT NULL,
    pattern_data JSONB
);

CREATE INDEX IF NOT EXISTS idx_burst_trading_user ON burst_trading_patterns(user_id);
CREATE INDEX IF NOT EXISTS idx_burst_trading_time ON burst_trading_patterns(detected_at);

-- Add columns to trade_fraud_detections
ALTER TABLE trade_fraud_detections
ADD COLUMN IF NOT EXISTS chain_trading_detected BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS burst_trading_detected BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS network_fraud_detected BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS market_manipulation_detected BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS pokemon_laundering_detected BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS comprehensive_risk_score DOUBLE PRECISION,
ADD COLUMN IF NOT EXISTS actionable_insights TEXT;

-- Add user risk profile tracking
CREATE TABLE IF NOT EXISTS user_risk_profiles (
    user_id BIGINT PRIMARY KEY,
    last_analyzed TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    total_trades_analyzed INTEGER NOT NULL DEFAULT 0,
    average_risk_score DOUBLE PRECISION NOT NULL DEFAULT 0,
    high_risk_trade_count INTEGER NOT NULL DEFAULT 0,
    chain_trading_incidents INTEGER NOT NULL DEFAULT 0,
    burst_trading_incidents INTEGER NOT NULL DEFAULT 0,
    market_manipulation_incidents INTEGER NOT NULL DEFAULT 0,
    risk_score_history DOUBLE PRECISION[] DEFAULT '{}',
    profile_data JSONB,
    CONSTRAINT fk_user_risk_profile_user FOREIGN KEY (user_id) REFERENCES users(u_id)
);

-- Add function to calculate trading velocity
CREATE OR REPLACE FUNCTION calculate_trading_velocity(p_user_id BIGINT, p_time_window INTERVAL)
RETURNS TABLE(trade_count INTEGER, unique_partners INTEGER, avg_interval DOUBLE PRECISION) AS $$
BEGIN
    RETURN QUERY
    WITH user_trades AS (
        SELECT 
            time,
            CASE 
                WHEN sender = p_user_id THEN receiver 
                ELSE sender 
            END AS partner_id
        FROM trade_logs
        WHERE (sender = p_user_id OR receiver = p_user_id)
            AND time >= CURRENT_TIMESTAMP - p_time_window
        ORDER BY time
    ),
    trade_intervals AS (
        SELECT 
            time - LAG(time) OVER (ORDER BY time) AS interval
        FROM user_trades
    )
    SELECT 
        COUNT(*)::INTEGER AS trade_count,
        COUNT(DISTINCT partner_id)::INTEGER AS unique_partners,
        COALESCE(AVG(EXTRACT(EPOCH FROM interval)), 0)::DOUBLE PRECISION AS avg_interval
    FROM user_trades
    LEFT JOIN trade_intervals ON TRUE;
END;
$$ LANGUAGE plpgsql;

-- Add function to detect circular trading paths
CREATE OR REPLACE FUNCTION detect_circular_trading(p_user_id BIGINT, p_depth INTEGER DEFAULT 5)
RETURNS TABLE(path_users BIGINT[], path_length INTEGER, is_circular BOOLEAN) AS $$
WITH RECURSIVE trade_paths AS (
    -- Base case: start from the given user
    SELECT 
        ARRAY[p_user_id] AS path,
        p_user_id AS current_user,
        1 AS depth
    
    UNION ALL
    
    -- Recursive case: follow trade connections
    SELECT 
        tp.path || t.other_user,
        t.other_user,
        tp.depth + 1
    FROM trade_paths tp
    CROSS JOIN LATERAL (
        SELECT DISTINCT
            CASE 
                WHEN sender = tp.current_user THEN receiver 
                ELSE sender 
            END AS other_user
        FROM trade_logs
        WHERE (sender = tp.current_user OR receiver = tp.current_user)
            AND time >= CURRENT_TIMESTAMP - INTERVAL '7 days'
    ) t
    WHERE tp.depth < p_depth
        AND NOT t.other_user = ANY(tp.path[2:]) -- Avoid revisiting nodes (except first)
)
SELECT 
    path AS path_users,
    array_length(path, 1) AS path_length,
    path[1] = path[array_length(path, 1)] AS is_circular
FROM trade_paths
WHERE array_length(path, 1) >= 3
    AND (path[1] = path[array_length(path, 1)] OR array_length(path, 1) = p_depth);
$$ LANGUAGE sql;

-- Add materialized view for fast fraud network detection
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_trading_network_graph AS
WITH trade_intervals AS (
    SELECT 
        LEAST(sender, receiver) AS user1,
        GREATEST(sender, receiver) AS user2,
        time,
        LAG(time) OVER (PARTITION BY LEAST(sender, receiver), GREATEST(sender, receiver) ORDER BY time) AS prev_time
    FROM trade_logs
    WHERE time >= CURRENT_TIMESTAMP - INTERVAL '30 days'
),
recent_trades AS (
    SELECT 
        user1,
        user2,
        COUNT(*) AS trade_count,
        MAX(time) AS last_trade,
        AVG(CASE 
            WHEN prev_time IS NOT NULL 
            THEN EXTRACT(EPOCH FROM time - prev_time)
            ELSE NULL 
        END) AS avg_interval
    FROM trade_intervals
    GROUP BY user1, user2
    HAVING COUNT(*) >= 3
)
SELECT 
    user1,
    user2,
    trade_count,
    last_trade,
    avg_interval,
    CASE 
        WHEN trade_count >= 10 AND avg_interval < 3600 THEN 'high'
        WHEN trade_count >= 5 AND avg_interval < 7200 THEN 'medium'
        ELSE 'low'
    END AS connection_strength
FROM recent_trades;

CREATE INDEX IF NOT EXISTS idx_mv_trading_network_user1 ON mv_trading_network_graph(user1);
CREATE INDEX IF NOT EXISTS idx_mv_trading_network_user2 ON mv_trading_network_graph(user2);
CREATE INDEX IF NOT EXISTS idx_mv_trading_network_strength ON mv_trading_network_graph(connection_strength);

-- Add trigger to auto-refresh the materialized view
CREATE OR REPLACE FUNCTION refresh_trading_network_graph()
RETURNS trigger AS $$
BEGIN
    -- Refresh asynchronously to avoid blocking
    PERFORM pg_notify('refresh_mv_trading_network', '');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger on trade_logs (only refresh periodically, not on every insert)
-- This would be handled by a background job in production

-- Add comments for documentation
COMMENT ON TABLE fraud_networks IS 'Tracks detected fraud networks and coordinated trading groups';
COMMENT ON TABLE pokemon_laundering_history IS 'Historical analysis of Pokemon movements to detect laundering patterns';
COMMENT ON TABLE market_manipulation_events IS 'Records detected market manipulation attempts';
COMMENT ON TABLE burst_trading_patterns IS 'Tracks rapid successive trading patterns that may indicate automation';
COMMENT ON TABLE user_risk_profiles IS 'Maintains risk profiles for users based on their trading history';
COMMENT ON FUNCTION calculate_trading_velocity IS 'Calculates trading velocity metrics for fraud detection';
COMMENT ON FUNCTION detect_circular_trading IS 'Detects circular trading paths that may indicate coordinated fraud';

-- Add trade ban columns to users table
ALTER TABLE users 
ADD COLUMN IF NOT EXISTS trade_banned BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS trade_ban_reason TEXT,
ADD COLUMN IF NOT EXISTS trade_ban_date TIMESTAMP,
ADD COLUMN IF NOT EXISTS trade_ban_fraud_id INTEGER REFERENCES trade_fraud_detections(id),
ADD COLUMN IF NOT EXISTS market_banned BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS market_ban_reason TEXT,
ADD COLUMN IF NOT EXISTS market_ban_date TIMESTAMP;

-- Add index for banned users
CREATE INDEX IF NOT EXISTS idx_users_trade_banned ON users(trade_banned) WHERE trade_banned = TRUE;
CREATE INDEX IF NOT EXISTS idx_users_market_banned ON users(market_banned) WHERE market_banned = TRUE;