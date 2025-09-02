using System.Collections.Generic;

namespace coptic_app_backend.Domain.Models
{
    public enum UserType
    {
        Regular = 0,
        Abune = 1
    }

    public enum UserStatus
    {
        Active = 0,
        Inactive = 1,
        Suspended = 2,
        PendingApproval = 3
    }

    public class User
    {
        public string? Id { get; set; } // Primary key
        public string? Username { get; set; } // Username (usually email)
        public string? Email { get; set; } // Email address
        public string? PhoneNumber { get; set; } // Phone number
        public string? Name { get; set; } // Full name
        public string? Gender { get; set; } // User gender
        public string? DeviceToken { get; set; } // FCM device token for notifications
        public string? PasswordHash { get; set; } // Hashed password for local authentication
        public bool EmailVerified { get; set; } = false; // Whether email is verified
        public bool PhoneNumberVerified { get; set; } = false; // Whether phone number is verified
        public long CreatedAt { get; set; } // User creation timestamp
        public long LastModified { get; set; } // Last modification timestamp
        
        // Hierarchical user system fields
        public UserType UserType { get; set; } = UserType.Regular; // Abune or Regular user
        public UserStatus UserStatus { get; set; } = UserStatus.PendingApproval; // User approval status
        public string? AbuneId { get; set; } // ID of the Abune this user belongs to (null for Abune users)
        public string? ChurchName { get; set; } // Church/Community name (for Abune users)
        public string? Location { get; set; } // Geographic location (for Abune users)
        public string? ProfileImageUrl { get; set; } // Profile picture URL
        public string? Bio { get; set; } // User biography/description
        public bool IsApproved { get; set; } = false; // Whether user is approved by Abune
        public long? ApprovedAt { get; set; } // When user was approved
        public string? ApprovedBy { get; set; } // Who approved the user (Abune ID)
        
        // Navigation properties (for Entity Framework)
        public User? Abune { get; set; } // The Abune this user belongs to
    }
}
