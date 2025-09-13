# Message Deletion Fix

## Problem
When a message was deleted, replies to that message would still exist in the database, creating orphaned replies that referenced a deleted message. This caused issues because:

1. The `GetMessageByIdAsync` method filters out deleted messages
2. When trying to reply to a deleted message, the system would return "Original message not found"
3. Replies to deleted messages would still be visible in conversation views

## Solution
Implemented conversion of replies to normal messages when the original message is deleted:

### Changes Made

1. **Modified `DeleteMessageAsync` in PostgreSQLChatRepository.cs**:
   - When deleting a message, find all replies to that message
   - Convert replies to normal messages by removing the `ReplyToMessageId` reference
   - This preserves the reply content while removing the broken reference

2. **Added `GetMessageByIdIncludingDeletedAsync` method**:
   - New method that can retrieve messages including deleted ones
   - Used for validation when replying or forwarding messages

3. **Updated `ReplyToMessageAsync` and `ForwardMessageAsync`**:
   - Allow replying to and forwarding deleted messages since replies are converted to normal messages
   - This maintains conversation flow and user experience

### Code Changes

#### PostgreSQLChatRepository.cs
```csharp
public async Task<bool> DeleteMessageAsync(string messageId, string deletedBy)
{
    var message = await _context.ChatMessages.FindAsync(messageId);
    if (message == null) return false;

    // Mark the original message as deleted
    message.IsDeleted = true;
    message.DeletedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    message.DeletedBy = deletedBy;

    // Find all replies to this message and convert them to normal messages
    var replies = await _context.ChatMessages
        .Where(m => m.ReplyToMessageId == messageId && !m.IsDeleted)
        .ToListAsync();

    foreach (var reply in replies)
    {
        // Convert reply to normal message by removing the ReplyToMessageId reference
        reply.ReplyToMessageId = null;
    }

    await _context.SaveChangesAsync();
    return true;
}
```

#### ChatService.cs
```csharp
// Note: We allow replying to deleted messages since replies will be converted to normal messages
// when the original message is deleted, so this maintains conversation flow
```

## Benefits
1. **Data Consistency**: No more orphaned replies in the database
2. **Better User Experience**: Reply content is preserved when original message is deleted
3. **Conversation Flow**: Users can still reply to deleted messages, maintaining natural conversation flow
4. **Content Preservation**: Important reply content is not lost when original message is deleted
5. **Maintains Audit Trail**: All deletions are properly tracked with timestamps and user information

## Testing
To test the fix:
1. Send a message
2. Reply to that message
3. Delete the original message
4. Verify that the reply is converted to a normal message (no ReplyToMessageId)
5. Verify that you can still reply to the deleted message (reply will be converted to normal message)

## Backward Compatibility
This fix is backward compatible and doesn't require any database migrations. Existing orphaned replies will be handled gracefully by the new validation logic.
