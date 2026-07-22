// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.CodeGeneration.Domain.Repositories;

/// <summary>
/// 代码生成数据源仓储接口
/// </summary>
public interface ICodeGenDataSourceRepository : ISaasRepository<SysCodeGenDataSource>
{
    /// <summary>
    /// 根据数据源名称获取
    /// </summary>
    Task<SysCodeGenDataSource?> GetByNameAsync(string sourceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查数据源名称是否存在
    /// </summary>
    Task<bool> ExistsNameAsync(string sourceName, long? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取默认数据源
    /// </summary>
    Task<SysCodeGenDataSource?> GetDefaultAsync(CancellationToken cancellationToken = default);
}
