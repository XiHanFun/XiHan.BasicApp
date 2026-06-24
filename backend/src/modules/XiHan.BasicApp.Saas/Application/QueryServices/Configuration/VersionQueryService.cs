#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:VersionQueryService
// Guid:d8bb509b-927f-4302-bd19-36190df3dc82
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 系统版本查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统版本")]
public sealed class VersionQueryService
    : SaasApplicationService, IVersionQueryService
{
    /// <summary>
    /// 系统版本仓储
    /// </summary>
    private readonly IVersionRepository _versionRepository;

    /// <summary>
    /// 系统迁移历史仓储
    /// </summary>
    private readonly IMigrationHistoryRepository _migrationHistoryRepository;

    /// <summary>
    /// 字段级安全（排序门控）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public VersionQueryService(
        IVersionRepository versionRepository,
        IMigrationHistoryRepository migrationHistoryRepository,
        IFieldSecurityService fieldSecurityService)
    {
        _versionRepository = versionRepository;
        _migrationHistoryRepository = migrationHistoryRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <summary>
    /// 获取系统版本分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统版本分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Version.Read)]
    public async Task<PageResultDtoBase<VersionListItemDto>> GetVersionPageAsync(VersionPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildVersionPageRequest(input);

        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysVersion", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyVersionSorts(request);
        }

        var versions = await _versionRepository.GetPagedAsync(request, cancellationToken);
        return versions.Map(VersionApplicationMapper.ToListItemDto);
    }

    /// <summary>
    /// 获取系统版本详情
    /// </summary>
    /// <param name="id">系统版本主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统版本详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.Version.Read)]
    public async Task<VersionDetailDto?> GetVersionDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "系统版本主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var version = await _versionRepository.GetByIdAsync(id, cancellationToken);
        return version is null ? null : VersionApplicationMapper.ToDetailDto(version);
    }

    /// <summary>
    /// 获取系统迁移历史分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统迁移历史分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Version.Read)]
    public async Task<PageResultDtoBase<MigrationHistoryListItemDto>> GetMigrationHistoryPageAsync(MigrationHistoryPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildMigrationHistoryPageRequest(input);
        var migrationHistories = await _migrationHistoryRepository.GetPagedAsync(request, cancellationToken);
        return migrationHistories.Map(VersionApplicationMapper.ToMigrationHistoryListItemDto);
    }

    /// <summary>
    /// 获取系统迁移历史详情
    /// </summary>
    /// <param name="id">系统迁移历史主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统迁移历史详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.Version.Read)]
    public async Task<MigrationHistoryDetailDto?> GetMigrationHistoryDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "系统迁移历史主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var migrationHistory = await _migrationHistoryRepository.GetByIdAsync(id, cancellationToken);
        return migrationHistory is null ? null : VersionApplicationMapper.ToMigrationHistoryDetailDto(migrationHistory);
    }

    /// <summary>
    /// 构建系统版本分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>系统版本分页请求</returns>
    private static BasicAppPRDto BuildVersionPageRequest(VersionPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysVersion>(
                input.Keyword.Trim(),
                version => version.AppVersion,
                version => version.DbVersion,
                version => version.MinSupportVersion,
                version => version.UpgradeNode);
        }

        if (!string.IsNullOrWhiteSpace(input.AppVersion))
        {
            request.Conditions.AddFilter((SysVersion version) => version.AppVersion, input.AppVersion.Trim());
        }

        if (!string.IsNullOrWhiteSpace(input.DbVersion))
        {
            request.Conditions.AddFilter((SysVersion version) => version.DbVersion, input.DbVersion.Trim());
        }

        if (!string.IsNullOrWhiteSpace(input.MinSupportVersion))
        {
            request.Conditions.AddFilter((SysVersion version) => version.MinSupportVersion, input.MinSupportVersion.Trim());
        }

        if (input.IsUpgrading.HasValue)
        {
            request.Conditions.AddFilter((SysVersion version) => version.IsUpgrading, input.IsUpgrading.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.UpgradeNode))
        {
            request.Conditions.AddFilter((SysVersion version) => version.UpgradeNode, input.UpgradeNode.Trim());
        }

        if (input.UpgradeStartTimeStart.HasValue)
        {
            request.Conditions.AddFilter((SysVersion version) => version.UpgradeStartTime, input.UpgradeStartTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.UpgradeStartTimeEnd.HasValue)
        {
            request.Conditions.AddFilter((SysVersion version) => version.UpgradeStartTime, input.UpgradeStartTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        // 前端选择的排序原样带入（FLS 门控与默认兜底在调用方 GetVersionPageAsync 处理）
        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }
        return request;
    }

    /// <summary>
    /// 应用系统版本默认排序
    /// </summary>
    private static void ApplyVersionSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysVersion version) => version.CreatedTime, SortDirection.Descending, 0);
        request.Conditions.AddSort((SysVersion version) => version.AppVersion, SortDirection.Descending, 1);
    }

    /// <summary>
    /// 构建系统迁移历史分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>系统迁移历史分页请求</returns>
    private static BasicAppPRDto BuildMigrationHistoryPageRequest(MigrationHistoryPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysMigrationHistory>(
                input.Keyword.Trim(),
                history => history.Version,
                history => history.ScriptName,
                history => history.NodeName);
        }

        if (!string.IsNullOrWhiteSpace(input.Version))
        {
            request.Conditions.AddFilter((SysMigrationHistory history) => history.Version, input.Version.Trim());
        }

        if (!string.IsNullOrWhiteSpace(input.ScriptName))
        {
            request.Conditions.AddFilter((SysMigrationHistory history) => history.ScriptName, input.ScriptName.Trim());
        }

        if (input.Success.HasValue)
        {
            request.Conditions.AddFilter((SysMigrationHistory history) => history.Success, input.Success.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.NodeName))
        {
            request.Conditions.AddFilter((SysMigrationHistory history) => history.NodeName, input.NodeName.Trim());
        }

        if (input.ExecutedTimeStart.HasValue)
        {
            request.Conditions.AddFilter((SysMigrationHistory history) => history.ExecutedTime, input.ExecutedTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.ExecutedTimeEnd.HasValue)
        {
            request.Conditions.AddFilter((SysMigrationHistory history) => history.ExecutedTime, input.ExecutedTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        request.Conditions.AddSort((SysMigrationHistory history) => history.ExecutedTime, SortDirection.Descending, 0);
        request.Conditions.AddSort((SysMigrationHistory history) => history.Version, SortDirection.Descending, 1);
        request.Conditions.AddSort((SysMigrationHistory history) => history.ScriptName, SortDirection.Ascending, 2);
        return request;
    }
}
