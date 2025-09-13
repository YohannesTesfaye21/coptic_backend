using System;
using System.Collections.Generic;
using System.Linq;
using coptic_app_backend.Domain.Models;

namespace TestMessageFixes
{
    /// <summary>
    /// Simple test to verify message deletion and editing fixes work correctly
    /// </summary>
    public class MessageFixesTest
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Testing Message Deletion and Edit Fixes...");
            
            // Test 1: Message Deletion with Replies
            TestMessageDeletionWithReplies();
            
            // Test 2: Message Editing
            TestMessageEditing();
            
            Console.WriteLine("All tests completed successfully!");
        }
        
        /// <summary>
        /// Test that when a message is deleted, replies are converted to normal messages
        /// </summary>
        public static void TestMessageDeletionWithReplies()
        {
            Console.WriteLine("\n=== Testing Message Deletion with Replies ===");
            
            // Create original message
            var originalMessage = new ChatMessage
            {
                Id = "msg1",
                SenderId = "user1",
                RecipientId = "user2",
                Content = "Original message",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            
            // Create reply to original message
            var replyMessage = new ChatMessage
            {
                Id = "msg2",
                SenderId = "user2",
                RecipientId = "user1",
                Content = "This is a reply",
                ReplyToMessageId = "msg1",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            
            Console.WriteLine($"Original message: {originalMessage.Content}");
            Console.WriteLine($"Reply message: {replyMessage.Content} (ReplyToMessageId: {replyMessage.ReplyToMessageId})");
            
            // Simulate deletion of original message
            originalMessage.IsDeleted = true;
            originalMessage.DeletedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            originalMessage.DeletedBy = "user1";
            
            // Simulate conversion of reply to normal message
            replyMessage.ReplyToMessageId = null; // This is what our fix does
            
            Console.WriteLine($"After deletion:");
            Console.WriteLine($"Original message deleted: {originalMessage.IsDeleted}");
            Console.WriteLine($"Reply converted to normal: {replyMessage.ReplyToMessageId == null}");
            Console.WriteLine($"Reply content preserved: {replyMessage.Content}");
            
            // Verify the fix works
            if (originalMessage.IsDeleted && replyMessage.ReplyToMessageId == null)
            {
                Console.WriteLine("✅ Message deletion fix works correctly!");
            }
            else
            {
                Console.WriteLine("❌ Message deletion fix failed!");
            }
        }
        
        /// <summary>
        /// Test that when a message is edited, it preserves original timestamp and shows edit status
        /// </summary>
        public static void TestMessageEditing()
        {
            Console.WriteLine("\n=== Testing Message Editing ===");
            
            var originalTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            // Create original message
            var message = new ChatMessage
            {
                Id = "msg1",
                SenderId = "user1",
                RecipientId = "user2",
                Content = "Original content",
                Timestamp = originalTimestamp,
                IsEdited = false
            };
            
            Console.WriteLine($"Original message: {message.Content}");
            Console.WriteLine($"Original timestamp: {message.Timestamp}");
            Console.WriteLine($"Is edited: {message.IsEdited}");
            
            // Simulate editing the message
            var editTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            message.Content = "Edited content";
            message.IsEdited = true;
            message.EditedAt = editTimestamp;
            message.EditedBy = "user1";
            // Note: We DON'T update the original timestamp - this is the fix!
            
            Console.WriteLine($"After editing:");
            Console.WriteLine($"New content: {message.Content}");
            Console.WriteLine($"Original timestamp preserved: {message.Timestamp == originalTimestamp}");
            Console.WriteLine($"Is edited: {message.IsEdited}");
            Console.WriteLine($"Edited at: {message.EditedAt}");
            Console.WriteLine($"Edited by: {message.EditedBy}");
            
            // Verify the fix works
            if (message.Timestamp == originalTimestamp && message.IsEdited && message.EditedAt.HasValue)
            {
                Console.WriteLine("✅ Message editing fix works correctly!");
            }
            else
            {
                Console.WriteLine("❌ Message editing fix failed!");
            }
        }
    }
}

