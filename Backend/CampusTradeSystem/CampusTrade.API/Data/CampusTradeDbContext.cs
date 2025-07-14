using CampusTrade.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Data
{
    public class CampusTradeDbContext : DbContext
    {
        public CampusTradeDbContext(DbContextOptions<CampusTradeDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置学生表
            modelBuilder.Entity<Student>(entity =>
            {
                entity.ToTable("STUDENTS");
                entity.HasKey(e => e.StudentId);

                entity.Property(e => e.StudentId)
                    .HasColumnName("STUDENT_ID")
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Department)
                    .HasColumnName("DEPARTMENT")
                    .HasMaxLength(50);
            });

            // 配置用户表
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("USERS");
                entity.HasKey(e => e.UserId);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.StudentId).IsUnique();

                entity.Property(e => e.UserId)
                    .HasColumnName("USER_ID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Email)
                    .HasColumnName("EMAIL")
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.CreditScore)
                    .HasColumnName("CREDIT_SCORE")
                    .HasPrecision(3, 1)
                    .HasDefaultValue(60.0m);

                entity.Property(e => e.PasswordHash)
                    .HasColumnName("PASSWORD_HASH")
                    .IsRequired()
                    .HasMaxLength(128);

                entity.Property(e => e.StudentId)
                    .HasColumnName("STUDENT_ID")
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Username)
                    .HasColumnName("USERNAME")
                    .HasMaxLength(50);

                entity.Property(e => e.FullName)
                    .HasColumnName("FULL_NAME")
                    .HasMaxLength(100);

                entity.Property(e => e.Phone)
                    .HasColumnName("PHONE")
                    .HasMaxLength(20);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("CREATED_AT")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("UPDATED_AT")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.IsActive)
                    .HasColumnName("IS_ACTIVE")
                    .HasDefaultValue(1);

                // JWT Token相关字段配置
                entity.Property(e => e.LastLoginAt)
                    .HasColumnName("LAST_LOGIN_AT");

                entity.Property(e => e.LastLoginIp)
                    .HasColumnName("LAST_LOGIN_IP")
                    .HasMaxLength(45);

                entity.Property(e => e.LoginCount)
                    .HasColumnName("LOGIN_COUNT")
                    .HasDefaultValue(0);

                entity.Property(e => e.IsLocked)
                    .HasColumnName("IS_LOCKED")
                    .HasDefaultValue(0);

                entity.Property(e => e.LockoutEnd)
                    .HasColumnName("LOCKOUT_END");

                entity.Property(e => e.FailedLoginAttempts)
                    .HasColumnName("FAILED_LOGIN_ATTEMPTS")
                    .HasDefaultValue(0);

                entity.Property(e => e.TwoFactorEnabled)
                    .HasColumnName("TWO_FACTOR_ENABLED")
                    .HasDefaultValue(0);

                entity.Property(e => e.PasswordChangedAt)
                    .HasColumnName("PASSWORD_CHANGED_AT");

                entity.Property(e => e.SecurityStamp)
                    .HasColumnName("SECURITY_STAMP")
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.EmailVerified)
                    .HasColumnName("EMAIL_VERIFIED")
                    .HasDefaultValue(0);

                entity.Property(e => e.EmailVerificationToken)
                    .HasColumnName("EMAIL_VERIFICATION_TOKEN")
                    .HasMaxLength(256);

                // 索引配置
                entity.HasIndex(e => e.LastLoginAt)
                    .HasDatabaseName("IX_USERS_LAST_LOGIN_AT");

                entity.HasIndex(e => e.IsLocked)
                    .HasDatabaseName("IX_USERS_IS_LOCKED");

                entity.HasIndex(e => e.SecurityStamp)
                    .HasDatabaseName("IX_USERS_SECURITY_STAMP");

                // 配置外键关系
                entity.HasOne(e => e.Student)
                    .WithOne(s => s.User)
                    .HasForeignKey<User>(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 配置与RefreshToken的一对多关系
                entity.HasMany(e => e.RefreshTokens)
                    .WithOne(r => r.User)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 配置RefreshToken表
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("REFRESH_TOKENS");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Token)
                    .HasColumnName("TOKEN")
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.UserId)
                    .HasColumnName("USER_ID")
                    .IsRequired();

                entity.Property(e => e.ExpiryDate)
                    .HasColumnName("EXPIRY_DATE")
                    .IsRequired();

                entity.Property(e => e.IsRevoked)
                    .HasColumnName("IS_REVOKED")
                    .HasDefaultValue(false);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("CREATED_AT")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.RevokedAt)
                    .HasColumnName("REVOKED_AT");

                entity.Property(e => e.IpAddress)
                    .HasColumnName("IP_ADDRESS")
                    .HasMaxLength(45);

                entity.Property(e => e.UserAgent)
                    .HasColumnName("USER_AGENT")
                    .HasMaxLength(500);

                entity.Property(e => e.DeviceId)
                    .HasColumnName("DEVICE_ID")
                    .HasMaxLength(100);

                entity.Property(e => e.ReplacedByToken)
                    .HasColumnName("REPLACED_BY_TOKEN")
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("CREATED_BY");

                entity.Property(e => e.LastUsedAt)
                    .HasColumnName("LAST_USED_AT");

                entity.Property(e => e.RevokedBy)
                    .HasColumnName("REVOKED_BY");

                entity.Property(e => e.RevokeReason)
                    .HasColumnName("REVOKE_REASON")
                    .HasMaxLength(200);

                // 索引配置
                entity.HasIndex(e => e.Token)
                    .IsUnique()
                    .HasDatabaseName("IX_REFRESH_TOKENS_TOKEN");

                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_REFRESH_TOKENS_USER_ID");

                entity.HasIndex(e => e.ExpiryDate)
                    .HasDatabaseName("IX_REFRESH_TOKENS_EXPIRY_DATE");

                entity.HasIndex(e => e.IsRevoked)
                    .HasDatabaseName("IX_REFRESH_TOKENS_IS_REVOKED");

                entity.HasIndex(e => e.DeviceId)
                    .HasDatabaseName("IX_REFRESH_TOKENS_DEVICE_ID");

                // 复合索引（用于查询优化）
                entity.HasIndex(e => new { e.UserId, e.IsRevoked, e.ExpiryDate })
                    .HasDatabaseName("IX_REFRESH_TOKENS_USER_STATUS_EXPIRY");

                entity.HasIndex(e => new { e.DeviceId, e.UserId, e.IsRevoked })
                    .HasDatabaseName("IX_REFRESH_TOKENS_DEVICE_USER_STATUS");

                // 外键关系已在User实体中配置
            });
        }
    }
}
