using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    [Table("STUDENTS")]
    public class Student
    {
        [Key]
        [Column("STUDENT_ID")]
        [StringLength(20)]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        [Column("NAME")]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Column("DEPARTMENT")]
        [StringLength(50)]
        public string? Department { get; set; }

        // 导航属性
        public virtual User? User { get; set; }
    }
} 