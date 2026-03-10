#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ExceptionLogAppService
// Guid:a1b2c3d4-0003-0003-0001-000000000001
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/10 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.SplitTables;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Application.AppServices.Implementations;

/// <summary>
/// 异常日志应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Rbac", GroupName = "系统Rbac服务")]
public class ExceptionLogAppService : ApplicationServiceBase, IExceptionLogAppService
{
    private readonly ISqlSugarDbContext _dbContext;
    private readonly ISqlSugarSplitTableExecutor _splitTableExecutor;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExceptionLogAppService(ISqlSugarDbContext dbContext, ISqlSugarSplitTableExecutor splitTableExecutor)
    {
        _dbContext = dbContext;
        _splitTableExecutor = splitTableExecutor;
    }

    private ISqlSugarClient DbClient => _dbContext.GetClient();

    /// <summary>
    /// 分页查询
    /// </summary>
    [HttpPost]
    public async Task<PageResultDtoBase<ExceptionLogDto>> PageAsync(BasicAppPRDto input)
    {
        var pageIndex = input.Page.PageIndex;
        var pageSize = input.Page.PageSize;
        RefAsync<int> total = 0;

        var list = await _splitTableExecutor.CreateQueryable<SysExceptionLog>(DbClient)
            .OrderByDescending(static x => x.CreatedTime)
            .ToPageListAsync(pageIndex, pageSize, total);

        var dtos = list.Adapt<List<ExceptionLogDto>>();
        return PageResultDtoBase<ExceptionLogDto>.Create(dtos, pageIndex, pageSize, total);
    }

    /// <summary>
    /// 清空日志
    /// </summary>
    [HttpDelete]
    public async Task<bool> ClearAsync()
    {
        return await DbClient.Deleteable<SysExceptionLog>().SplitTable().ExecuteCommandAsync() > 0;
    }
}
