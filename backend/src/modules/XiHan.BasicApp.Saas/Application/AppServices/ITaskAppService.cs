#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITaskAppService
// Guid:2a90cf53-d30f-48bc-902b-9938198af969
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:39:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 任务应用服务
/// </summary>
public interface ITaskAppService
    : ICrudApplicationService<TaskDto, long, TaskCreateDto, TaskUpdateDto, BasicAppPRDto>
{
    /// <summary>
    /// 根据任务编码获取任务
    /// </summary>
    Task<TaskDto?> GetByTaskCodeAsync(string taskCode, long? tenantId = null);
}
