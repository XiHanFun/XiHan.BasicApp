#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuditLogAppService
// Guid:a1b2c3d4-0003-0004-0001-000000000001
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/10 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 审计日志应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
public class AuditLogAppService(IAuditLogSplitRepository repository) : ApplicationServiceBase, IAuditLogAppService
{
    /// <summary>
    /// 分页查询
    /// </summary>
    [HttpPost]
    public async Task<PageResultDtoBase<AuditLogDto>> PageAsync(BasicAppPRDto input)
    {
        var result = await repository.ScanPagedAsync(input.Page.PageIndex, input.Page.PageSize);
        var dtos = result.Items.Adapt<List<AuditLogDto>>();
        return PageResultDtoBase<AuditLogDto>.Create(dtos, input.Page.PageIndex, input.Page.PageSize, result.TotalCount);
    }

    /// <summary>
    /// 清空日志
    /// </summary>
    [HttpDelete]
    public async Task<bool> ClearAsync()
    {
        return await repository.ClearAllAsync();
    }
}
