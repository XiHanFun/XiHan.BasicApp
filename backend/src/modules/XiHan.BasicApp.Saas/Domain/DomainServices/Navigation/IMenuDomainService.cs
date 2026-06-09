#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IMenuDomainService
// Guid:ba553958-f0d7-4b6b-9659-6a8f9ffcb1ec
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
