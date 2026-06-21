#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDatabaseSchemaImporter
// Guid:c0de9e00-0005-4a00-9000-000000000005
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 数据库结构导入器：基于 DbFirst 元数据读取目标表/列结构（复用框架 IDatabaseMetadataProvider）
/// </summary>
public interface IDatabaseSchemaImporter
{
    /// <summary>
    /// 列出连接下所有表名
    /// </summary>
    /// <param name="connectionConfigId">连接配置标识（对应 SysCodeGenDataSource，为空表示主库）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表名列表</returns>
    Task<IReadOnlyList<string>> ListTablesAsync(string? connectionConfigId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 导入单表结构
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="connectionConfigId">连接配置标识，为空表示主库</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表结构；表不存在返回 null</returns>
    Task<TableSchema?> ImportTableAsync(string tableName, string? connectionConfigId = null, CancellationToken cancellationToken = default);
}
