// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 字段级安全领域服务
/// </summary>
public interface IFieldLevelSecurityDomainService
{
    /// <summary>
    /// 创建字段级安全策略
    /// </summary>
    Task<FieldLevelSecurityCommandResult> CreateAsync(FieldLevelSecurityCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新字段级安全策略
    /// </summary>
    Task<FieldLevelSecurityCommandResult> UpdateAsync(FieldLevelSecurityUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新字段级安全策略状态
    /// </summary>
    Task<FieldLevelSecurityCommandResult> UpdateStatusAsync(FieldLevelSecurityStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除字段级安全策略
    /// </summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
