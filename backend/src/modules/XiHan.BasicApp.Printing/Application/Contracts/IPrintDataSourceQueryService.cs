// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Printing.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Printing.Application.Contracts;

/// <summary>
/// 打印数据源目录查询服务契约。
/// </summary>
public interface IPrintDataSourceQueryService : IApplicationService
{
    /// <summary>
    /// 全部已注册的打印数据源（含字段清单与样例数据）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>数据源目录。</returns>
    Task<List<PrintDataSourceDto>> GetListAsync(CancellationToken cancellationToken = default);
}
