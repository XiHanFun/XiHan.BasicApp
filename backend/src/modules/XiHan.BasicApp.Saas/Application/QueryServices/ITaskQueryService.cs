#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITaskQueryService
// Guid:4a5b6c7d-8e9f-4123-def0-410000000001
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Queries;
using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 任务查询服务接口
/// </summary>
public interface ITaskQueryService : IQueryService
{
    /// <summary>
    /// 根据 ID 获取任务
    /// </summary>
    Task<TaskDto?> GetByIdAsync(long id);
}
