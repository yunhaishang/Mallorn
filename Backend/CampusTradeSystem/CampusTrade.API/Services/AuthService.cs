using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CampusTrade.API.Data;
using CampusTrade.API.Models;

namespace CampusTrade.API.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
        Task<User?> RegisterAsync(RegisterDto registerDto);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<bool> ValidateStudentAsync(string studentId, string name);
    }

    public class AuthService : IAuthService
    {
        private readonly CampusTradeDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(CampusTradeDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
        {
            // 支持用户名或邮箱登录
            var user = await _context.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => 
                    (u.Username == loginDto.Username || u.Email == loginDto.Username) 
                    && u.IsActive == 1);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return null;
            }

            var token = GenerateJwtToken(user);
            
            return new LoginResponseDto
            {
                Token = token,
                Username = user.Username ?? string.Empty,
                Email = user.Email,
                FullName = user.FullName,
                CreditScore = user.CreditScore,
                StudentId = user.StudentId
            };
        }

        public async Task<User?> RegisterAsync(RegisterDto registerDto)
        {
            // 1. 验证学生身份
            var isValidStudent = await ValidateStudentAsync(registerDto.StudentId, registerDto.Name);
            if (!isValidStudent)
            {
                throw new ArgumentException("学生身份验证失败，请检查学号和姓名是否正确");
            }

            // 2. 检查学号是否已被注册
            var existingUserByStudent = await _context.Users
                .FirstOrDefaultAsync(u => u.StudentId == registerDto.StudentId);
            if (existingUserByStudent != null)
            {
                throw new ArgumentException("该学号已被注册");
            }

            // 3. 检查邮箱是否已存在
            var existingUserByEmail = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == registerDto.Email);
            if (existingUserByEmail != null)
            {
                throw new ArgumentException("该邮箱已被注册");
            }

            // 4. 检查用户名是否已存在（如果提供了用户名）
            if (!string.IsNullOrEmpty(registerDto.Username))
            {
                var existingUserByUsername = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == registerDto.Username);
                if (existingUserByUsername != null)
                {
                    throw new ArgumentException("该用户名已被使用");
                }
            }

            // 5. 创建新用户
            var user = new User
            {
                StudentId = registerDto.StudentId,
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                Username = registerDto.Username,
                FullName = registerDto.FullName ?? registerDto.Name,
                Phone = registerDto.Phone,
                CreditScore = 60.0m, // 新用户默认信用分
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = 1
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => 
                    (u.Username == username || u.Email == username) 
                    && u.IsActive == 1);
        }

        public async Task<bool> ValidateStudentAsync(string studentId, string name)
        {
            // 验证学生信息是否在预存的学生表中
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.Name == name);

            return student != null;
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "YourSecretKeyForCampusTradingPlatform2024!";
            var issuer = jwtSettings["Issuer"] ?? "CampusTrade.API";
            var audience = jwtSettings["Audience"] ?? "CampusTrade.Client";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("UserId", user.UserId.ToString()),
                new Claim("StudentId", user.StudentId),
                new Claim("CreditScore", user.CreditScore.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
} 