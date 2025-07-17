using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 刷新令牌管理Repository接口
    /// 提供JWT令牌管理等功能
    /// </summary>
    public interface IRefreshTokenRepository : IRepository<RefreshToken>
    {
        // 令牌查询
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task<IEnumerable<RefreshToken>> GetByUserIdAsync(int userId);
        Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(int userId);

        // 令牌管理
        Task<bool> RevokeTokenAsync(string token, string? reason = null);
        Task<bool> RevokeAllUserTokensAsync(int userId, string? reason = null);
        Task<int> CleanupExpiredTokensAsync();

        // 安全功能
        Task<IEnumerable<RefreshToken>> GetSuspiciousTokensAsync();
        Task<bool> IsTokenValidAsync(string token);
        Task<IEnumerable<RefreshToken>> GetTokensByDeviceAsync(string deviceId);
    }
} 