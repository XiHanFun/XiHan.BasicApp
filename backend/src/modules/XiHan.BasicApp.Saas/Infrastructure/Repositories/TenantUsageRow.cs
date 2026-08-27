// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 租户用量分组统计行
/// </summary>
/// <remarks>
/// 仅用于承载 SqlSugar 的 GROUP BY 聚合投影结果（一次查询拿回多个租户的用量），不对外暴露。
/// SqlSugar 要求投影目标为具名类型且具备无参构造，故不使用匿名类型或元组。
/// </remarks>
internal sealed class TenantUsageRow
{
    /// <summary>
    /// 租户主键
    /// </summary>
    public long TenantId { get; set; }

    /// <summary>
    /// 聚合值（成员数或已占用字节数）
    /// </summary>
    public long Value { get; set; }
}
