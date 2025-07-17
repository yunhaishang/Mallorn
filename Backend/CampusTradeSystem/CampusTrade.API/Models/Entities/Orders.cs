using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 订单实体类 - 对应 Oracle 数据库中的 ORDERS 表
    /// 表示普通的商品购买订单，继承自抽象订单
    /// </summary>
    [Table("ORDERS")]
    public class Order
    {
        /// <summary>
        /// 订单ID - 主键，对应Oracle中的order_id字段
        /// 同时也是AbstractOrder的外键，一对一关系
        /// </summary>
        [Key]
        [Column("ORDER_ID")]
        public int OrderId { get; set; }

        /// <summary>
        /// 买家用户ID - 外键，对应Oracle中的buyer_id字段
        /// </summary>
        [Required]
        [Column("BUYER_ID")]
        public int BuyerId { get; set; }

        /// <summary>
        /// 卖家用户ID - 外键，对应Oracle中的seller_id字段
        /// </summary>
        [Required]
        [Column("SELLER_ID")]
        public int SellerId { get; set; }

        /// <summary>
        /// 商品ID - 外键，对应Oracle中的product_id字段
        /// </summary>
        [Required]
        [Column("PRODUCT_ID")]
        public int ProductId { get; set; }

        /// <summary>
        /// 订单总金额 - 对应Oracle中的total_amount字段
        /// </summary>
        [Column("TOTAL_AMOUNT", TypeName = "NUMBER(10,2)")]
        [Range(0, 99999999.99, ErrorMessage = "订单金额必须在有效范围内")]
        public decimal? TotalAmount { get; set; }

        /// <summary>
        /// 订单状态 - 对应Oracle中的status字段
        /// 限制值：待付款、已付款、已发货、已送达、已完成、已取消，默认值"待付款"
        /// </summary>
        [Required]
        [Column("STATUS", TypeName = "VARCHAR2(20)")]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 订单创建时间 - 对应Oracle中的create_time字段
        /// 默认为当前时间戳
        /// </summary>
        [Column("CREATE_TIME")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 过期时间 - 对应Oracle中的expire_time字段
        /// </summary>
        [Column("EXPIRE_TIME")]
        public DateTime? ExpireTime { get; set; }

        /// <summary>
        /// 最终成交价格 - 对应Oracle中的final_price字段
        /// </summary>
        [Column("FINAL_PRICE", TypeName = "NUMBER(10,2)")]
        [Range(0, 99999999.99, ErrorMessage = "最终价格必须在有效范围内")]
        public decimal? FinalPrice { get; set; }

        /// <summary>
        /// 对应的抽象订单 - 一对一关系
        /// 通过OrderId外键关联AbstractOrders表
        /// </summary>
        [ForeignKey("OrderId")]
        public virtual AbstractOrder? AbstractOrder { get; set; }

        /// <summary>
        /// 买家信息 - 多对一关系
        /// 通过BuyerId外键关联Users表
        /// </summary>
        [ForeignKey("BuyerId")]
        public virtual User? Buyer { get; set; }

        /// <summary>
        /// 卖家信息 - 多对一关系
        /// 通过SellerId外键关联Users表
        /// </summary>
        [ForeignKey("SellerId")]
        public virtual User? Seller { get; set; }

        /// <summary>
        /// 订单商品 - 多对一关系
        /// 通过ProductId外键关联Products表
        /// </summary>
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        /// <summary>
        /// 该订单的议价记录集合 - 一对多关系
        /// 一个订单可以有多个议价记录
        /// </summary>
        public virtual ICollection<Negotiation> Negotiations { get; set; } = new List<Negotiation>();

        /// <summary>
        /// 订单状态的有效值 - 与Oracle检查约束保持一致
        /// </summary>
        public static class OrderStatus
        {
            /// <summary>
            /// 待付款 - 订单已创建，等待买家付款
            /// </summary>
            public const string PendingPayment = "待付款";

            /// <summary>
            /// 已付款 - 买家已付款，等待卖家发货
            /// </summary>
            public const string Paid = "已付款";

            /// <summary>
            /// 已发货 - 卖家已发货，等待买家确认收货
            /// </summary>
            public const string Shipped = "已发货";

            /// <summary>
            /// 已送达 - 商品已送达，等待买家确认
            /// </summary>
            public const string Delivered = "已送达";

            /// <summary>
            /// 已完成 - 交易完成
            /// </summary>
            public const string Completed = "已完成";

            /// <summary>
            /// 已取消 - 订单已取消
            /// </summary>
            public const string Cancelled = "已取消";
        }

        /// <summary>
        /// 检查订单状态是否有效
        /// </summary>
        /// <param name="status">状态值</param>
        /// <returns>是否有效</returns>
        public static bool IsValidStatus(string status)
        {
            return status == OrderStatus.PendingPayment ||
                   status == OrderStatus.Paid ||
                   status == OrderStatus.Shipped ||
                   status == OrderStatus.Delivered ||
                   status == OrderStatus.Completed ||
                   status == OrderStatus.Cancelled;
        }

        /// <summary>
        /// 验证当前实例的状态是否有效
        /// </summary>
        /// <returns>是否有效</returns>
        public bool IsValidStatus()
        {
            return IsValidStatus(Status);
        }

        /// <summary>
        /// 判断订单是否已过期
        /// </summary>
        /// <returns>是否过期</returns>
        public bool IsExpired()
        {
            return ExpireTime.HasValue && ExpireTime.Value < DateTime.Now;
        }

        /// <summary>
        /// 判断订单是否可以取消
        /// </summary>
        /// <returns>是否可以取消</returns>
        public bool CanCancel()
        {
            return Status == OrderStatus.PendingPayment || Status == OrderStatus.Paid;
        }

        /// <summary>
        /// 判断订单是否可以付款
        /// </summary>
        /// <returns>是否可以付款</returns>
        public bool CanPay()
        {
            return Status == OrderStatus.PendingPayment && !IsExpired();
        }

        /// <summary>
        /// 判断订单是否可以发货
        /// </summary>
        /// <returns>是否可以发货</returns>
        public bool CanShip()
        {
            return Status == OrderStatus.Paid;
        }

        /// <summary>
        /// 判断订单是否可以确认收货
        /// </summary>
        /// <returns>是否可以确认收货</returns>
        public bool CanConfirmDelivery()
        {
            return Status == OrderStatus.Shipped || Status == OrderStatus.Delivered;
        }

        /// <summary>
        /// 判断是否为最终状态（不可再变更）
        /// </summary>
        /// <returns>是否为最终状态</returns>
        public bool IsFinalStatus()
        {
            return Status == OrderStatus.Completed || Status == OrderStatus.Cancelled;
        }

        /// <summary>
        /// 更新订单状态
        /// </summary>
        /// <param name="newStatus">新状态</param>
        /// <exception cref="ArgumentException">状态无效时抛出</exception>
        public void UpdateStatus(string newStatus)
        {
            if (!IsValidStatus(newStatus))
                throw new ArgumentException($"无效的订单状态: {newStatus}");

            if (IsFinalStatus())
                throw new InvalidOperationException("订单已处于最终状态，无法修改");

            Status = newStatus;
        }

        /// <summary>
        /// 获取剩余付款时间
        /// </summary>
        /// <returns>剩余时间，如果已过期或无过期时间返回null</returns>
        public TimeSpan? GetRemainingTime()
        {
            if (!ExpireTime.HasValue || IsExpired())
                return null;

            return ExpireTime.Value - DateTime.Now;
        }

        /// <summary>
        /// 设置订单过期时间（从当前时间开始计算）
        /// </summary>
        /// <param name="minutes">过期分钟数</param>
        public void SetExpireTime(int minutes)
        {
            ExpireTime = DateTime.Now.AddMinutes(minutes);
        }

        /// <summary>
        /// 获取显示价格（优先显示最终价格，其次总金额）
        /// </summary>
        /// <returns>显示价格</returns>
        public decimal? GetDisplayPrice()
        {
            return FinalPrice ?? TotalAmount;
        }

        /// <summary>
        /// 判断是否有议价记录
        /// </summary>
        /// <returns>是否有议价</returns>
        public bool HasPriceNegotiation()
        {
            return Negotiations?.Any() == true;
        }

        /// <summary>
        /// 重写ToString方法
        /// </summary>
        /// <returns>订单信息</returns>
        public override string ToString()
        {
            return $"Order[{OrderId}] - {Status} - Amount: {GetDisplayPrice():C}";
        }
    }
}
