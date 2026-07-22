// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.CodeGeneration.Domain.Repositories;

/// <summary>
/// 代码生成表配置仓储接口
/// </summary>
public interface ICodeGenTableRepository : ISaasRepository<SysCodeGenTable>
{
    /// <summary>
    /// 根据数据库表名获取
    /// </summary>
    Task<SysCodeGenTable?> GetByTableNameAsync(string tableName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查表名是否已配置
    /// </summary>
    Task<bool> ExistsTableNameAsync(string tableName, long? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按模块获取表配置
    /// </summary>
    Task<IReadOnlyList<SysCodeGenTable>> GetByModuleAsync(string moduleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取以指定表为主表的子表配置（主子表生成时反查明细表）
    /// </summary>
    Task<IReadOnlyList<SysCodeGenTable>> GetByMasterTableIdAsync(long masterTableId, CancellationToken cancellationToken = default);
}
