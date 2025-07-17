using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// 议价管理Repository实现
    /// 提供价格协商、状态跟踪等功能
    /// </summary>
    public class NegotiationsRepository : Repository<Negotiation>, INegotiationsRepository
    {
        public NegotiationsRepository(CampusTradeDbContext context) : base(context)
        {
        }

        // 议价查询
        public async Task<IEnumerable<Negotiation>> GetByOrderIdAsync(int orderId)
        {
            return await _context.Negotiations
                .Include(n => n.Order)
                .Where(n => n.OrderId == orderId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<Negotiation?> GetLatestNegotiationAsync(int orderId)
        {
            return await _context.Negotiations
                .Include(n => n.Order)
                .Where(n => n.OrderId == orderId)
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Negotiation>> GetPendingNegotiationsAsync(int userId)
        {
            return await _context.Negotiations
                .Include(n => n.Order)
                .ThenInclude(o => o.Product)
                .Where(n => n.Status == "等待回应" &&
                           (n.Order.BuyerId == userId || n.Order.SellerId == userId))
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        // 状态管理
        public async Task<bool> UpdateNegotiationStatusAsync(int negotiationId, string status)
        {
            try
            {
                var negotiation = await GetByPrimaryKeyAsync(negotiationId);
                if (negotiation == null) return false;

                negotiation.Status = status;
                Update(negotiation);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<Negotiation>> GetNegotiationsByStatusAsync(string status)
        {
            return await _context.Negotiations
                .Include(n => n.Order)
                .ThenInclude(o => o.Product)
                .Where(n => n.Status == status)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> AcceptNegotiationAsync(int negotiationId)
        {
            return await UpdateNegotiationStatusAsync(negotiationId, "接受");
        }

        public async Task<bool> RejectNegotiationAsync(int negotiationId)
        {
            return await UpdateNegotiationStatusAsync(negotiationId, "拒绝");
        }

        // 议价历史
        public async Task<IEnumerable<Negotiation>> GetNegotiationHistoryAsync(int orderId)
        {
            return await GetByOrderIdAsync(orderId);
        }

        public async Task<int> GetNegotiationCountByOrderAsync(int orderId)
        {
            return await _context.Negotiations
                .CountAsync(n => n.OrderId == orderId);
        }

        public async Task<bool> HasActiveNegotiationAsync(int orderId)
        {
            return await _context.Negotiations
                .AnyAsync(n => n.OrderId == orderId &&
                              (n.Status == "等待回应" || n.Status == "反报价"));
        }

        // 统计分析
        public async Task<decimal> GetAverageNegotiationRateAsync()
        {
            var negotiations = await _context.Negotiations
                .Include(n => n.Order)
                .ThenInclude(o => o.Product)
                .Where(n => n.Order.Product != null && n.Order.Product.BasePrice > 0)
                .Select(n => new
                {
                    ProposedPrice = n.ProposedPrice,
                    BasePrice = n.Order.Product.BasePrice
                })
                .ToListAsync();

            if (!negotiations.Any()) return 0;

            var rates = negotiations
                .Select(n => n.ProposedPrice / n.BasePrice)
                .ToList();

            return rates.Average();
        }

        public async Task<int> GetSuccessfulNegotiationCountAsync()
        {
            return await _context.Negotiations
                .CountAsync(n => n.Status == "接受");
        }

        public async Task<IEnumerable<dynamic>> GetNegotiationStatisticsAsync()
        {
            return await _context.Negotiations
                .GroupBy(n => n.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count(),
                    AveragePrice = g.Average(n => n.ProposedPrice)
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync<dynamic>();
        }

        public async Task<IEnumerable<Negotiation>> GetRecentNegotiationsAsync(int days = 7)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _context.Negotiations
                .Include(n => n.Order)
                .ThenInclude(o => o.Product)
                .Where(n => n.CreatedAt >= cutoffDate)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
    }
}