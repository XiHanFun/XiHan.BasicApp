#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ICodeGenDataSourceRepository
// Guid:c0de9e00-0101-4a00-9000-000000000101
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
