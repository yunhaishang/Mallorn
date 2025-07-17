using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(CampusTradeDbContext context) : base(context)
        {
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(int userId)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(int userId)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && 
                            rt.IsRevoked == 0 && 
                            rt.ExpiryDate > DateTime.UtcNow)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> RevokeTokenAsync(string token, string? reason = null)
        {
            try
            {
                var refreshToken = await GetByTokenAsync(token);
                if (refreshToken == null) return false;

                refreshToken.IsRevoked = 1;
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.RevokeReason = reason;

                Update(refreshToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RevokeAllUserTokensAsync(int userId, string? reason = null)
        {
            try
            {
                var tokens = await GetActiveTokensByUserAsync(userId);
                foreach (var token in tokens)
                {
                    token.IsRevoked = 1;
                    token.RevokedAt = DateTime.UtcNow;
                    token.RevokeReason = reason;
                    Update(token);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> CleanupExpiredTokensAsync()
        {
            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.ExpiryDate < DateTime.UtcNow)
                .ToListAsync();

            _context.RefreshTokens.RemoveRange(expiredTokens);
            return expiredTokens.Count;
        }

        public async Task<IEnumerable<RefreshToken>> GetSuspiciousTokensAsync()
        {
            // 查找可疑的令牌，比如同一用户有过多活跃令牌
            var suspiciousTokens = await _context.RefreshTokens
                .Where(rt => rt.IsRevoked == 0 && rt.ExpiryDate > DateTime.UtcNow)
                .GroupBy(rt => rt.UserId)
                .Where(g => g.Count() > 5) // 超过5个活跃令牌认为可疑
                .SelectMany(g => g)
                .ToListAsync();

            return suspiciousTokens;
        }

        public async Task<bool> IsTokenValidAsync(string token)
        {
            var refreshToken = await GetByTokenAsync(token);
            return refreshToken != null && 
                   refreshToken.IsRevoked == 0 && 
                   refreshToken.ExpiryDate > DateTime.UtcNow;
        }

        public async Task<IEnumerable<RefreshToken>> GetTokensByDeviceAsync(string deviceId)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.DeviceId == deviceId)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();
        }
    }
} 