// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace XiHan.BasicApp.Integration.Tests.Infrastructure;

/// <summary>
/// 用测试密钥自签 HS256 令牌：<c>sub</c> 主体、<c>tenant</c> 当前租户、<c>role</c> 角色，与 <c>Security.Jwt</c> 的校验参数对齐。N1 的 <c>auth.login</c> 落地前，测试与前端联调都用它。
/// </summary>
public static class TestTokens
{
    public const string Secret = "integration-tests-only-jwt-secret-of-at-least-32-bytes";

    public const string Issuer = "xihan.basicapp.tests";

    public const string Audience = "xihan.basicapp";

    public static string Issue(string subject, string? tenant, params string[] roles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        var claims = new Dictionary<string, object>(StringComparer.Ordinal) { ["sub"] = subject, ["name"] = subject };
        if (tenant is not null)
        {
            claims["tenant"] = tenant;
        }

        if (roles.Length > 0)
        {
            claims["role"] = roles.Length == 1 ? roles[0] : roles;
        }

        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            IssuedAt = now,
            NotBefore = now.AddMinutes(-1),
            Expires = now.AddHours(1),
            Claims = claims,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)), SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
