// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 版本领域服务
/// </summary>
public interface IVersionDomainService
{
    /// <summary>
    /// 创建版本
    /// </summary>
    Task<VersionCommandResult> CreateVersionAsync(VersionCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除版本
    /// </summary>
    Task DeleteVersionAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 完成版本升级
    /// </summary>
    Task<VersionCommandResult> FinishVersionUpgradeAsync(VersionUpgradeFinishCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 开始版本升级
    /// </summary>
    Task<VersionCommandResult> StartVersionUpgradeAsync(VersionUpgradeStartCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新版本
    /// </summary>
    Task<VersionCommandResult> UpdateVersionAsync(VersionUpdateCommand command, CancellationToken cancellationToken = default);
}
