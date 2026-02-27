#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITenantAppService
// Guid:a79416cd-ef2a-4eac-8c34-cc07f6839d29
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:47:08
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Commands;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.Queries;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices;

/// <summary>
/// 租户应用服务
/// </summary>
public interface ITenantAppService : IApplicationService
{
    /// <summary>
    /// 根据租户ID获取租户
    /// </summary>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task<TenantDto?> GetByIdAsync(long tenantId);

    /// <summary>
    /// 根据租户编码获取租户
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    Task<TenantDto?> GetByCodeAsync(TenantByCodeQuery query);

    /// <summary>
    /// 创建租户
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    Task<TenantDto> CreateAsync(TenantCreateDto input);

    /// <summary>
    /// 更新租户
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    Task<TenantDto> UpdateAsync(TenantUpdateDto input);

    /// <summary>
    /// 修改租户状态
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    Task ChangeStatusAsync(ChangeTenantStatusCommand command);
}
