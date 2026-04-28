#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITenantQueryService
// Guid:db18b111-37f1-46f0-9fb4-c1b4e0cba16d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 租户查询应用服务接口
/// </summary>
public interface ITenantQueryService : IApplicationService
{
    /// <summary>
    /// 获取当前用户可进入的租户列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>当前用户可进入的租户列表</returns>
    Task<IReadOnlyList<TenantSwitcherDto>> GetMyAvailableTenantsAsync(CancellationToken cancellationToken = default);
}
