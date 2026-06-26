#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ExternalLoginDtos
// Guid:6f1b2c34-7d8e-4a91-b0c2-3e4f5a6b7c8d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/16 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 第三方登录编排命令
/// </summary>
/// <remarks>
/// 由 OAuth 回调端点在校验 ExternalCookie 后组装并调用；非公开 API（ExternalLoginAsync 已禁用动态暴露）。
/// </remarks>
/// <param name="Provider">提供商标识（同 OAuth scheme，如 github/google/qq）</param>
/// <param name="ProviderKey">提供商用户唯一标识（OpenId/NameIdentifier）</param>
/// <param name="DisplayName">三方昵称</param>
/// <param name="Email">三方邮箱（仅未被占用时用于建号，避免按邮箱并入既有账号）</param>
/// <param name="AvatarUrl">三方头像</param>
/// <param name="IsBind">true=绑定到指定用户；false=登录（含首登自动建号）</param>
/// <param name="BindUserId">绑定意图时的目标用户ID（由一次性票据解析得到）</param>
public sealed record ExternalLoginCommand(
    string Provider,
    string ProviderKey,
    string? DisplayName,
    string? Email,
    string? AvatarUrl,
    bool IsBind,
    long? BindUserId);

/// <summary>
/// 第三方登录编排结果
/// </summary>
public sealed record ExternalLoginResultDto
{
    /// <summary>
    /// 是否成功（登录成功或绑定成功）
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 是否为绑定成功（区别于登录）
    /// </summary>
    public bool Bound { get; init; }

    /// <summary>
    /// 登录令牌（登录成功时返回）
    /// </summary>
    public LoginTokenDto? Token { get; init; }

    /// <summary>
    /// 失败码（invalid / conflict / disabled / user_not_found / unauthenticated 等）
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// 失败提示
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 构造登录成功结果
    /// </summary>
    public static ExternalLoginResultDto LoginSuccess(LoginTokenDto token)
    {
        return new ExternalLoginResultDto { Success = true, Token = token };
    }

    /// <summary>
    /// 构造绑定成功结果
    /// </summary>
    public static ExternalLoginResultDto BindSuccess()
    {
        return new ExternalLoginResultDto { Success = true, Bound = true };
    }

    /// <summary>
    /// 构造失败结果
    /// </summary>
    public static ExternalLoginResultDto Fail(string code, string message)
    {
        return new ExternalLoginResultDto { Success = false, ErrorCode = code, ErrorMessage = message };
    }
}

/// <summary>
/// 第三方绑定一次性票据的分布式缓存键约定。
/// </summary>
/// <remarks>
/// 单一事实来源：票据签发方（CreateOAuthBindTicketAsync）与读取方（OAuth 发起端点）必须使用同一键，
/// 避免两处各写字面量而漂移。
/// </remarks>
public static class OAuthBindTicket
{
    /// <summary>
    /// 票据缓存键
    /// </summary>
    /// <param name="ticket">票据令牌</param>
    public static string CacheKey(string ticket)
    {
        return $"oauth:bind-ticket:{ticket}";
    }
}
