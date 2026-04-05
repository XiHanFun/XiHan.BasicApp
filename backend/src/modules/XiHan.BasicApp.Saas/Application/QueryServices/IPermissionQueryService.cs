#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPermissionQueryService
// Guid:5d6e7f80-9102-1234-def0-123456789a01
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Queries;
using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 权限查询服务接口
/// </summary>
public interface IPermissionQueryService : IQueryService
{
    /// <summary>
    /// 根据 ID 获取权限
    /// </summary>
    Task<PermissionDto?> GetByIdAsync(long id);
}
