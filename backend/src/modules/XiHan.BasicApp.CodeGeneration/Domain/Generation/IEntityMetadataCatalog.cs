// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 已注册实体元数据目录（反射扫描 XiHan* 程序集中带 [SugarTable] 的实体，进程内缓存）
/// </summary>
/// <remarks>
/// 两类用途：
/// <list type="number">
/// <item>名称还原：把 DB 返回的全小写表名/列名还原为实体上的真实大小写，并把分表分片折叠为逻辑名。</item>
/// <item>推断杠杆：对项目内的表直接取出实体 <see cref="Type"/>，据此推断类名/命名空间/模块/基类/枚举，
/// 是"输入最小化"对本系统表能做到近乎零配置的关键。外部库的表不在目录中，走名称约定推断。</item>
/// </list>
/// </remarks>
public interface IEntityMetadataCatalog
{
    /// <summary>
    /// 还原表名真实大小写（未注册则原样返回）
    /// </summary>
    string ResolveTable(string dbTableName);

    /// <summary>
    /// 还原为逻辑表名：精确匹配优先；分表分片折叠为基础逻辑名；否则原样
    /// </summary>
    string ResolveLogical(string dbTableName);

    /// <summary>
    /// 还原列名真实大小写（未注册则原样返回）
    /// </summary>
    string ResolveColumn(string realTableName, string dbColumnName);

    /// <summary>
    /// 是否为分表基础名
    /// </summary>
    bool IsSplitBase(string tableName);

    /// <summary>
    /// 是否为某分表实体的物理分片（如 sysdifflog_20260601 → SysDiffLog）
    /// </summary>
    bool TryResolveSplitShard(string dbTableName, out string baseRealName);

    /// <summary>
    /// 取表对应的已注册实体类型（外部库的表返回 false）
    /// </summary>
    /// <param name="tableName">表名（大小写不敏感；逻辑名或物理分片名均可）</param>
    /// <param name="entityType">命中的实体类型</param>
    bool TryGetEntityType(string tableName, out Type entityType);
}
