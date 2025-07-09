using Microsoft.EntityFrameworkCore;
using CampusTrade.API.Models;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置学生表
            modelBuilder.Entity<Student>(entity =>
            {
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

                // 配置外键关系
                entity.HasOne(e => e.Student)
                    .WithOne(s => s.User)
                    .HasForeignKey<User>(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
} 