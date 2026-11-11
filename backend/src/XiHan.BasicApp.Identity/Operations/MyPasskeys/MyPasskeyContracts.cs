// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Security.WebAuthn;

namespace XiHan.BasicApp.Identity.Operations.MyPasskeys;

/// <summary>
/// <c>my-passkeys.list</c> 的请求
/// </summary>
public sealed record MyPasskeysRequest;

/// <summary>
/// 本人的一个通行密钥
/// </summary>
/// <param name="Id">通行密钥 ID</param>
/// <param name="Name">名字</param>
/// <param name="BackupEligible">能否在设备之间同步</param>
/// <param name="BackedUp">是否已同步备份</param>
/// <param name="Transports">认证器支持的传输方式</param>
/// <param name="CreatedAt">登记时间</param>
/// <param name="LastUsedAt">最近一次用它登录的时间</param>
public sealed record MyPasskeyResponse(Guid Id, string Name, bool BackupEligible, bool BackedUp, IReadOnlyList<string> Transports, DateTimeOffset CreatedAt, DateTimeOffset? LastUsedAt)
{
    /// <summary>
    /// 从实体投影
    /// </summary>
    /// <param name="passkey">通行密钥</param>
    public static MyPasskeyResponse From(SysUserPasskey passkey)
    {
        ArgumentNullException.ThrowIfNull(passkey);
        IReadOnlyList<string> transports = string.IsNullOrEmpty(passkey.Transports) ? [] : passkey.Transports.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return new MyPasskeyResponse(passkey.Id, passkey.Name, passkey.BackupEligible, passkey.BackedUp, transports, passkey.CreatedAt, passkey.LastUsedAt);
    }
}

/// <summary>
/// 本人的全部通行密钥
/// </summary>
/// <param name="Items">通行密钥</param>
public sealed record MyPasskeysResponse(IReadOnlyList<MyPasskeyResponse> Items);

/// <summary>
/// <c>my-passkeys.begin-create</c> 的请求
/// </summary>
/// <param name="CurrentPassword">当前密码</param>
public sealed record BeginCreateMyPasskeyRequest([Required, StringLength(128, MinimumLength = 1)] string CurrentPassword);

/// <summary>
/// 登记通行密钥的创建选项
/// </summary>
/// <param name="Ticket">一次性票据，确认登记时带回</param>
/// <param name="Options">交给浏览器 <c>navigator.credentials.create</c> 的选项（WebAuthn JSON 形态）</param>
public sealed record BeginCreateMyPasskeyResponse(string Ticket, WebAuthnCreationOptions Options);

/// <summary>
/// <c>my-passkeys.create</c> 的请求
/// </summary>
/// <param name="Ticket">发起登记时给的票据</param>
/// <param name="Name">名字；留空即缺省名</param>
/// <param name="Credential">浏览器创建的凭据（<c>PublicKeyCredential.toJSON()</c>）</param>
public sealed record CreateMyPasskeyRequest([Required, StringLength(128)] string Ticket, [StringLength(64)] string? Name, [Required] WebAuthnRegistrationResponse Credential);

/// <summary>
/// <c>my-passkeys.update</c> 的请求
/// </summary>
/// <param name="Id">通行密钥 ID</param>
/// <param name="Name">新名字</param>
public sealed record UpdateMyPasskeyRequest(Guid Id, [Required, StringLength(64, MinimumLength = 1)] string Name);

/// <summary>
/// <c>my-passkeys.delete</c> 的请求
/// </summary>
/// <param name="Ids">通行密钥 ID</param>
public sealed record MyPasskeyIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
