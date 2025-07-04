-- Migration 004: Add TradeLogTradeId foreign key to trade_fraud_detections
-- This migration adds the missing foreign key column for the Entity Framework
-- navigation property between TradeFraudDetection and TradeLog

-- Add the TradeLog foreign key column (Entity Framework shadow property)
ALTER TABLE trade_fraud_detections 
ADD COLUMN "TradeLogTradeId" INTEGER;

-- Add the TradeLog foreign key constraint (if trade_logs table exists)
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'trade_logs') THEN
        ALTER TABLE trade_fraud_detections 
        ADD CONSTRAINT fk_trade_fraud_detections_trade_log 
        FOREIGN KEY ("TradeLogTradeId") REFERENCES trade_logs(t_id) ON DELETE SET NULL;
    END IF;
END $$;

-- Add index for performance
CREATE INDEX idx_trade_fraud_detections_trade_log_id ON trade_fraud_detections("TradeLogTradeId");