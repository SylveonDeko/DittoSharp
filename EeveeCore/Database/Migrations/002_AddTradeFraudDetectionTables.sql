-- Migration 002: Add Trade Fraud Detection Tables
-- This migration adds the tables required for the trade fraud detection system

-- Create suspicious_trade_analytics table
CREATE TABLE suspicious_trade_analytics (
    id SERIAL PRIMARY KEY,
    trade_id INTEGER NOT NULL,
    analysis_timestamp TIMESTAMP NOT NULL,
    
    -- Risk Scores
    overall_risk_score DOUBLE PRECISION NOT NULL,
    value_imbalance_score DOUBLE PRECISION NOT NULL,
    relationship_risk_score DOUBLE PRECISION NOT NULL,
    behavioral_risk_score DOUBLE PRECISION NOT NULL,
    account_age_risk_score DOUBLE PRECISION NOT NULL,
    
    -- Trade Value Analysis
    sender_total_value DECIMAL(20,2) NOT NULL,
    receiver_total_value DECIMAL(20,2) NOT NULL,
    value_ratio DOUBLE PRECISION NOT NULL,
    value_difference DECIMAL(20,2) NOT NULL,
    
    -- Account Analysis
    sender_account_age_days INTEGER NOT NULL,
    receiver_account_age_days INTEGER NOT NULL,
    previous_trades_count INTEGER NOT NULL,
    previous_total_value DECIMAL(20,2) NOT NULL,
    
    -- Detection Flags
    flagged_alt_account BOOLEAN NOT NULL DEFAULT FALSE,
    flagged_rmt BOOLEAN NOT NULL DEFAULT FALSE,
    flagged_newbie_exploitation BOOLEAN NOT NULL DEFAULT FALSE,
    flagged_unusual_behavior BOOLEAN NOT NULL DEFAULT FALSE,
    flagged_bot_activity BOOLEAN NOT NULL DEFAULT FALSE,
    
    -- Additional Metadata
    analysis_notes TEXT,
    admin_reviewed BOOLEAN NOT NULL DEFAULT FALSE,
    admin_verdict BOOLEAN,
    admin_notes TEXT,
    admin_review_timestamp TIMESTAMP
);

-- Create user_trade_relationships table
CREATE TABLE user_trade_relationships (
    id SERIAL PRIMARY KEY,
    user1_id BIGINT NOT NULL,
    user2_id BIGINT NOT NULL,
    
    -- Trade Statistics
    total_trades INTEGER NOT NULL DEFAULT 0,
    first_trade_timestamp TIMESTAMP NOT NULL,
    last_trade_timestamp TIMESTAMP NOT NULL,
    user1_total_given_value DECIMAL(20,2) NOT NULL DEFAULT 0,
    user2_total_given_value DECIMAL(20,2) NOT NULL DEFAULT 0,
    user1_favoring_trades INTEGER NOT NULL DEFAULT 0,
    user2_favoring_trades INTEGER NOT NULL DEFAULT 0,
    balanced_trades INTEGER NOT NULL DEFAULT 0,
    
    -- Risk Analysis
    relationship_risk_score DOUBLE PRECISION NOT NULL DEFAULT 0,
    value_imbalance_ratio DOUBLE PRECISION NOT NULL DEFAULT 1,
    trading_frequency_score DOUBLE PRECISION NOT NULL DEFAULT 0,
    flagged_potential_alts BOOLEAN NOT NULL DEFAULT FALSE,
    flagged_potential_rmt BOOLEAN NOT NULL DEFAULT FALSE,
    flagged_newbie_exploitation BOOLEAN NOT NULL DEFAULT FALSE,
    
    -- Account Age Analysis
    account_age_difference_days INTEGER NOT NULL DEFAULT 0,
    suspicious_creation_timing BOOLEAN NOT NULL DEFAULT FALSE,
    newer_account_age_at_first_trade INTEGER NOT NULL DEFAULT 0,
    
    -- Temporal Analysis
    average_trade_interval_hours DOUBLE PRECISION NOT NULL DEFAULT 0,
    trade_interval_std_dev DOUBLE PRECISION NOT NULL DEFAULT 0,
    suspicious_timing_pattern BOOLEAN NOT NULL DEFAULT FALSE,
    
    -- Metadata
    last_updated TIMESTAMP NOT NULL,
    admin_reviewed BOOLEAN NOT NULL DEFAULT FALSE,
    admin_verdict BOOLEAN,
    admin_notes TEXT,
    whitelisted BOOLEAN NOT NULL DEFAULT FALSE,
    
    -- Ensure user1_id is always less than user2_id for consistency
    CONSTRAINT check_user_order CHECK (user1_id < user2_id),
    CONSTRAINT unique_user_pair UNIQUE (user1_id, user2_id)
);

-- Create trade_fraud_detections table
CREATE TABLE trade_fraud_detections (
    id SERIAL PRIMARY KEY,
    trade_id INTEGER,
    detection_timestamp TIMESTAMP NOT NULL,
    
    -- Involved Users
    primary_user_id BIGINT NOT NULL,
    secondary_user_id BIGINT,
    additional_user_ids TEXT, -- JSON array of additional user IDs
    
    -- Detection Details
    fraud_type INTEGER NOT NULL, -- Maps to FraudType enum
    confidence_level DOUBLE PRECISION NOT NULL,
    risk_score DOUBLE PRECISION NOT NULL,
    triggered_rules TEXT NOT NULL, -- JSON array of triggered rule names
    detection_details TEXT, -- JSON object with detailed analysis
    
    -- Action Taken
    automated_action INTEGER NOT NULL, -- Maps to AutomatedAction enum
    trade_blocked BOOLEAN NOT NULL DEFAULT FALSE,
    users_notified BOOLEAN NOT NULL DEFAULT FALSE,
    admin_alerted BOOLEAN NOT NULL DEFAULT FALSE,
    
    -- Investigation and Resolution
    investigation_status INTEGER NOT NULL DEFAULT 1, -- Maps to InvestigationStatus enum
    assigned_admin_id BIGINT,
    investigation_started TIMESTAMP,
    resolution_timestamp TIMESTAMP,
    final_verdict INTEGER, -- Maps to FraudVerdict enum
    admin_notes TEXT,
    admin_actions TEXT,
    
    -- False Positive Analysis
    false_positive BOOLEAN NOT NULL DEFAULT FALSE,
    false_positive_reason TEXT,
    requires_rule_adjustment BOOLEAN NOT NULL DEFAULT FALSE
);

-- Create indexes for performance
CREATE INDEX idx_suspicious_trade_analytics_trade_id ON suspicious_trade_analytics(trade_id);
CREATE INDEX idx_suspicious_trade_analytics_timestamp ON suspicious_trade_analytics(analysis_timestamp);
CREATE INDEX idx_suspicious_trade_analytics_risk_score ON suspicious_trade_analytics(overall_risk_score);
CREATE INDEX idx_suspicious_trade_analytics_flags ON suspicious_trade_analytics(flagged_alt_account, flagged_rmt, flagged_newbie_exploitation);

CREATE INDEX idx_user_trade_relationships_users ON user_trade_relationships(user1_id, user2_id);
CREATE INDEX idx_user_trade_relationships_risk ON user_trade_relationships(relationship_risk_score);
CREATE INDEX idx_user_trade_relationships_flags ON user_trade_relationships(flagged_potential_alts, flagged_potential_rmt);
CREATE INDEX idx_user_trade_relationships_updated ON user_trade_relationships(last_updated);

CREATE INDEX idx_trade_fraud_detections_timestamp ON trade_fraud_detections(detection_timestamp);
CREATE INDEX idx_trade_fraud_detections_primary_user ON trade_fraud_detections(primary_user_id);
CREATE INDEX idx_trade_fraud_detections_trade_id ON trade_fraud_detections(trade_id);
CREATE INDEX idx_trade_fraud_detections_fraud_type ON trade_fraud_detections(fraud_type);
CREATE INDEX idx_trade_fraud_detections_status ON trade_fraud_detections(investigation_status);
CREATE INDEX idx_trade_fraud_detections_risk_score ON trade_fraud_detections(risk_score);

-- Add foreign key constraints (if the trade_logs table exists)
-- Note: These will only be added if the referenced table exists
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'trade_logs') THEN
        ALTER TABLE suspicious_trade_analytics 
            ADD CONSTRAINT fk_suspicious_trade_analytics_trade_id 
            FOREIGN KEY (trade_id) REFERENCES trade_logs(t_id) ON DELETE CASCADE;
        
        ALTER TABLE trade_fraud_detections 
            ADD CONSTRAINT fk_trade_fraud_detections_trade_id 
            FOREIGN KEY (trade_id) REFERENCES trade_logs(t_id) ON DELETE SET NULL;
    END IF;
END $$;