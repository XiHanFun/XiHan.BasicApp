// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 版本领域服务实现
/// </summary>
public sealed class VersionDomainService
    : IVersionDomainService
{
    private readonly IVersionRepository _versionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public VersionDomainService(IVersionRepository versionRepository)
    {
        _versionRepository = versionRepository;
    }

    /// <inheritdoc />
    public async Task<VersionCommandResult> CreateVersionAsync(VersionCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateVersionCommand(command.AppVersion, command.DbVersion, command.MinSupportVersion, command.UpgradeNode);
        var version = new SysVersion
        {
            AppVersion = Required(command.AppVersion, 64, nameof(command.AppVersion), "应用版本不能超过 64 个字符。"),
            DbVersion = Required(command.DbVersion, 64, nameof(command.DbVersion), "数据库版本不能超过 64 个字符。"),
            MinSupportVersion = Optional(command.MinSupportVersion, 64, nameof(command.MinSupportVersion), "最小支持版本不能超过 64 个字符。"),
            IsUpgrading = command.IsUpgrading,
            UpgradeNode = Optional(command.UpgradeNode, 128, nameof(command.UpgradeNode), "升级节点不能超过 128 个字符。"),
            UpgradeStartTime = command.IsUpgrading ? command.UpgradeStartTime ?? DateTimeOffset.UtcNow : command.UpgradeStartTime
        };

        return new VersionCommandResult(await _versionRepository.AddAsync(version, cancellationToken));
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<VersionCommandResult> FinishVersionUpgradeAsync(VersionUpgradeFinishCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var version = await GetVersionOrThrowAsync(command.BasicId, cancellationToken);
        version.IsUpgrading = false;
        if (!string.IsNullOrWhiteSpace(command.AppVersion))
        {
            version.AppVersion = Required(command.AppVersion, 64, nameof(command.AppVersion), "应用版本不能超过 64 个字符。");
        }

        if (!string.IsNullOrWhiteSpace(command.DbVersion))
        {
            version.DbVersion = Required(command.DbVersion, 64, nameof(command.DbVersion), "数据库版本不能超过 64 个字符。");
        }

        version.MinSupportVersion = Optional(command.MinSupportVersion, 64, nameof(command.MinSupportVersion), "最小支持版本不能超过 64 个字符。") ?? version.MinSupportVersion;

        return new VersionCommandResult(await _versionRepository.UpdateAsync(version, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<VersionCommandResult> StartVersionUpgradeAsync(VersionUpgradeStartCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var version = await GetVersionOrThrowAsync(command.BasicId, cancellationToken);
        version.IsUpgrading = true;
        version.UpgradeNode = Optional(command.UpgradeNode, 128, nameof(command.UpgradeNode), "升级节点不能超过 128 个字符。");
        version.UpgradeStartTime = command.UpgradeStartTime ?? DateTimeOffset.UtcNow;

        return new VersionCommandResult(await _versionRepository.UpdateAsync(version, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<VersionCommandResult> UpdateVersionAsync(VersionUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "系统版本主键必须大于 0。");
        ValidateVersionCommand(command.AppVersion, command.DbVersion, command.MinSupportVersion, command.UpgradeNode);
        var version = await GetVersionOrThrowAsync(command.BasicId, cancellationToken);
        version.AppVersion = Required(command.AppVersion, 64, nameof(command.AppVersion), "应用版本不能超过 64 个字符。");
        version.DbVersion = Required(command.DbVersion, 64, nameof(command.DbVersion), "数据库版本不能超过 64 个字符。");
        version.MinSupportVersion = Optional(command.MinSupportVersion, 64, nameof(command.MinSupportVersion), "最小支持版本不能超过 64 个字符。");
        version.IsUpgrading = command.IsUpgrading;
        version.UpgradeNode = Optional(command.UpgradeNode, 128, nameof(command.UpgradeNode), "升级节点不能超过 128 个字符。");
        version.UpgradeStartTime = command.IsUpgrading ? command.UpgradeStartTime ?? version.UpgradeStartTime ?? DateTimeOffset.UtcNow : command.UpgradeStartTime;

        return new VersionCommandResult(await _versionRepository.UpdateAsync(version, cancellationToken));
    }

    private static void ValidateVersionCommand(string appVersion, string dbVersion, string? minSupportVersion, string? upgradeNode)
    {
        _ = Required(appVersion, 64, nameof(appVersion), "应用版本不能超过 64 个字符。");
        _ = Required(dbVersion, 64, nameof(dbVersion), "数据库版本不能超过 64 个字符。");
        _ = Optional(minSupportVersion, 64, nameof(minSupportVersion), "最小支持版本不能超过 64 个字符。");
        _ = Optional(upgradeNode, 128, nameof(upgradeNode), "升级节点不能超过 128 个字符。");
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }

    private static string? Optional(string? value, int maxLength, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private static string Required(string? value, int maxLength, string paramName, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private async Task<SysVersion> GetVersionOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "系统版本主键必须大于 0。");
        return await _versionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统版本不存在。");
    }
}
