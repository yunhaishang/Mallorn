using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models
{
    [Table("USERS")]
    public class User
    {
        [Key]
        [Column("USER_ID")]
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
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("UPDATED_AT")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("IS_ACTIVE")]
        public int IsActive { get; set; } = 1;

        // 导航属性
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }
    }
} 