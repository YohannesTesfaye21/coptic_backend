-- Add ConversationId column to ChatMessages table
ALTER TABLE "ChatMessages" ADD COLUMN IF NOT EXISTS "ConversationId" text DEFAULT '';
