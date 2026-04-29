#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleHierarchyAppService
// Guid:67474529-41c7-4507-a98b-68d76c836bf7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 角色继承命令应用服务接口
/// </summary>
public interface IRoleHierarchyAppService : IApplicationService
{
    /// <summary>
    /// 创建角色直接继承关系
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色继承详情</returns>
    Task<RoleHierarchyDetailDto> CreateRoleHierarchyAsync(RoleHierarchyCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色直接继承关系
    /// </summary>
    /// <param name="id">角色继承主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteRoleHierarchyAsync(long id, CancellationToken cancellationToken = default);
}
