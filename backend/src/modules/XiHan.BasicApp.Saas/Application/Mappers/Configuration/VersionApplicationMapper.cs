#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:VersionApplicationMapper
// Guid:cd90954c-5e5f-42d1-9d42-bf1af3714741
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 系统版本应用层映射器
/// </summary>
public static class VersionApplicationMapper
{
    /// <summary>
    /// 映射系统版本列表项
    /// </summary>
    /// <param name="version">系统版本实体</param>
    /// <returns>系统版本列表项 DTO</returns>
    public static VersionListItemDto ToListItemDto(SysVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        return new VersionListItemDto
        {
            BasicId = version.BasicId,
            AppVersion = version.AppVersion,
            DbVersion = version.DbVersion,
            MinSupportVersion = version.MinSupportVersion,
            IsUpgrading = version.IsUpgrading,
            UpgradeNode = version.UpgradeNode,
            UpgradeStartTime = version.UpgradeStartTime,
            CreatedTime = version.CreatedTime
        };
    }

    /// <summary>
    /// 映射系统版本详情
    /// </summary>
    /// <param name="version">系统版本实体</param>
    /// <returns>系统版本详情 DTO</returns>
    public static VersionDetailDto ToDetailDto(SysVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        var item = ToListItemDto(version);
        return new VersionDetailDto
        {
            BasicId = item.BasicId,
            AppVersion = item.AppVersion,
            DbVersion = item.DbVersion,
            MinSupportVersion = item.MinSupportVersion,
            IsUpgrading = item.IsUpgrading,
            UpgradeNode = item.UpgradeNode,
            UpgradeStartTime = item.UpgradeStartTime,
            CreatedTime = item.CreatedTime,
            CreatedId = version.CreatedId,
            CreatedBy = version.CreatedBy
        };
    }

    /// <summary>
    /// 映射系统迁移历史列表项
    /// </summary>
    /// <param name="migrationHistory">系统迁移历史实体</param>
    /// <returns>系统迁移历史列表项 DTO</returns>
    public static MigrationHistoryListItemDto ToMigrationHistoryListItemDto(SysMigrationHistory migrationHistory)
    {
        ArgumentNullException.ThrowIfNull(migrationHistory);

        return new MigrationHistoryListItemDto
        {
            BasicId = migrationHistory.BasicId,
            Version = migrationHistory.Version,
            ScriptName = migrationHistory.ScriptName,
            ExecutedTime = migrationHistory.ExecutedTime,
            Success = migrationHistory.Success,
            NodeName = migrationHistory.NodeName,
            HasFailureDetail = !string.IsNullOrWhiteSpace(migrationHistory.ErrorMessage),
            CreatedTime = migrationHistory.CreatedTime
        };
    }

    /// <summary>
    /// 映射系统迁移历史详情
    /// </summary>
    /// <param name="migrationHistory">系统迁移历史实体</param>
    /// <returns>系统迁移历史详情 DTO</returns>
    public static MigrationHistoryDetailDto ToMigrationHistoryDetailDto(SysMigrationHistory migrationHistory)
    {
        ArgumentNullException.ThrowIfNull(migrationHistory);

        var item = ToMigrationHistoryListItemDto(migrationHistory);
        return new MigrationHistoryDetailDto
        {
            BasicId = item.BasicId,
            Version = item.Version,
            ScriptName = item.ScriptName,
            ExecutedTime = item.ExecutedTime,
            Success = item.Success,
            NodeName = item.NodeName,
            HasFailureDetail = item.HasFailureDetail,
            CreatedTime = item.CreatedTime,
            CreatedId = migrationHistory.CreatedId,
            CreatedBy = migrationHistory.CreatedBy
        };
    }
}
