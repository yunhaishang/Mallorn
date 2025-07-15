using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    [Table("USERS")]
    public class User
    {
        [Key]
        [Column("USER_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        [Column("EMAIL")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Column("CREDIT_SCORE")]
        [Range(0, 100)]
        public decimal CreditScore { get; set; } = 60.0m;

        [Required]
        [Column("PASSWORD_HASH")]
        [StringLength(128)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [Column("STUDENT_ID")]
        [StringLength(20)]
        public string StudentId { get; set; } = string.Empty;

        [Column("USERNAME")]
        [StringLength(50)]
        public string? Username { get; set; }

        [Column("FULL_NAME")]
        [StringLength(100)]
        public string? FullName { get; set; }

        [Column("PHONE")]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; }

        [Column("UPDATED_AT")]
        public DateTime UpdatedAt { get; set; }

        [Column("IS_ACTIVE")]
        public int IsActive { get; set; } = 1;

        // JWT Token相关字段
        [Column("LAST_LOGIN_AT")]
        public DateTime? LastLoginAt { get; set; }

        [Column("LAST_LOGIN_IP")]
        [StringLength(45)] // IPv6最大长度
        public string? LastLoginIp { get; set; }

        [Column("LOGIN_COUNT")]
        public int LoginCount { get; set; } = 0;

        [Column("IS_LOCKED")]
        public int IsLocked { get; set; } = 0;

        [Column("LOCKOUT_END")]
        public DateTime? LockoutEnd { get; set; }

        [Column("FAILED_LOGIN_ATTEMPTS")]
        public int FailedLoginAttempts { get; set; } = 0;

        [Column("TWO_FACTOR_ENABLED")]
        public int TwoFactorEnabled { get; set; } = 0;

        [Column("PASSWORD_CHANGED_AT")]
        public DateTime? PasswordChangedAt { get; set; }

        [Column("SECURITY_STAMP")]
        [StringLength(256)]
        public string SecurityStamp { get; set; } = string.Empty;

        [Column("EMAIL_VERIFIED")]
        public int EmailVerified { get; set; } = 0;

        [Column("EMAIL_VERIFICATION_TOKEN")]
        [StringLength(256)]
        public string? EmailVerificationToken { get; set; }

        // 导航属性
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
} 