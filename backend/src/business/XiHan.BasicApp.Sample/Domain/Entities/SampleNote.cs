// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Sample.Domain.Entities;

/// <summary>
/// 示例便签实体：未声明数据源，落在主库。
/// </summary>
/// <remarks>
/// 这是绝大多数业务实体的写法——继承 <see cref="BasicAppFullAuditedEntity"/> 即自带
/// 主键、租户标识、创建/修改/删除审计与行版本，不需要自己声明这些列。
/// </remarks>
[SugarTable(TableName = "Sample_Note", TableDescription = "示例便签表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
public class SampleNote : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 便签标题
    /// </summary>
    [SugarColumn(ColumnName = "Title", ColumnDescription = "便签标题", Length = 100, IsNullable = false)]
    public virtual string Title { get; set; } = string.Empty;

    /// <summary>
    /// 便签内容
    /// </summary>
    [SugarColumn(ColumnName = "Content", ColumnDescription = "便签内容", Length = 500, IsNullable = true)]
    public virtual string? Content { get; set; }
}
