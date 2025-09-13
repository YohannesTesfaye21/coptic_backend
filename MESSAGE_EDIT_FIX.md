# Message Edit Fix

## Problem
When editing a message, it was being treated as a new message because:
1. The `Timestamp` was being updated to the current time
2. This made edited messages appear at the top of the conversation
3. No tracking of edit status or edit history

## Solution
Implemented proper message editing that preserves the original message position and tracks edit status:

### Changes Made

1. **Added Edit Tracking Fields to ChatMessage Model**:
   - `IsEdited`: Boolean flag indicating if message was edited
   - `EditedAt`: Unix timestamp when the message was edited
   - `EditedBy`: User ID who edited the message

2. **Fixed EditMessageAsync in ChatService**:
   - Removed timestamp update to preserve original message position
   - Added edit tracking fields when message is edited
   - Keeps original timestamp for proper conversation ordering

3. **Updated UpdateMessageAsync in Repository**:
   - Now handles the new edit tracking fields
   - Properly saves edit status to database

4. **Enhanced API Response**:
   - Returns edit status information in API responses
   - Frontend can show "edited" indicator

### Code Changes

#### ChatMessage.cs
```csharp
/// <summary>
/// Whether message was edited
/// </summary>
public bool IsEdited { get; set; } = false;

/// <summary>
/// Edit timestamp
/// </summary>
public long? EditedAt { get; set; }

/// <summary>
/// User who edited the message
/// </summary>
public string? EditedBy { get; set; }
```

#### ChatService.cs
```csharp
// Update message content - keep original timestamp, add edited timestamp
message.Content = newContent;
message.IsEdited = true;
message.EditedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
message.EditedBy = userId;
// Don't update the original timestamp - this keeps the message in its original position
```

#### API Response
```json
{
  "id": "message-id",
  "content": "Edited content",
  "timestamp": 1234567890,  // Original timestamp preserved
  "isEdited": true,
  "editedAt": 1234567891,   // When it was edited
  "editedBy": "user-id"     // Who edited it
}
```

## Benefits
1. **Preserves Message Order**: Edited messages stay in their original position
2. **Edit Tracking**: Clear indication when a message was edited
3. **Audit Trail**: Know who edited what and when
4. **Better UX**: Messages don't jump around when edited
5. **Transparency**: Users can see edit status

## Database Migration
Run the SQL script `add_edit_fields.sql` to add the new columns:
```sql
ALTER TABLE "ChatMessages" 
ADD COLUMN "IsEdited" boolean NOT NULL DEFAULT false,
ADD COLUMN "EditedAt" bigint NULL,
ADD COLUMN "EditedBy" text NULL;
```

## Testing
Use the test script `test_message_edit_fix.http` to verify:
1. Message editing preserves original position
2. Edit status is properly tracked
3. API returns edit information
4. Messages stay in chronological order

## Frontend Integration
The frontend can now:
- Show "edited" indicator on edited messages
- Display edit timestamp if needed
- Show who edited the message
- Maintain proper message ordering

