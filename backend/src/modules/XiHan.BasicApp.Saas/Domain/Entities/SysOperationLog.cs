#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOperationLog
// Guid:7c81aea1-5298-46e8-b83b-7bcee87b103e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 05:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统操作日志实体
/// </summary>
[SugarTable("SysOperationLog_{year}{month}{day}", "系统操作日志表"), SplitTable(SplitType.Month)]
[SugarIndex("IX_{split_table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_UsId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_OpTy", nameof(OperationType), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_TeId_OpTi", nameof(TenantId), OrderByType.Asc, nameof(OperationTime), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_St", nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_TrId", nameof(TraceId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_SeId", nameof(SessionId), OrderByType.Asc)]
public partial class SysOperationLog : BasicAppCreationEntity, ITraceableEntity
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = true)]
    public virtual long? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [SugarColumn(ColumnDescription = "用户名", Length = 50, IsNullable = true)]
    public virtual string? UserName { get; set; }

    /// <summary>
    /// 链路追踪ID，用于串联整个请求生命周期
    /// </summary>
    [SugarColumn(ColumnDescription = "链路追踪ID", Length = 64, IsNullable = true)]
    public virtual string? TraceId { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    [SugarColumn(ColumnDescription = "会话ID", Length = 100, IsNullable = true)]
    public virtual string? SessionId { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    [SugarColumn(ColumnDescription = "操作类型")]
    public virtual OperationType OperationType { get; set; } = OperationType.Other;

    /// <summary>
    /// 操作模块
    /// </summary>
    [SugarColumn(ColumnDescription = "操作模块", Length = 50, IsNullable = true)]
    public virtual string? Module { get; set; }

    /// <summary>
    /// 操作功能
    /// </summary>
    [SugarColumn(ColumnDescription = "操作功能", Length = 50, IsNullable = true)]
    public virtual string? Function { get; set; }

    /// <summary>
    /// 操作标题
    /// </summary>
    [SugarColumn(ColumnDescription = "操作标题", Length = 200, IsNullable = true)]
    public virtual string? Title { get; set; }

    /// <summary>
    /// 操作描述
    /// </summary>
    [SugarColumn(ColumnDescription = "操作描述", Length = 500, IsNullable = true)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    [SugarColumn(ColumnDescription = "请求方法", Length = 10, IsNullable = true)]
    public virtual string? Method { get; set; }

    /// <summary>
    /// 请求URL
    /// </summary>
    [SugarColumn(ColumnDescription = "请求URL", Length = 500, IsNullable = true)]
    public virtual string? RequestUrl { get; set; }

    /// <summary>
    /// 请求参数
    /// </summary>
    [SugarColumn(ColumnDescription = "请求参数", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? RequestParams { get; set; }

    /// <summary>
    /// 响应结果
    /// </summary>
    [SugarColumn(ColumnDescription = "响应结果", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ResponseResult { get; set; }

    /// <summary>
    /// 执行时间（毫秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "执行时间（毫秒）")]
    public virtual long ExecutionTime { get; set; } = 0;

    /// <summary>
    /// 操作IP
    /// </summary>
    [SugarColumn(ColumnDescription = "操作IP", Length = 50, IsNullable = true)]
    public virtual string? OperationIp { get; set; }

    /// <summary>
    /// 操作地址
    /// </summary>
    [SugarColumn(ColumnDescription = "操作地址", Length = 200, IsNullable = true)]
    public virtual string? OperationLocation { get; set; }

    /// <summary>
    /// 浏览器类型
    /// </summary>
    [SugarColumn(ColumnDescription = "浏览器类型", Length = 100, IsNullable = true)]
    public virtual string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    [SugarColumn(ColumnDescription = "操作系统", Length = 100, IsNullable = true)]
    public virtual string? Os { get; set; }

    /// <summary>
    /// User-Agent
    /// </summary>
    [SugarColumn(ColumnDescription = "User-Agent", Length = 500, IsNullable = true)]
    public virtual string? UserAgent { get; set; }

    /// <summary>
    /// 操作状态
    /// </summary>
    [SugarColumn(ColumnDescription = "操作状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 错误消息
    /// </summary>
    [SugarColumn(ColumnDescription = "错误消息", Length = 1000, IsNullable = true)]
    public virtual string? ErrorMessage { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    [SugarColumn(ColumnDescription = "操作时间")]
    public virtual DateTimeOffset OperationTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false, ColumnDescription = "创建时间")]
    [SplitField]
    public override DateTimeOffset CreatedTime { get; set; }
}
