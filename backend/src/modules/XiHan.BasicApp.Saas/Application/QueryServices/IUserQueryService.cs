#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserQueryService
// Guid:3a4b5c6d-7e8f-4012-cdef-310000000001
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Queries;
using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 用户查询服务接口
/// </summary>
public interface IUserQueryService : IQueryService
{
    /// <summary>
    /// 根据 ID 获取用户
    /// </summary>
    Task<UserDto?> GetByIdAsync(long id);
}
