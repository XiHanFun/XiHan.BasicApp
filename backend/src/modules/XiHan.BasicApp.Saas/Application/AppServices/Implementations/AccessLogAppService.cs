#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AccessLogAppService
// Guid:a1b2c3d4-0003-0001-0001-000000000001
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
/// 访问日志应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
public class AccessLogAppService(IAccessLogSplitRepository repository) : ApplicationServiceBase, IAccessLogAppService
{
    /// <summary>
    /// 分页查询
    /// </summary>
    [HttpPost]
    public async Task<PageResultDtoBase<AccessLogDto>> PageAsync(BasicAppPRDto input)
    {
        var pageIndex = input.Page.PageIndex;
        var pageSize = input.Page.PageSize;
        var totalCount = await repository.ScanCountAsync();
        var items = await repository.ScanAllAsync();
        var pagedItems = items
            .OrderByDescending(log => log.CreatedTime)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        var dtos = pagedItems.Adapt<List<AccessLogDto>>() ?? [];
        return PageResultDtoBase<AccessLogDto>.Create(dtos, pageIndex, pageSize, (int)totalCount);
    }

    /// <summary>
    /// 清空日志
    /// </summary>
    [HttpDelete]
    public async Task<bool> ClearAsync()
    {
        var items = await repository.ScanAllAsync();
        foreach (var item in items)
        {
            await repository.DeleteByIdAsync(item.BasicId);
        }

        return true;
    }
}
