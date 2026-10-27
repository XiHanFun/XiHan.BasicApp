// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 用户统计（租户库，表 <c>sys_user_statistics</c>）：随用户创建在其注册地租户初始化一行，由框架仓储按租户过滤并随租户分库落到 <c>shard</c> 连接。
/// </summary>
public sealed class SysUserStatistics : TenantScopedAuditedEntity
{
    /// <summary>用户 ID。</summary>
    public Guid UserId { get; set; }

    /// <summary>登录次数。</summary>
    public int LoginCount { get; set; }

    /// <summary>最近登录时间。</summary>
    public DateTimeOffset? LastLoginAt { get; set; }
}
