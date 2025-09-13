-- Add edit tracking fields to ChatMessages table
-- This migration adds fields to track when a message was edited

ALTER TABLE "ChatMessages" 
ADD COLUMN "IsEdited" boolean NOT NULL DEFAULT false,
ADD COLUMN "EditedAt" bigint NULL,
ADD COLUMN "EditedBy" text NULL;

-- Add comments for documentation
COMMENT ON COLUMN "ChatMessages"."IsEdited" IS 'Whether the message was edited';
COMMENT ON COLUMN "ChatMessages"."EditedAt" IS 'Unix timestamp when the message was edited';
COMMENT ON COLUMN "ChatMessages"."EditedBy" IS 'User ID who edited the message';

