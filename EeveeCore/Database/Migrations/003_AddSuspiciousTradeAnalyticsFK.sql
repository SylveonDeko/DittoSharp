-- Migration 003: Add SuspiciousTradeAnalyticsId foreign key to trade_fraud_detections
-- This migration adds the missing foreign key column for the relationship between
-- TradeFraudDetection and SuspiciousTradeAnalytics

-- Add the foreign key column
ALTER TABLE trade_fraud_detections 
ADD COLUMN suspicious_trade_analytics_id INTEGER;

-- Add the foreign key constraint
ALTER TABLE trade_fraud_detections 
ADD CONSTRAINT fk_trade_fraud_detections_suspicious_analytics 
FOREIGN KEY (suspicious_trade_analytics_id) REFERENCES suspicious_trade_analytics(id) ON DELETE SET NULL;

-- Add index for performance
CREATE INDEX idx_trade_fraud_detections_analytics_id ON trade_fraud_detections(suspicious_trade_analytics_id);