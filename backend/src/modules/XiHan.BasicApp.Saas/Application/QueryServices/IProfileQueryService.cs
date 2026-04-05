#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IProfileQueryService
// Guid:d3e4f5a6-b7c8-4901-2345-6789abcdef01
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Queries;
using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 个人资料查询服务接口
/// </summary>
public interface IProfileQueryService : IQueryService
{
    /// <summary>
    /// 根据用户 ID 获取当前用户资料
    /// </summary>
    Task<UserDto?> GetCurrentUserAsync(long userId);
}
