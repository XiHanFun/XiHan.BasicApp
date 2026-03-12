#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CacheDto
// Guid:1a3905a5-e276-4be4-a458-cf08fef42099
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 14:09:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统缓存快照 DTO
/// </summary>
public class SysCacheSnapshotDto
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 权限缓存版本
    /// </summary>
    public long PermissionVersion { get; set; }

    /// <summary>
    /// 数据范围缓存版本
    /// </summary>
    public long DataScopeVersion { get; set; }

    /// <summary>
    /// 文件查找缓存版本
    /// </summary>
    public long FileLookupVersion { get; set; }

    /// <summary>
    /// 任务查找缓存版本
    /// </summary>
    public long TaskLookupVersion { get; set; }

    /// <summary>
    /// OAuth 应用查找缓存版本
    /// </summary>
    public long OAuthAppLookupVersion { get; set; }

    /// <summary>
    /// 消息未读缓存版本
    /// </summary>
    public long MessageUnreadVersion { get; set; }

    /// <summary>
    /// 采集时间
    /// </summary>
    public DateTimeOffset CollectedAt { get; set; } = DateTimeOffset.UtcNow;
}
