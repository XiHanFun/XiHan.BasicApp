// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.OpenPlatform.Domain.Entities;
using XiHan.BasicApp.OpenPlatform.Infrastructure.OAuth;

namespace XiHan.BasicApp.OpenPlatform.Operations.OauthSigningKeys;

/// <summary>
/// <c>oauth-signing-keys.list</c> 与 <c>oauth-signing-keys.rotate</c> 的请求：没有输入
/// </summary>
public sealed record OAuthSigningKeysRequest;

/// <summary>
/// 按 ID 停用一把签名密钥的请求
/// </summary>
/// <param name="Id">密钥行 ID</param>
public sealed record OAuthSigningKeyIdRequest(Guid Id);

/// <summary>
/// 签名密钥的状态
/// </summary>
public enum OAuthSigningKeyStatus
{
    /// <summary>
    /// 正在签名
    /// </summary>
    Signing = 0,

    /// <summary>
    /// 已公布，到了开始签名的时间接替签名
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 已被接替，仍公布，验它签过的 id_token
    /// </summary>
    Retained = 2,

    /// <summary>
    /// 已停用，不再公布
    /// </summary>
    Retired = 3,
}

/// <summary>
/// 一把签名密钥（私钥不回显）
/// </summary>
/// <param name="Id">密钥行 ID</param>
/// <param name="KeyId">密钥 ID（JWKS 的 <c>kid</c>）</param>
/// <param name="Status">状态</param>
/// <param name="SignsFrom">开始签名的时间</param>
/// <param name="CreatedAt">生成时间</param>
/// <param name="RetiredAt">停用时间</param>
public sealed record OAuthSigningKeyResponse(Guid Id, string KeyId, OAuthSigningKeyStatus Status, DateTimeOffset SignsFrom, DateTimeOffset CreatedAt, DateTimeOffset? RetiredAt)
{
    /// <summary>
    /// 由实体转成响应
    /// </summary>
    /// <param name="key">密钥</param>
    /// <param name="signing">是否此刻签名的那把</param>
    /// <param name="now">此刻</param>
    public static OAuthSigningKeyResponse From(SysOAuthSigningKey key, bool signing, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(key);
        var status = !key.IsActive ? OAuthSigningKeyStatus.Retired
            : key.SignsFrom > now ? OAuthSigningKeyStatus.Pending
            : signing ? OAuthSigningKeyStatus.Signing
            : OAuthSigningKeyStatus.Retained;
        return new OAuthSigningKeyResponse(key.Id, key.KeyId, status, key.SignsFrom ?? key.CreatedAt, key.CreatedAt, key.RetiredAt);
    }
}

/// <summary>
/// 全部签名密钥（含停用的，新的在前）
/// </summary>
/// <param name="Items">密钥</param>
/// <param name="RotationLeadSeconds">轮换出的新密钥从公布到接替签名的秒数</param>
public sealed record OAuthSigningKeysResponse(IReadOnlyList<OAuthSigningKeyResponse> Items, int RotationLeadSeconds)
{
    /// <summary>
    /// 由实体转成响应
    /// </summary>
    /// <param name="keys">密钥</param>
    /// <param name="now">此刻</param>
    public static OAuthSigningKeysResponse From(IReadOnlyList<SysOAuthSigningKey> keys, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(keys);
        var signing = OAuthSigningKeys.SigningOf(keys, now);
        return new OAuthSigningKeysResponse([.. keys.Select(k => OAuthSigningKeyResponse.From(k, k.Id == signing?.Id, now))], (int)OAuthSigningKeys.RotationLead.TotalSeconds);
    }
}
