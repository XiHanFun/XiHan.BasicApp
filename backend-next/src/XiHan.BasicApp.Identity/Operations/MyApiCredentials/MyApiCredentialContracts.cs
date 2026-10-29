// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Identity.Domain.Entities;

namespace XiHan.BasicApp.Identity.Operations.MyApiCredentials;

/// <summary>
/// 开放接口凭证没有输入的请求。
/// </summary>
/// <remarks>
/// <c>my-api-credentials.list</c> 与 <c>my-api-credentials.settings</c> 的请求：没有输入。
/// </remarks>
public sealed record MyApiCredentialsRequest;

/// <summary>
/// 本人的一个开放接口凭证（密钥不回显）。
/// </summary>
/// <param name="Id">凭证 ID。</param>
/// <param name="Name">名称。</param>
/// <param name="AppKey">AppKey。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="LastUsedAt">最近一次验签通过的时间；从未用过为空。</param>
/// <param name="CreatedAt">创建时间。</param>
public sealed record MyApiCredentialResponse(Guid Id, string Name, string AppKey, bool IsEnabled, DateTimeOffset? LastUsedAt, DateTimeOffset CreatedAt)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    public static MyApiCredentialResponse From(SysUserApiCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);
        return new MyApiCredentialResponse(credential.Id, credential.Name, credential.AppKey, credential.IsEnabled, credential.LastUsedAt, credential.CreatedAt);
    }
}

/// <summary>
/// 本人的全部开放接口凭证（按创建先后）。
/// </summary>
/// <param name="Items">凭证。</param>
public sealed record MyApiCredentialsResponse(IReadOnlyList<MyApiCredentialResponse> Items);

/// <summary>
/// <c>my-api-credentials.create</c> 的请求。
/// </summary>
/// <param name="Name">名称；不填即「默认凭证」（按当前区域）。</param>
public sealed record CreateMyApiCredentialRequest([StringLength(100)] string? Name);

/// <summary>
/// <c>my-api-credentials.rotate-secret</c> 的请求。
/// </summary>
/// <param name="Id">凭证 ID。</param>
public sealed record MyApiCredentialIdRequest(Guid Id);

/// <summary>
/// <c>my-api-credentials.delete</c> 的请求。
/// </summary>
/// <param name="Ids">凭证 ID，1 到 100 个。</param>
public sealed record MyApiCredentialIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);

/// <summary>
/// <c>my-api-credentials.update</c> 的请求：启用或停用。
/// </summary>
/// <param name="Id">凭证 ID。</param>
/// <param name="IsEnabled">是否启用。</param>
public sealed record UpdateMyApiCredentialRequest(Guid Id, bool IsEnabled);

/// <summary>
/// 新建或滚动后的凭证：密钥只在这一次返回。
/// </summary>
/// <param name="Id">凭证 ID。</param>
/// <param name="AppKey">AppKey。</param>
/// <param name="Secret">密钥明文。</param>
public sealed record ApiCredentialSecretResponse(Guid Id, string AppKey, string Secret);

/// <summary>
/// 本人的调用安全设置（作用于本人全部凭证）。
/// </summary>
/// <param name="SignatureAlgorithm">签名算法。</param>
/// <param name="IpAllowList">IP 白名单（IP 或 CIDR）；空即不限。</param>
/// <param name="Algorithms">可选的签名算法。</param>
public sealed record MyApiSettingsResponse(string SignatureAlgorithm, IReadOnlyList<string> IpAllowList, IReadOnlyList<string> Algorithms);

/// <summary>
/// 写调用安全设置的请求。
/// </summary>
/// <remarks>
/// <c>my-api-credentials.update-settings</c> 的请求，整体覆盖。
/// </remarks>
/// <param name="SignatureAlgorithm">签名算法（<c>HmacSHA256</c> / <c>HmacSHA512</c>）。</param>
/// <param name="IpAllowList">IP 白名单（每条一个 IP 或 CIDR，最多 50 条）；空即不限。</param>
public sealed record UpdateMyApiSettingsRequest([Required, StringLength(32)] string SignatureAlgorithm, [MaxLength(50)] IReadOnlyList<string>? IpAllowList);
