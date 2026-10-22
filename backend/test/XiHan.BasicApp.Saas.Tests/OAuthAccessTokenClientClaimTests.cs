// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using Moq;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Authentication.Jwt;
using XiHan.Framework.Security.Claims;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// OAuth 应用代用户签发的令牌带 client_id：条件里的客户端类型据此认出 OAuth，不再被当作网页登录
/// </summary>
public sealed class OAuthAccessTokenClientClaimTests
{
    /// <summary>
    /// 给了应用标识就写 client_id 声明
    /// </summary>
    [Fact]
    public void IssueAccessToken_WithClientId_WritesClientIdClaim()
    {
        var claims = CaptureIssuedClaims(Command(clientId: "app-1"));

        Assert.Equal("app-1", claims.Single(claim => claim.Type == XiHanClaimTypes.ClientId).Value);
    }

    /// <summary>
    /// 网页登录不带 client_id
    /// </summary>
    [Fact]
    public void IssueAccessToken_WithoutClientId_WritesNoClientIdClaim()
    {
        var claims = CaptureIssuedClaims(Command(clientId: null));

        Assert.DoesNotContain(claims, claim => claim.Type == XiHanClaimTypes.ClientId);
    }

    private static AuthAccessTokenIssueCommand Command(string? clientId)
    {
        var user = new SysUser { UserName = "alice" };
        SaasTestHelper.SetBasicId(user, 1001);
        return new AuthAccessTokenIssueCommand(user, 7, "sess-1", "jti-1", ["member"], [], DeviceId: null, ClientId: clientId);
    }

    private static List<Claim> CaptureIssuedClaims(AuthAccessTokenIssueCommand command)
    {
        List<Claim>? captured = null;
        var jwt = new Mock<IJwtTokenService>();
        jwt
            .Setup(service => service.GenerateAccessToken(It.IsAny<List<Claim>>()))
            .Returns((List<Claim> claims) =>
            {
                captured = claims;
                var issuedAt = new DateTime(2026, 10, 22, 0, 0, 0, DateTimeKind.Utc);
                return new JwtTokenResult
                {
                    AccessToken = "access",
                    RefreshToken = "refresh",
                    TokenType = "Bearer",
                    ExpiresIn = 3600,
                    IssuedAt = issuedAt,
                    ExpiresAt = issuedAt.AddHours(1),
                    RefreshTokenExpiresAt = issuedAt.AddDays(7)
                };
            });

        _ = new AuthTokenIssueService(jwt.Object).IssueAccessToken(command);

        Assert.NotNull(captured);
        return captured;
    }
}
