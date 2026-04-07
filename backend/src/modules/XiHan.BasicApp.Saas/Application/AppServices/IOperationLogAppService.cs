#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOperationLogAppService
// Guid:1bd63f22-0b8d-48a9-a87a-c36a80760c1f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/10 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 操作日志应用服务接口
/// </summary>
public interface IOperationLogAppService : IApplicationService
{
    /// <summary>
    /// 分页查询
    /// </summary>
    Task<PageResultDtoBase<OperationLogDto>> PageAsync(BasicAppPRDto input);

    /// <summary>
    /// 清空日志
    /// </summary>
    Task<bool> ClearAsync();
}
