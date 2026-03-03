#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleAppService
// Guid:fb5f77dd-0653-4987-be28-f559db7f6ca2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:46:36
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Core.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.BasicApp.Rbac.Application.UseCases.Queries;

namespace XiHan.BasicApp.Rbac.Application.AppServices;

/// <summary>
/// 角色应用服务
/// </summary>
public interface IRoleAppService
    : ICrudApplicationService<RoleDto, long, RoleCreateDto, RoleUpdateDto, BasicAppPRDto>
{
    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    Task<RoleDto?> GetByCodeAsync(RoleByCodeQuery query);

}
