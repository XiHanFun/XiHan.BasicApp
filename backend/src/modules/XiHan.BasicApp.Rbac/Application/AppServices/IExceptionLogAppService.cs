#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IExceptionLogAppService
// Guid:a1b2c3d4-0002-0003-0001-000000000001
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/10 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Application.AppServices;

/// <summary>
/// 异常日志应用服务接口
/// </summary>
public interface IExceptionLogAppService : IApplicationService
{
    /// <summary>
    /// 分页查询
    /// </summary>
    Task<PageResultDtoBase<ExceptionLogDto>> PageAsync(BasicAppPRDto input);

    /// <summary>
    /// 清空日志
    /// </summary>
    Task<bool> ClearAsync();
}
