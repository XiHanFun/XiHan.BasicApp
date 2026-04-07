#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDepartmentQueryService
// Guid:3b4c5d6e-7f80-9012-bcde-f01234567801
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Queries;
using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 部门查询服务接口
/// </summary>
public interface IDepartmentQueryService : IQueryService
{
    /// <summary>
    /// 根据 ID 获取部门
    /// </summary>
    Task<DepartmentDto?> GetByIdAsync(long id);
}
