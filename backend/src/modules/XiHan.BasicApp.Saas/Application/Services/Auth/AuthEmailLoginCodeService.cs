#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthEmailLoginCodeService
// Guid:9c4e7b21-3a6d-4f81-b5c0-2d8f1a6e4b73
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 邮箱验证码登录的验证码下发与校验服务实现
/// </summary>
/// <remarks>
/// 登录前用户处于匿名态，无法通过站内信接收验证码，故采用进程内暂存。
/// 单实例（Singleton）注册，验证码键为「租户 + 邮箱」，一次性消费，过期自动失效。
/// 当前未接入真实邮件通道，验证码经接口 DebugCode 回显以便联调；生产应替换为真实邮件发送并停用回显。
/// </remarks>
public sealed class AuthEmailLoginCodeService : IAuthEmailLoginCodeService
{
    private readonly ConcurrentDictionary<string, CodeState> _codes = new();

    /// <inheritdoc />
    public int ExpiresInSeconds => 600;

    /// <inheritdoc />
    public string IssueCode(long? tenantId, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        _codes[BuildKey(tenantId, email)] = new CodeState(code, DateTimeOffset.UtcNow.AddSeconds(ExpiresInSeconds));
        return code;
    }

    /// <inheritdoc />
    public bool TryConsume(long? tenantId, string email, string? code)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var key = BuildKey(tenantId, email);
        if (!_codes.TryRemove(key, out var state))
        {
            return false;
        }

        return state.ExpiresAt > DateTimeOffset.UtcNow
               && string.Equals(state.Code, code.Trim(), StringComparison.Ordinal);
    }

    private static string BuildKey(long? tenantId, string email)
    {
        return $"{tenantId?.ToString() ?? "host"}:{email.Trim().ToLowerInvariant()}";
    }

    private sealed record CodeState(string Code, DateTimeOffset ExpiresAt);
}
