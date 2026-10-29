// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace XiHan.BasicApp.Files.Infrastructure;

/// <summary>
/// 签名下载地址：令牌是数据保护加密的「侧 + 文件 ID」，带到期时间，地址在有效期内不再校验身份（给 <c>&lt;img&gt;</c>、链接与新窗口用）。
/// 框架的对象存储没有预签名，所以一律签发应用自己的下载端点；换取地址时按访问级别核对调用方，地址本身只认令牌。
/// 多实例部署要共用数据保护的密钥环（<c>BasicApp:DataProtection:KeysDirectory</c>）。
/// 过渡实现：框架的预签名（开发计划 P5-30）推送、锁定升上去后，签名地址改由对象存储签发，这个类与签名下载端点随之替换。
/// </summary>
/// <param name="protection">数据保护。</param>
public sealed class FileLinks(IDataProtectionProvider protection)
{
    /// <summary>
    /// 下载端点的路径前缀（后接令牌）。
    /// </summary>
    public const string PathPrefix = "/api/files/content/";

    private readonly ITimeLimitedDataProtector _protector = protection.CreateProtector("XiHan.BasicApp.Files.Link").ToTimeLimitedDataProtector();

    /// <summary>
    /// 签发令牌。
    /// </summary>
    /// <param name="side">文件所在的侧。</param>
    /// <param name="fileId">文件 ID。</param>
    /// <param name="expiresAt">到期时间。</param>
    /// <returns>令牌（URL 安全）。</returns>
    public string Issue(string side, Guid fileId, DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(side);
        return _protector.Protect($"{fileId:N}/{side}", expiresAt);
    }

    /// <summary>
    /// 解开令牌。
    /// </summary>
    /// <param name="token">令牌。</param>
    /// <param name="side">文件所在的侧。</param>
    /// <param name="fileId">文件 ID。</param>
    /// <returns>令牌有效且未到期时为 <see langword="true"/>。</returns>
    public bool TryRead(string token, out string side, out Guid fileId)
    {
        side = string.Empty;
        fileId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string payload;
        try
        {
            payload = _protector.Unprotect(token, out _);
        }
        catch (CryptographicException)
        {
            // 篡改、到期或密钥环里没有对应的密钥：都按地址无效处理
            return false;
        }

        var slash = payload.IndexOf('/', StringComparison.Ordinal);
        if (slash <= 0 || slash == payload.Length - 1 || !Guid.TryParseExact(payload[..slash], "N", out fileId))
        {
            return false;
        }

        side = payload[(slash + 1)..];
        return true;
    }
}
