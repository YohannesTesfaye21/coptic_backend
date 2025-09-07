-- Fix database schema issues
-- First, let's check if the column exists and add it if it doesn't
DO $$ 
BEGIN
    -- Add ConversationId column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'ChatMessages' 
        AND column_name = 'ConversationId'
    ) THEN
        ALTER TABLE "ChatMessages" ADD COLUMN "ConversationId" text;
    END IF;
    
    -- Update existing messages to have a default conversation ID
    UPDATE "ChatMessages" 
    SET "ConversationId" = 'default-conversation-' || "Id" 
    WHERE "ConversationId" IS NULL;
END $$;
