#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IMenuDomainService
// Guid:4c5d6e7f-8091-0123-cdef-012345678903
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 菜单领域服务接口
/// </summary>
public interface IMenuDomainService
{
    /// <summary>
    /// 创建菜单
    /// </summary>
    Task<SysMenu> CreateAsync(SysMenu menu);

    /// <summary>
    /// 更新菜单
    /// </summary>
    Task<SysMenu> UpdateAsync(SysMenu menu);

    /// <summary>
    /// 删除菜单
    /// </summary>
    Task<bool> DeleteAsync(long id);
}
