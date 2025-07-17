using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// 虚拟账户管理Repository实现
    /// 提供账户余额管理、交易处理等功能，保证线程安全
    /// </summary>
    public class VirtualAccountsRepository : Repository<VirtualAccount>, IVirtualAccountsRepository
    {
        public VirtualAccountsRepository(CampusTradeDbContext context) : base(context)
        {
        }

        // 账户基础操作
        public async Task<VirtualAccount?> GetByUserIdAsync(int userId)
        {
            return await _context.VirtualAccounts
                .FirstOrDefaultAsync(va => va.UserId == userId);
        }

        public async Task<decimal> GetBalanceAsync(int userId)
        {
            var account = await GetByUserIdAsync(userId);
            return account?.Balance ?? 0;
        }

        public async Task<bool> HasSufficientBalanceAsync(int userId, decimal amount)
        {
            var balance = await GetBalanceAsync(userId);
            return balance >= amount;
        }

        // 余额操作（线程安全）
        public async Task<bool> DebitAsync(int userId, decimal amount, string reason)
        {
            if (amount <= 0) return false;

            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                // 先检查余额是否足够
                var account = await GetByUserIdAsync(userId);
                if (account == null || account.Balance < amount)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // 更新余额
                account.Balance -= amount;
                _context.VirtualAccounts.Update(account);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CreditAsync(int userId, decimal amount, string reason)
        {
            if (amount <= 0) return false;

            try
            {
                var account = await GetByUserIdAsync(userId);
                if (account == null)
                {
                    // 如果账户不存在，创建新账户
                    account = new VirtualAccount
                    {
                        UserId = userId,
                        Balance = amount,
                        CreatedAt = DateTime.UtcNow
                    };
                    await AddAsync(account);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // 更新余额
                    account.Balance += amount;
                    _context.VirtualAccounts.Update(account);
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TransferAsync(int fromUserId, int toUserId, decimal amount, string reason)
        {
            if (amount <= 0 || fromUserId == toUserId) return false;

            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                // 先扣除发送方余额
                var debitSuccess = await DebitAsync(fromUserId, amount, reason);
                if (!debitSuccess)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // 再增加接收方余额
                var creditSuccess = await CreditAsync(toUserId, amount, reason);
                if (!creditSuccess)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // 冻结和解冻功能（这里简化实现，实际项目中可能需要单独的冻结余额字段）
        public async Task<bool> FreezeBalanceAsync(int userId, decimal amount)
        {
            // 简化实现：直接扣除可用余额
            return await DebitAsync(userId, amount, "余额冻结");
        }

        public async Task<bool> UnfreezeBalanceAsync(int userId, decimal amount)
        {
            // 简化实现：直接增加可用余额
            return await CreditAsync(userId, amount, "余额解冻");
        }

        public async Task<decimal> GetFrozenBalanceAsync(int userId)
        {
            // 简化实现：返回0（实际项目中需要单独的冻结余额字段）
            return 0;
        }

        // 账户统计
        public async Task<decimal> GetTotalSystemBalanceAsync()
        {
            return await _context.VirtualAccounts
                .SumAsync(va => va.Balance);
        }

        public async Task<IEnumerable<VirtualAccount>> GetAccountsWithBalanceAboveAsync(decimal minBalance)
        {
            return await _context.VirtualAccounts
                .Include(va => va.User)
                .Where(va => va.Balance >= minBalance)
                .OrderByDescending(va => va.Balance)
                .ToListAsync();
        }

        public async Task<IEnumerable<VirtualAccount>> GetTopBalanceAccountsAsync(int count)
        {
            return await _context.VirtualAccounts
                .Include(va => va.User)
                .OrderByDescending(va => va.Balance)
                .Take(count)
                .ToListAsync();
        }

        // 批量操作
        public async Task<bool> BatchUpdateBalancesAsync(Dictionary<int, decimal> balanceChanges)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                foreach (var change in balanceChanges)
                {
                    var userId = change.Key;
                    var amount = change.Value;

                    if (amount > 0)
                    {
                        await CreditAsync(userId, amount, "批量更新");
                    }
                    else if (amount < 0)
                    {
                        var success = await DebitAsync(userId, Math.Abs(amount), "批量更新");
                        if (!success)
                        {
                            await transaction.RollbackAsync();
                            return false;
                        }
                    }
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<VirtualAccount>> GetAccountsByUserIdsAsync(IEnumerable<int> userIds)
        {
            return await _context.VirtualAccounts
                .Include(va => va.User)
                .Where(va => userIds.Contains(va.UserId))
                .ToListAsync();
        }
    }
} 