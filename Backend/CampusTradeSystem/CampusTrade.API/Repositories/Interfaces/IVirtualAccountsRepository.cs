using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 虚拟账户管理Repository接口
    /// 提供账户余额管理、交易处理等功能
    /// </summary>
    public interface IVirtualAccountsRepository : IRepository<VirtualAccount>
    {
        // 账户基础操作
        Task<VirtualAccount?> GetByUserIdAsync(int userId);
        Task<decimal> GetBalanceAsync(int userId);
        Task<bool> HasSufficientBalanceAsync(int userId, decimal amount);

        // 余额操作（线程安全）
        Task<bool> DebitAsync(int userId, decimal amount, string reason);
        Task<bool> CreditAsync(int userId, decimal amount, string reason);
        Task<bool> TransferAsync(int fromUserId, int toUserId, decimal amount, string reason);

        // 冻结和解冻
        Task<bool> FreezeBalanceAsync(int userId, decimal amount);
        Task<bool> UnfreezeBalanceAsync(int userId, decimal amount);
        Task<decimal> GetFrozenBalanceAsync(int userId);

        // 账户统计
        Task<decimal> GetTotalSystemBalanceAsync();
        Task<IEnumerable<VirtualAccount>> GetAccountsWithBalanceAboveAsync(decimal minBalance);
        Task<IEnumerable<VirtualAccount>> GetTopBalanceAccountsAsync(int count);

        // 批量操作
        Task<bool> BatchUpdateBalancesAsync(Dictionary<int, decimal> balanceChanges);
        Task<IEnumerable<VirtualAccount>> GetAccountsByUserIdsAsync(IEnumerable<int> userIds);
    }
} 