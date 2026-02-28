#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IMenuAppService
// Guid:e94c8abc-4b86-4a57-9a97-dac31f69f473
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:46:53
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.Queries;
using XiHan.BasicApp.Core.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices;

/// <summary>
/// 菜单应用服务
/// </summary>
public interface IMenuAppService
    : ICrudApplicationService<MenuDto, long, MenuCreateDto, MenuUpdateDto, BasicAppPRDto>
{
    /// <summary>
    /// 根据角色ID获取菜单
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task<IReadOnlyList<MenuDto>> GetRoleMenusAsync(long roleId, long? tenantId = null);

    /// <summary>
    /// 根据用户ID获取菜单
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    Task<IReadOnlyList<MenuDto>> GetUserMenusAsync(UserMenuQuery query);
}
