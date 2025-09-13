-- Fix StorageType for existing files that were uploaded to MinIO
-- This script updates all existing MediaFile records to have StorageType = 'MinIO'
-- since they were uploaded before the StorageType field was added

UPDATE "MediaFiles" 
SET "StorageType" = 'MinIO' 
WHERE "StorageType" = 'Local' 
  AND "ObjectName" LIKE 'uploads/%';

-- Verify the update
SELECT "FileName", "ObjectName", "StorageType" 
FROM "MediaFiles" 
WHERE "IsActive" = true 
ORDER BY "UploadedAt" DESC;
