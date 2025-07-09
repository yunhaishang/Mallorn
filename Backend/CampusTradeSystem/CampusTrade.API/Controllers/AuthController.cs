using Microsoft.AspNetCore.Mvc;
using CampusTrade.API.Models;
using CampusTrade.API.Services;

namespace CampusTrade.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _authService.LoginAsync(loginDto);
                
                if (result == null)
                {
                    return Unauthorized(new { message = "用户名或密码错误" });
                }

                return Ok(new
                {
                    success = true,
                    message = "登录成功",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "登录时发生错误", error = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var user = await _authService.RegisterAsync(registerDto);

                return Ok(new
                {
                    success = true,
                    message = "注册成功",
                    data = new
                    {
                        userId = user.UserId,
                        username = user.Username,
                        email = user.Email,
                        fullName = user.FullName,
                        studentId = user.StudentId,
                        creditScore = user.CreditScore
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "注册时发生错误", error = ex.Message });
            }
        }

        [HttpPost("validate-student")]
        public async Task<IActionResult> ValidateStudent([FromBody] StudentValidationDto validationDto)
        {
            try
            {
                var isValid = await _authService.ValidateStudentAsync(validationDto.StudentId, validationDto.Name);
                
                return Ok(new
                {
                    success = true,
                    isValid = isValid,
                    message = isValid ? "学生身份验证成功" : "学生身份验证失败"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "验证时发生错误", error = ex.Message });
            }
        }

        [HttpGet("user/{username}")]
        public async Task<IActionResult> GetUser(string username)
        {
            try
            {
                var user = await _authService.GetUserByUsernameAsync(username);
                
                if (user == null)
                {
                    return NotFound(new { message = "用户不存在" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        userId = user.UserId,
                        username = user.Username,
                        email = user.Email,
                        fullName = user.FullName,
                        phone = user.Phone,
                        studentId = user.StudentId,
                        creditScore = user.CreditScore,
                        createdAt = user.CreatedAt,
                        student = new
                        {
                            studentId = user.Student?.StudentId,
                            name = user.Student?.Name,
                            department = user.Student?.Department
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "查询用户时发生错误", error = ex.Message });
            }
        }
    }

    // 学生身份验证DTO
    public class StudentValidationDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
} 