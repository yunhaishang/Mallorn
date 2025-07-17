using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 虚拟账户实体 - 对应 Oracle 数据库中的 VIRTUAL_ACCOUNTS 表
    /// 管理用户的虚拟余额
    /// </summary>
    [Table("VIRTUAL_ACCOUNTS")]
    public class VirtualAccount
    {
        /// <summary>
        /// 账户ID - 主键，对应Oracle中的account_id字段，由序列和触发器自增
        /// </summary>
        [Key]
        [Column("ACCOUNT_ID")]
        public int AccountId { get; set; }

        /// <summary>
        /// 用户ID - 外键，对应Oracle中的user_id字段
        /// </summary>
        [Required]
        [Column("USER_ID")]
        public int UserId { get; set; }

        /// <summary>
        /// 账户余额 - 对应Oracle中的balance字段，精度为10位数字2位小数，默认值0.00（由Oracle处理）
        /// </summary>
        [Required]
        [Column("BALANCE", TypeName = "NUMBER(10,2)")]
        [Range(0, 99999999.99, ErrorMessage = "余额不能为负数且不能超过99999999.99")]
        public decimal Balance { get; set; }

        /// <summary>
        /// 创建时间 - 对应Oracle中的created_at字段，默认为当前时间（由Oracle处理）
        /// </summary>
        [Required]
        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 关联的用户 - 一对一关系
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// 充值记录集合 - 一对多关系
        /// </summary>
        public virtual ICollection<RechargeRecord> RechargeRecords { get; set; } = new List<RechargeRecord>();

        /// <summary>
        /// 检查是否有足够余额
        /// </summary>
        /// <param name="amount">需要的金额</param>
        /// <returns>是否有足够余额</returns>
        public bool HasSufficientBalance(decimal amount)
        {
            if (amount < 0)
                throw new ArgumentException("金额不能为负数", nameof(amount));

            return Balance >= amount;
        }

        /// <summary>
        /// 增加余额（简单操作，复杂操作请使用Repository）
        /// </summary>
        /// <param name="amount">增加的金额</param>
        public void AddBalance(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("增加的金额必须大于0", nameof(amount));

            if (Balance + amount > MaxBalance)
                throw new InvalidOperationException($"余额不能超过{MaxBalance:C}");

            Balance += amount;
        }

        /// <summary>
        /// 扣减余额（简单操作，复杂操作请使用Repository）
        /// </summary>
        /// <param name="amount">扣减的金额</param>
        public void DeductBalance(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("扣减的金额必须大于0", nameof(amount));

            if (!HasSufficientBalance(amount))
                throw new InvalidOperationException("余额不足");

            Balance -= amount;
        }

        /// <summary>
        /// 获取格式化的余额字符串
        /// </summary>
        /// <returns>格式化的余额</returns>
        public string GetFormattedBalance()
        {
            return $"¥{Balance:F2}";
        }

        /// <summary>
        /// 是否为新账户（创建不超过24小时）
        /// </summary>
        /// <returns>是否为新账户</returns>
        public bool IsNewAccount()
        {
            return DateTime.Now - CreatedAt <= TimeSpan.FromDays(1);
        }

        /// <summary>
        /// 为用户创建新的虚拟账户
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>新的虚拟账户实例</returns>
        public static VirtualAccount CreateForUser(int userId)
        {
            if (userId <= 0)
                throw new ArgumentException("用户ID必须大于0", nameof(userId));

            return new VirtualAccount
            {
                UserId = userId,
                Balance = 0.00m,
                CreatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// 最大余额限制
        /// </summary>
        public const decimal MaxBalance = 99999999.99m;

        /// <summary>
        /// 最小充值金额
        /// </summary>
        public const decimal MinRechargeAmount = 0.01m;

        /// <summary>
        /// 最大充值金额
        /// </summary>
        public const decimal MaxRechargeAmount = 50000.00m;
    }
}
