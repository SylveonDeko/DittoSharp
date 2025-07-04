-- Migration: Fix Market Table Identity Column

-- Drop and recreate the market table with proper identity column
DROP TABLE IF EXISTS market CASCADE;

-- Create new market table with proper IDENTITY column
CREATE TABLE market (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    poke INTEGER NOT NULL,                          -- Pokemon ID (Foreign key to pokes.id)
    owner BIGINT NOT NULL,                          -- Seller Discord user ID  
    price INTEGER NOT NULL,                         -- Price in coins
    buyer BIGINT NULL,                              -- Buyer Discord user ID (NULL = active, 0 = removed, user_id = sold)
    listed_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,  -- When the listing was created
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP, -- When the listing was last modified
    view_count INTEGER NOT NULL DEFAULT 0,         -- Number of times this listing has been viewed
    
    -- Constraints
    CONSTRAINT market_poke_fkey FOREIGN KEY (poke) REFERENCES pokes(id) ON DELETE CASCADE,
    CONSTRAINT market_price_positive CHECK (price > 0),
    CONSTRAINT market_view_count_positive CHECK (view_count >= 0)
);

-- Create indexes for efficient querying
CREATE INDEX idx_market_active_listings ON market (listed_at DESC) WHERE buyer IS NULL;
CREATE INDEX idx_market_owner ON market (owner);
CREATE INDEX idx_market_price ON market (price);
CREATE INDEX idx_market_pokemon ON market (poke);
CREATE INDEX idx_market_listed_at ON market (listed_at);
CREATE INDEX idx_market_buyer ON market (buyer);

-- Create function to update the updated_at timestamp automatically
CREATE OR REPLACE FUNCTION update_market_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger to automatically update updated_at on price changes
CREATE TRIGGER market_update_timestamp
    BEFORE UPDATE ON market
    FOR EACH ROW
    EXECUTE FUNCTION update_market_updated_at();

-- Add comments for documentation
COMMENT ON TABLE market IS 'Pokemon marketplace listings with fraud detection support';
COMMENT ON COLUMN market.id IS 'Unique listing identifier (auto-generated)';
COMMENT ON COLUMN market.poke IS 'Pokemon ID being sold';
COMMENT ON COLUMN market.owner IS 'Discord user ID of the seller';
COMMENT ON COLUMN market.price IS 'Listing price in coins';
COMMENT ON COLUMN market.buyer IS 'Discord user ID of buyer (NULL=active, 0=removed, user_id=sold)';
COMMENT ON COLUMN market.listed_at IS 'Timestamp when the listing was created';
COMMENT ON COLUMN market.updated_at IS 'Timestamp when the listing was last modified (triggers on price changes)';
COMMENT ON COLUMN market.view_count IS 'Number of times this listing has been viewed';