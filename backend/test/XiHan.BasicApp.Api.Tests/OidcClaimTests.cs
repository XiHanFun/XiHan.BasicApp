// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Authentication.Oidc;

namespace XiHan.BasicApp.Api.Tests;

/// <summary>
/// OIDC 声明下发与 id_token 签发测试：未授予的 scope 一律不下发对应资料。
/// </summary>
public sealed class OidcClaimTests
{
    /// <summary>
    /// 只授予 openid 时，不下发任何资料声明。
    /// </summary>
    [Fact]
    public void BuildProfileClaims_ShouldEmitNothing_WhenOnlyOpenIdGranted()
    {
        var claims = OAuthServerService.BuildProfileClaims(BuildUser(), [OidcConstants.Scopes.OpenId]);

        Assert.Empty(claims);
    }

    /// <summary>
    /// profile 只带出用户名与显示名头像，不含邮箱手机号。
    /// </summary>
    [Fact]
    public void BuildProfileClaims_ShouldScopeEmailAndPhoneSeparately()
    {
        var profileOnly = OAuthServerService.BuildProfileClaims(BuildUser(), [OidcConstants.Scopes.Profile])
            .Select(claim => claim.Type)
            .ToList();

        Assert.Contains(OidcConstants.Claims.PreferredUserName, profileOnly);
        Assert.DoesNotContain(OidcConstants.Claims.Email, profileOnly);
        Assert.DoesNotContain(OidcConstants.Claims.PhoneNumber, profileOnly);

        var withEmail = OAuthServerService.BuildProfileClaims(BuildUser(), [OidcConstants.Scopes.Email])
            .Select(claim => claim.Type)
            .ToList();

        Assert.Contains(OidcConstants.Claims.Email, withEmail);
        Assert.DoesNotContain(OidcConstants.Claims.PreferredUserName, withEmail);
    }

    /// <summary>
    /// id_token 必带 iss / aud / sub，并在给了访问令牌时算出 at_hash、给了 nonce 时原样回填。
    /// </summary>
    [Fact]
    public void IdToken_ShouldCarryRequiredClaims()
    {
        var options = Options.Create(new OidcOptions
        {
            IsEnabled = true,
            Issuer = "https://sso.example.com",
            SigningKeyPath = Path.Combine(Path.GetTempPath(), $"oidc-test-{Guid.NewGuid():N}.pem"),
            AutoGenerateSigningKey = true
        });

        var service = new IdTokenService(new OidcSigningKeyProvider(options), options);

        var raw = service.Issue(new IdTokenRequest(
            Subject: "42",
            Audience: "client-abc",
            Nonce: "n-0S6_WzA2Mj",
            AuthenticationTime: DateTimeOffset.UtcNow,
            AccessToken: "an-access-token"));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(raw);

        Assert.Equal("https://sso.example.com", token.Issuer);
        Assert.Contains("client-abc", token.Audiences);
        Assert.Equal("42", token.Claims.First(claim => claim.Type == OidcConstants.Claims.Subject).Value);
        Assert.Equal("n-0S6_WzA2Mj", token.Claims.First(claim => claim.Type == OidcConstants.Claims.Nonce).Value);
        Assert.Contains(token.Claims, claim => claim.Type == OidcConstants.Claims.AccessTokenHash);
        Assert.Contains(token.Claims, claim => claim.Type == OidcConstants.Claims.AuthTime);
        Assert.Equal("RS256", token.Header.Alg);
        Assert.False(string.IsNullOrWhiteSpace(token.Header.Kid));

        File.Delete(options.Value.SigningKeyPath);
    }

    /// <summary>
    /// 发现文档的端点全部由签发者根地址推导，且声明 RS256。
    /// </summary>
    [Fact]
    public void DiscoveryDocument_ShouldDeriveEndpointsFromIssuer()
    {
        var document = OidcDiscoveryDocument.Create(new OidcOptions { Issuer = "https://sso.example.com/" });

        Assert.Equal("https://sso.example.com", document.Issuer);
        Assert.Equal("https://sso.example.com/connect/token", document.TokenEndpoint);
        Assert.Equal("https://sso.example.com/.well-known/jwks.json", document.JwksUri);
        Assert.Equal("https://sso.example.com/connect/userinfo", document.UserInfoEndpoint);
        Assert.Contains("RS256", document.IdTokenSigningAlgValuesSupported);
        Assert.Contains("S256", document.CodeChallengeMethodsSupported);
    }

    private static SysUser BuildUser()
    {
        return new SysUser
        {
            UserName = "alice",
            NickName = "Alice",
            Avatar = "https://cdn.example.com/a.png",
            Email = "alice@example.com",
            Phone = "13800000000"
        };
    }
}
