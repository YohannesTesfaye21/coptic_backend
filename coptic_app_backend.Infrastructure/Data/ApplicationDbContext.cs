using Microsoft.EntityFrameworkCore;
using coptic_app_backend.Domain.Models;

namespace coptic_app_backend.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        
        public DbSet<User> Users { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ChatConversation> ChatConversations { get; set; }
        public DbSet<Folder> Folders { get; set; }
        public DbSet<MediaFile> MediaFiles { get; set; }
        // public DbSet<FileUpload> FileUploads { get; set; }
        // public DbSet<UploadSession> UploadSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(450);
                entity.Property(e => e.Username).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Name).HasMaxLength(255);
                entity.Property(e => e.Gender).HasMaxLength(50);
                entity.Property(e => e.DeviceToken).HasMaxLength(500);
                entity.Property(e => e.PasswordHash).HasMaxLength(500);
                entity.Property(e => e.ChurchName).HasMaxLength(255);
                entity.Property(e => e.Location).HasMaxLength(255);
                entity.Property(e => e.ProfileImageUrl).HasMaxLength(1000);
                entity.Property(e => e.Bio).HasMaxLength(1000);
                entity.Property(e => e.AbuneId).HasMaxLength(450);
                entity.Property(e => e.ApprovedBy).HasMaxLength(450);
                
                // Configure enums
                entity.Property(e => e.UserType).HasConversion<int>();
                entity.Property(e => e.UserStatus).HasConversion<int>();
                
                // Configure relationships - disable circular references
                entity.HasOne(e => e.Abune)
                    .WithMany()
                    .HasForeignKey(e => e.AbuneId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
                
                // Create indexes
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.AbuneId);
                entity.HasIndex(e => e.UserType);
                entity.HasIndex(e => e.UserStatus);
                entity.HasIndex(e => e.IsApproved);
            });

            // Configure ChatMessage entity
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(450);
                entity.Property(e => e.SenderId).HasMaxLength(450).IsRequired();
                entity.Property(e => e.RecipientId).HasMaxLength(450);
                entity.Property(e => e.AbuneId).HasMaxLength(450).IsRequired();
                entity.Property(e => e.Content).HasMaxLength(4000);
                entity.Property(e => e.FileUrl).HasMaxLength(500);
                entity.Property(e => e.FileName).HasMaxLength(255);
                entity.Property(e => e.FileType).HasMaxLength(100);
                entity.Property(e => e.ReplyToMessageId).HasMaxLength(450);
                entity.Property(e => e.ForwardedFromMessageId).HasMaxLength(450);
                entity.Property(e => e.DeletedBy).HasMaxLength(450);
                
                // Configure enums
                entity.Property(e => e.MessageType).HasConversion<int>();
                entity.Property(e => e.Status).HasConversion<int>();
                
                // Configure complex properties as JSON
                entity.Property(e => e.Reactions)
                    .HasColumnType("jsonb");
                
                entity.Property(e => e.ReadStatus)
                    .HasColumnType("jsonb");
                
                // Configure relationships
                entity.HasOne(e => e.Sender)
                    .WithMany()
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.Recipient)
                    .WithMany()
                    .HasForeignKey(m => m.RecipientId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.Abune)
                    .WithMany()
                    .HasForeignKey(m => m.AbuneId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.ReplyToMessage)
                    .WithMany()
                    .HasForeignKey(m => m.ReplyToMessageId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.ForwardedFromMessage)
                    .WithMany()
                    .HasForeignKey(m => m.ForwardedFromMessageId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Create indexes
                entity.HasIndex(e => e.SenderId);
                entity.HasIndex(e => e.RecipientId);
                entity.HasIndex(e => e.AbuneId);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.IsBroadcast);
                entity.HasIndex(e => e.IsDeleted);
                entity.HasIndex(e => e.MessageType);
                entity.HasIndex(e => e.Status);
            });

            // Configure ChatConversation entity
            modelBuilder.Entity<ChatConversation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(450);
                entity.Property(e => e.AbuneId).HasMaxLength(450).IsRequired();
                entity.Property(e => e.UserId).HasMaxLength(450).IsRequired();
                entity.Property(e => e.LastMessageContent).HasMaxLength(4000);
                
                // Configure enums
                entity.Property(e => e.LastMessageType).HasConversion<int>();
                
                // Configure relationships
                entity.HasOne(e => e.Abune)
                    .WithMany()
                    .HasForeignKey(c => c.AbuneId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Create indexes
                entity.HasIndex(e => e.AbuneId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.LastMessageAt);
                entity.HasIndex(e => e.IsActive);
            });

            // Configure Folder entity
            modelBuilder.Entity<Folder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(450);
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.ParentId).HasMaxLength(450);
                entity.Property(e => e.CreatedBy).HasMaxLength(450).IsRequired();
                entity.Property(e => e.AbuneId).HasMaxLength(450).IsRequired();
                entity.Property(e => e.Color).HasMaxLength(7); // Hex color code
                entity.Property(e => e.Icon).HasMaxLength(100);
                
                // Configure relationships
                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
                
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.Abune)
                    .WithMany()
                    .HasForeignKey(e => e.AbuneId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Create indexes
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.ParentId);
                entity.HasIndex(e => e.CreatedBy);
                entity.HasIndex(e => e.AbuneId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.LastModified);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.SortOrder);
                
                // Create composite index for unique folder names within same parent
                entity.HasIndex(e => new { e.Name, e.ParentId, e.AbuneId })
                    .IsUnique()
                    .HasFilter("IsActive = true");
            });

            // Configure MediaFile entity
            modelBuilder.Entity<MediaFile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(450);
                entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.ObjectName).HasMaxLength(500).IsRequired();
                entity.Property(e => e.FileUrl).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
                entity.Property(e => e.FolderId).HasMaxLength(450).IsRequired();
                entity.Property(e => e.UploadedBy).HasMaxLength(450).IsRequired();
                entity.Property(e => e.AbuneId).HasMaxLength(450).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.StorageType).HasMaxLength(50).IsRequired();
                
                // Configure enums
                entity.Property(e => e.MediaType).HasConversion<int>();
                
                // Configure relationships
                entity.HasOne(e => e.Folder)
                    .WithMany()
                    .HasForeignKey(e => e.FolderId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.UploadedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UploadedBy)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.Abune)
                    .WithMany()
                    .HasForeignKey(e => e.AbuneId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Create indexes
                entity.HasIndex(e => e.FileName);
                entity.HasIndex(e => e.ObjectName);
                entity.HasIndex(e => e.FolderId);
                entity.HasIndex(e => e.UploadedBy);
                entity.HasIndex(e => e.AbuneId);
                entity.HasIndex(e => e.UploadedAt);
                entity.HasIndex(e => e.LastModified);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.MediaType);
                entity.HasIndex(e => e.StorageType);
            });

            // Configure FileUpload entity
            // modelBuilder.Entity<FileUpload>(entity =>
            // {
            //     entity.HasKey(e => e.Id);
            //     entity.Property(e => e.Id).HasMaxLength(450);
            //     entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            //     entity.Property(e => e.OriginalFileName).HasMaxLength(255).IsRequired();
            //     entity.Property(e => e.FileUrl).HasMaxLength(1000).IsRequired();
            //     entity.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
            //     entity.Property(e => e.FolderId).HasMaxLength(450).IsRequired();
            //     entity.Property(e => e.UploadedBy).HasMaxLength(450).IsRequired();
            //     entity.Property(e => e.AbuneId).HasMaxLength(450).IsRequired();
            //     entity.Property(e => e.ThumbnailUrl).HasMaxLength(1000);
            //     entity.Property(e => e.Description).HasMaxLength(1000);
            //     entity.Property(e => e.UploadSessionId).HasMaxLength(450);
            //     
            //     // Configure enums
            //     entity.Property(e => e.MediaType).HasConversion<int>();
            //     entity.Property(e => e.Status).HasConversion<int>();
            //     
            //     // Configure relationships
            //     entity.HasOne(e => e.Folder)
            //         .WithMany()
            //         .HasForeignKey(e => e.FolderId)
            //         .OnDelete(DeleteBehavior.Restrict);
            //     
            //     entity.HasOne(e => e.UploadedByUser)
            //         .WithMany()
            //         .HasForeignKey(e => e.UploadedBy)
            //         .OnDelete(DeleteBehavior.Restrict);
            //     
            //     entity.HasOne(e => e.Abune)
            //         .WithMany()
            //         .HasForeignKey(e => e.AbuneId)
            //         .OnDelete(DeleteBehavior.Restrict);
            //     
            //     // Create indexes
            //     entity.HasIndex(e => e.FolderId);
            //     entity.HasIndex(e => e.UploadedBy);
            //     entity.HasIndex(e => e.AbuneId);
            //     entity.HasIndex(e => e.UploadedAt);
            //     entity.HasIndex(e => e.IsActive);
            //     entity.HasIndex(e => e.MediaType);
            //     entity.HasIndex(e => e.Status);
            //     entity.HasIndex(e => e.UploadSessionId);
            // });
            //
            // // Configure UploadSession entity
            // modelBuilder.Entity<UploadSession>(entity =>
            // {
            //     entity.HasKey(e => e.Id);
            //     entity.Property(e => e.Id).HasMaxLength(450);
            //     entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            //     entity.Property(e => e.OriginalFileName).HasMaxLength(255).IsRequired();
            //     entity.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
            //     entity.Property(e => e.FolderId).HasMaxLength(450).IsRequired();
            //     entity.Property(e => e.UploadedBy).HasMaxLength(450).IsRequired();
            //     entity.Property(e => e.AbuneId).HasMaxLength(450).IsRequired();
            //     entity.Property(e => e.MinioUploadId).HasMaxLength(500);
            //     entity.Property(e => e.Description).HasMaxLength(1000);
            //     
            //     // Configure enums
            //     entity.Property(e => e.Status).HasConversion<int>();
            //     entity.Property(e => e.MediaType).HasConversion<int>();
            //     
            //     // Configure complex properties as JSON
            //     entity.Property(e => e.CompletedChunkETags)
            //         .HasColumnType("jsonb");
            //     
            //     // Configure relationships
            //     entity.HasOne(e => e.Folder)
            //         .WithMany()
            //         .HasForeignKey(e => e.FolderId)
            //         .OnDelete(DeleteBehavior.Restrict);
            //     
            //     entity.HasOne(e => e.UploadedByUser)
            //         .WithMany()
            //         .HasForeignKey(e => e.UploadedBy)
            //         .OnDelete(DeleteBehavior.Restrict);
            //     
            //     entity.HasOne(e => e.Abune)
            //         .WithMany()
            //         .HasForeignKey(e => e.AbuneId)
            //         .OnDelete(DeleteBehavior.Restrict);
            //     
            //     // Create indexes
            //     entity.HasIndex(e => e.FolderId);
            //     entity.HasIndex(e => e.UploadedBy);
            //     entity.HasIndex(e => e.AbuneId);
            //     entity.HasIndex(e => e.CreatedAt);
            //     entity.HasIndex(e => e.LastActivity);
            //     entity.HasIndex(e => e.Status);
            //     entity.HasIndex(e => e.MediaType);
            // });
        }
    }
}
