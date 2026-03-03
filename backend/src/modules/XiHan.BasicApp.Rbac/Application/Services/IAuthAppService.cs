#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAuthAppService
// Guid:3f99078f-71c5-4fa0-a685-c8c3ad90addf
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 15:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.UseCases.Commands;
using XiHan.BasicApp.Rbac.Application.UseCases.Queries;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.Services;

/// <summary>
/// 认证应用服务
/// </summary>
public interface IAuthAppService : IApplicationService
{
    /// <summary>
    /// 登录
    /// </summary>
    Task<UserLoginResultDto> LoginAsync(UserLoginCommand command);

    /// <summary>
    /// 修改密码
    /// </summary>
    Task ChangePasswordAsync(ChangePasswordCommand command);

    /// <summary>
    /// 获取用户权限编码
    /// </summary>
    Task<IReadOnlyCollection<string>> GetPermissionCodesAsync(UserPermissionQuery query);

    /// <summary>
    /// 获取用户数据范围部门ID
    /// </summary>
    /// <remarks>
    /// 空集合表示不限部门（全量数据范围）。
    /// </remarks>
    Task<IReadOnlyCollection<long>> GetDataScopeDepartmentIdsAsync(UserDataScopeQuery query);
}
