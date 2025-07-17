using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    public class CreditHistoryRepository : Repository<CreditHistory>, ICreditHistoryRepository
    {
        public CreditHistoryRepository(CampusTradeDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<CreditHistory>> GetByUserIdAsync(int userId)
        {
            return await _context.CreditHistories
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<(IEnumerable<CreditHistory> Records, int TotalCount)> GetPagedByUserIdAsync(
            int userId, int pageIndex = 0, int pageSize = 10)
        {
            var query = _context.CreditHistories
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt);

            var totalCount = await query.CountAsync();
            var records = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (records, totalCount);
        }

        public async Task<IEnumerable<CreditHistory>> GetByChangeTypeAsync(string changeType)
        {
            return await _context.CreditHistories
                .Where(c => c.ChangeType == changeType)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<CreditHistory>> GetRecentChangesAsync(int days = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _context.CreditHistories
                .Where(c => c.CreatedAt >= cutoffDate)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalCreditChangeAsync(int userId, string? changeType = null)
        {
            var query = _context.CreditHistories.Where(c => c.UserId == userId);
            
            if (!string.IsNullOrEmpty(changeType))
            {
                query = query.Where(c => c.ChangeType == changeType);
            }

            return await query.SumAsync(c => c.NewScore);
        }

        public async Task<Dictionary<string, int>> GetChangeTypeStatisticsAsync()
        {
            return await _context.CreditHistories
                .GroupBy(c => c.ChangeType)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<IEnumerable<dynamic>> GetCreditTrendsAsync(int userId, int days = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _context.CreditHistories
                .Where(c => c.UserId == userId && c.CreatedAt >= cutoffDate)
                .GroupBy(c => c.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count(),
                    AverageScore = g.Average(c => c.NewScore)
                })
                .OrderBy(x => x.Date)
                .ToListAsync<dynamic>();
        }
    }
} 