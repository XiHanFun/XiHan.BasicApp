#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:VersionAppService
// Guid:c32f63f3-dda0-4288-a51c-acd6ebd326bd
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;
using static XiHan.BasicApp.Saas.Application.AppServices.SaasCommandValidation;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统版本命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统版本")]
public sealed class VersionAppService
    : SaasApplicationService, IVersionAppService
{
    private readonly IVersionRepository _versionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public VersionAppService(IVersionRepository versionRepository)
    {
        _versionRepository = versionRepository;
    }
    /// <summary>
    /// 创建系统版本
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Version.Create)]
    public async Task<VersionDetailDto> CreateVersionAsync(VersionCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateVersionInput(input.AppVersion, input.DbVersion, input.MinSupportVersion, input.UpgradeNode);
        var version = new SysVersion
        {
            AppVersion = Required(input.AppVersion, 64, nameof(input.AppVersion), "应用版本不能超过 64 个字符。"),
            DbVersion = Required(input.DbVersion, 64, nameof(input.DbVersion), "数据库版本不能超过 64 个字符。"),
            MinSupportVersion = Optional(input.MinSupportVersion, 64, nameof(input.MinSupportVersion), "最小支持版本不能超过 64 个字符。"),
            IsUpgrading = input.IsUpgrading,
            UpgradeNode = Optional(input.UpgradeNode, 128, nameof(input.UpgradeNode), "升级节点不能超过 128 个字符。"),
            UpgradeStartTime = input.IsUpgrading ? input.UpgradeStartTime ?? DateTimeOffset.UtcNow : input.UpgradeStartTime
        };

        var savedVersion = await _versionRepository.AddAsync(version, cancellationToken);
        return VersionApplicationMapper.ToDetailDto(savedVersion);
    }

    /// <summary>
    /// 删除系统版本
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Version.Delete)]
    public async Task DeleteVersionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var version = await GetVersionOrThrowAsync(id, cancellationToken);
        if (version.IsUpgrading)
        {
            throw new InvalidOperationException("系统升级中的版本记录不能删除。");
        }

        if (!await _versionRepository.DeleteAsync(version, cancellationToken))
        {
            throw new InvalidOperationException("系统版本删除失败。");
        }
    }

    /// <summary>
    /// 完成系统升级
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Version.Upgrade)]
    public async Task<VersionDetailDto> FinishVersionUpgradeAsync(VersionUpgradeFinishDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var version = await GetVersionOrThrowAsync(input.BasicId, cancellationToken);
        version.IsUpgrading = false;
        if (!string.IsNullOrWhiteSpace(input.AppVersion))
        {
            version.AppVersion = Required(input.AppVersion, 64, nameof(input.AppVersion), "应用版本不能超过 64 个字符。");
        }

        if (!string.IsNullOrWhiteSpace(input.DbVersion))
        {
            version.DbVersion = Required(input.DbVersion, 64, nameof(input.DbVersion), "数据库版本不能超过 64 个字符。");
        }

        version.MinSupportVersion = Optional(input.MinSupportVersion, 64, nameof(input.MinSupportVersion), "最小支持版本不能超过 64 个字符。") ?? version.MinSupportVersion;

        var savedVersion = await _versionRepository.UpdateAsync(version, cancellationToken);
        return VersionApplicationMapper.ToDetailDto(savedVersion);
    }

    /// <summary>
    /// 开始系统升级
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Version.Upgrade)]
    public async Task<VersionDetailDto> StartVersionUpgradeAsync(VersionUpgradeStartDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var version = await GetVersionOrThrowAsync(input.BasicId, cancellationToken);
        version.IsUpgrading = true;
        version.UpgradeNode = Optional(input.UpgradeNode, 128, nameof(input.UpgradeNode), "升级节点不能超过 128 个字符。");
        version.UpgradeStartTime = input.UpgradeStartTime ?? DateTimeOffset.UtcNow;

        var savedVersion = await _versionRepository.UpdateAsync(version, cancellationToken);
        return VersionApplicationMapper.ToDetailDto(savedVersion);
    }

    /// <summary>
    /// 更新系统版本
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Version.Update)]
    public async Task<VersionDetailDto> UpdateVersionAsync(VersionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统版本主键必须大于 0。");
        ValidateVersionInput(input.AppVersion, input.DbVersion, input.MinSupportVersion, input.UpgradeNode);
        var version = await GetVersionOrThrowAsync(input.BasicId, cancellationToken);
        version.AppVersion = Required(input.AppVersion, 64, nameof(input.AppVersion), "应用版本不能超过 64 个字符。");
        version.DbVersion = Required(input.DbVersion, 64, nameof(input.DbVersion), "数据库版本不能超过 64 个字符。");
        version.MinSupportVersion = Optional(input.MinSupportVersion, 64, nameof(input.MinSupportVersion), "最小支持版本不能超过 64 个字符。");
        version.IsUpgrading = input.IsUpgrading;
        version.UpgradeNode = Optional(input.UpgradeNode, 128, nameof(input.UpgradeNode), "升级节点不能超过 128 个字符。");
        version.UpgradeStartTime = input.IsUpgrading ? input.UpgradeStartTime ?? version.UpgradeStartTime ?? DateTimeOffset.UtcNow : input.UpgradeStartTime;

        var savedVersion = await _versionRepository.UpdateAsync(version, cancellationToken);
        return VersionApplicationMapper.ToDetailDto(savedVersion);
    }

    private static void ValidateVersionInput(string appVersion, string dbVersion, string? minSupportVersion, string? upgradeNode)
    {
        _ = Required(appVersion, 64, nameof(appVersion), "应用版本不能超过 64 个字符。");
        _ = Required(dbVersion, 64, nameof(dbVersion), "数据库版本不能超过 64 个字符。");
        _ = Optional(minSupportVersion, 64, nameof(minSupportVersion), "最小支持版本不能超过 64 个字符。");
        _ = Optional(upgradeNode, 128, nameof(upgradeNode), "升级节点不能超过 128 个字符。");
    }

    private async Task<SysVersion> GetVersionOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "系统版本主键必须大于 0。");
        return await _versionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统版本不存在。");
    }
}
