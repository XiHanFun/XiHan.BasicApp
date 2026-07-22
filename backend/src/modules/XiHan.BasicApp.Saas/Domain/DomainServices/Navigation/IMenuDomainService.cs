// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 菜单领域服务
/// </summary>
public interface IMenuDomainService
{
    /// <summary>
    /// 创建菜单
    /// </summary>
    Task<MenuCommandResult> CreateAsync(MenuCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新菜单
    /// </summary>
    Task<MenuCommandResult> UpdateAsync(MenuUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新菜单状态
    /// </summary>
    Task<MenuCommandResult> UpdateStatusAsync(MenuStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除菜单
    /// </summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
