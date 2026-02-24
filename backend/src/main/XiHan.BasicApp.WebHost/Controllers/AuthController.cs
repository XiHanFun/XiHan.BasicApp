#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthController
// Guid:bfc2ad43-0451-4b4c-8043-3ac6c8851df2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Authentication.Jwt;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Security.Users;
using System.Security.Claims;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories;
using XiHan.BasicApp.Rbac.ApplicationServices.Users;
using XiHan.BasicApp.Rbac.ApplicationServices.Users.Dtos;
using XiHan.BasicApp.Rbac.DomainServices;

namespace XiHan.BasicApp.WebHost.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserApplicationService _userApplicationService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISysUserRoleRepository _userRoleRepository;
    private readonly ISysRolePermissionRepository _rolePermissionRepository;
    private readonly ISysLoginLogRepository _loginLogRepository;
    private readonly IUserAuthenticationService _authenticationService;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthController(
        UserApplicationService userApplicationService,
        IJwtTokenService jwtTokenService,
        ISysUserRoleRepository userRoleRepository,
        ISysRolePermissionRepository rolePermissionRepository,
        ISysLoginLogRepository loginLogRepository,
        IUserAuthenticationService authenticationService,
        ICurrentUser currentUser)
    {
        _userApplicationService = userApplicationService;
        _jwtTokenService = jwtTokenService;
        _userRoleRepository = userRoleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _loginLogRepository = loginLogRepository;
        _authenticationService = authenticationService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="input">登录信息</param>
    /// <returns></returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<UserLoginResponseDto>> LoginAsync([FromBody] UserLoginDto input)
    {
        var loginResult = LoginResult.Failed;
        string? failureReason = null;
        long? userId = null;

        try
        {
            // 先根据用户名获取用户，用于检查锁定状态
            var userDto = await _userApplicationService.GetByUserNameAsync(input.UserName, input.TenantId);
            if (userDto != null)
            {
                userId = userDto.BasicId;

                // 检查用户是否被锁定（需要先获取完整用户实体）
                var userForLockCheck = await _authenticationService.AuthenticateAsync(input.UserName, input.Password, input.TenantId);
                if (userForLockCheck != null && _authenticationService.IsUserLocked(userForLockCheck))
                {
                    loginResult = LoginResult.AccountLocked;
                    failureReason = "账户已被锁定，请稍后再试";
                    return Unauthorized(new { message = failureReason });
                }
            }

            // 验证用户凭证
            userDto = await _userApplicationService.LoginAsync(input);
            if (userDto == null)
            {
                loginResult = LoginResult.InvalidCredentials;
                failureReason = "用户名或密码错误";

                // 记录失败尝试并检查是否需要锁定
                if (userId.HasValue)
                {
                    var isLocked = await _authenticationService.RecordFailedLoginAttemptAsync(userId.Value);
                    if (isLocked)
                    {
                        loginResult = LoginResult.AccountLocked;
                        failureReason = "登录失败次数过多，账户已被锁定30分钟";
                    }
                }

                return Unauthorized(new { message = failureReason });
            }

            userId = userDto.BasicId;

            // 检查用户状态
            if (userDto.Status != YesOrNo.Yes)
            {
                loginResult = LoginResult.AccountDisabled;
                failureReason = "账户已被禁用";
                return Unauthorized(new { message = failureReason });
            }

            // 登录成功，重置失败次数
            await _authenticationService.ResetFailedLoginAttemptsAsync(userId.Value);

            // 获取用户角色
            var userRoles = await _userRoleRepository.GetByUserIdAsync(userDto.BasicId);
            var roleNames = userRoles
                .Where(ur => ur.Role != null)
                .Select(ur => ur.Role!.RoleName)
                .Distinct()
                .ToList();

            // 获取用户权限
            var permissions = new List<string>();
            foreach (var userRole in userRoles)
            {
                var rolePermissions = await _rolePermissionRepository.GetByRoleIdAsync(userRole.RoleId);
                var codes = rolePermissions
                    .Where(rp => rp.Permission != null)
                    .Select(rp => rp.Permission!.PermissionCode);
                permissions.AddRange(codes);
            }
            permissions = permissions.Distinct().ToList();

            // 创建用户声明
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userDto.BasicId.ToString()),
                new(ClaimTypes.Name, userDto.UserName),
                new("UserId", userDto.BasicId.ToString()),
                new("UserName", userDto.UserName)
            };

            if (userDto.TenantId.HasValue)
            {
                claims.Add(new Claim(XiHanClaimTypes.TenantId, userDto.TenantId.Value.ToString()));
                claims.Add(new Claim("TenantId", userDto.TenantId.Value.ToString()));
            }

            // 添加角色声明
            foreach (var roleName in roleNames)
            {
                claims.Add(new Claim(ClaimTypes.Role, roleName));
            }

            // 添加权限声明
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("Permission", permission));
            }

            // 生成JWT Token
            var tokenResult = _jwtTokenService.GenerateAccessToken(claims);

            // 记录登录成功
            loginResult = LoginResult.Success;

            // 构建响应（前端 defaultResponseInterceptor 期望 { code: 200, data: ... }）
            var response = new UserLoginResponseDto
            {
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                TokenType = tokenResult.TokenType,
                ExpiresIn = tokenResult.ExpiresIn,
                IssuedAt = tokenResult.IssuedAt,
                ExpiresAt = tokenResult.ExpiresAt,
                User = userDto,
                Roles = roleNames,
                Permissions = permissions
            };

            return Ok(new { code = 200, data = response });
        }
        catch (Exception ex)
        {
            loginResult = LoginResult.Failed;
            failureReason = ex.Message;
            throw;
        }
        finally
        {
            // 记录登录日志
            await RecordLoginLogAsync(input.UserName, userId, loginResult, failureReason);
        }
    }

    /// <summary>
    /// 刷新Token
    /// </summary>
    /// <param name="input">刷新Token请求</param>
    /// <returns></returns>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<RefreshTokenResponseDto>> RefreshTokenAsync([FromBody] RefreshTokenDto input)
    {
        try
        {
            // 验证刷新Token（刷新令牌为 JWT 时可用；若框架使用随机字符串则需改用 RefreshAccessToken）
            var principal = _jwtTokenService.ValidateToken(input.RefreshToken);
            if (principal == null)
            {
                return Unauthorized(new { message = "无效的刷新令牌" });
            }

            // 检查是否过期
            if (_jwtTokenService.IsTokenExpired(input.RefreshToken))
            {
                return Unauthorized(new { message = "刷新令牌已过期" });
            }

            // 提取用户信息
            var claims = principal.Claims.ToList();

            // 生成新的Token
            var tokenResult = _jwtTokenService.GenerateAccessToken(claims);

            var response = new RefreshTokenResponseDto
            {
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                TokenType = tokenResult.TokenType,
                ExpiresIn = tokenResult.ExpiresIn,
                IssuedAt = tokenResult.IssuedAt,
                ExpiresAt = tokenResult.ExpiresAt
            };

            return Ok(new { code = 200, data = response });
        }
        catch (Exception)
        {
            return Unauthorized(new { message = "令牌刷新失败" });
        }
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    /// <returns></returns>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> LogoutAsync()
    {
        try
        {
            // TODO: 可以在这里实现Token黑名单机制
            // 当前简单返回成功，客户端删除Token即可

            var userName = _currentUser.UserName;
            var userId = _currentUser.UserId;

            // 可以记录登出日志
            await RecordLoginLogAsync(userName ?? "Unknown", userId, LoginResult.Success, "用户登出");

            return Ok(new { message = "登出成功" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"登出失败: {ex.Message}" });
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    /// <returns></returns>
    [HttpGet("current-user")]
    [Authorize]
    public async Task<ActionResult<SysUserDto>> GetCurrentUserAsync()
    {
        try
        {
            var userId = _currentUser.UserId;
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "未登录" });
            }

            var user = await _userApplicationService.GetByIdAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new { message = "用户不存在" });
            }

            return Ok(new { code = 200, data = user });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"获取用户信息失败: {ex.Message}" });
        }
    }

    /// <summary>
    /// 记录登录日志
    /// </summary>
    private async Task RecordLoginLogAsync(string userName, long? userId, LoginResult result, string? failureReason)
    {
        try
        {
            var loginLog = new SysLoginLog
            {
                UserId = userId ?? 0,
                UserName = userName,
                LoginTime = DateTimeOffset.Now,
                LoginResult = result,
                Message = failureReason,
                LoginIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                //Agent = HttpContext.Request.Headers["User-Agent"].ToString(),
                CreatedTime = DateTimeOffset.Now
            };

            await _loginLogRepository.SaveAsync(loginLog);
        }
        catch
        {
            // 记录日志失败不影响登录流程
        }
    }
}
