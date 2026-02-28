#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDepartmentAppService
// Guid:00ce1fbc-d066-4802-b5a7-4d446c714732
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:47:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Core.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices;

/// <summary>
/// 部门应用服务
/// </summary>
public interface IDepartmentAppService
    : ICrudApplicationService<DepartmentDto, long, DepartmentCreateDto, DepartmentUpdateDto, BasicAppPRDto>
{
    /// <summary>
    /// 获取子部门
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task<IReadOnlyList<DepartmentDto>> GetChildrenAsync(long? parentId, long? tenantId = null);
}
