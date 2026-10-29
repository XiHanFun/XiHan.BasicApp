// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.OpenPlatform.Domain.Entities;

/// <summary>
/// 用户对某个应用的授权同意。
/// </summary>
/// <remarks>
/// 平台库实体；4.x 不落库，每次都要确认。
/// 同意过的范围覆盖本次请求的就不再弹同意页；用户在个人中心撤销即删掉它并吊销这个应用的全部令牌。
/// </remarks>
[DiffLogIgnored]
public sealed class SysOAuthConsent : AppAuditedEntity
{
    /// <summary>
    /// 用户。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// ClientId。
    /// </summary>
    [StringLength(100)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 同意过的范围（空格分隔，累积）。
    /// </summary>
    [StringLength(200)]
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// 最近一次同意的时间。
    /// </summary>
    public DateTimeOffset GrantedAt { get; set; }
}
