#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITenantQueryService
// Guid:2a3b4c5d-6e7f-4901-bcde-210000000001
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Queries;
using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 租户查询服务接口
/// </summary>
public interface ITenantQueryService : IQueryService
{
    /// <summary>
    /// 根据 ID 获取租户
    /// </summary>
    Task<TenantDto?> GetByIdAsync(long id);

    /// <summary>
    /// 根据编码获取租户。
    /// </summary>
    Task<TenantDto?> GetByCodeAsync(string tenantCode);
}
