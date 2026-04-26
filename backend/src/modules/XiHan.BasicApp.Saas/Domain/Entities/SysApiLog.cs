#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysApiLog
// Guid:3d28152c-d6e9-4396-addb-b479254bad53
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 06:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统接口日志实体
/// 记录 API 层级的每次请求/响应详情（路径/方法/状态码/耗时/请求响应体摘要等）
/// </summary>
/// <remarks>
/// 分表策略：
/// - 按月分表（SplitTable.Month）
/// - 查询/清理必须带时间范围
///
/// 关联：
/// - UserId → SysUser；ClientId/AppId → SysOAuthApp；TraceId 串联同一请求
///
/// 写入：
/// - 只追加，由 API 中间件异步批量写入，避免影响业务性能
/// - 大请求/响应体建议截断（如 4KB）或只记 Hash，防止日志爆量
/// - StatusCode>=400 的请求建议提升采样率
///
/// 查询：
/// - 接口性能分析：IX_ExTi + ORDER BY ExecutionTime DESC（慢接口 Top-N）
/// - 按路径统计调用量：IX_ApPa
/// - 状态码分布：IX_StCo（4xx/5xx 排障）
/// - 按 TraceId 关联跨服务调用链
///
/// 删除：
/// - 不支持业务删除；按保留策略 TRUNCATE 月表
///
/// 场景：
/// - API 性能监控与 APM 集成
/// - 第三方调用量计费（按 ClientId/AppId 聚合）
/// - 异常请求追溯
/// </remarks>
[SugarTable("SysApiLog_{year}{month}{day}", "系统接口日志表"), SplitTable(SplitType.Month)]
[SugarIndex("IX_{split_table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_UsId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_ApPa", nameof(ApiPath), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_Me", nameof(Method), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_StCo", nameof(StatusCode), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_TeId_ReTi", nameof(TenantId), OrderByType.Asc, nameof(RequestTime), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_ExTi", nameof(ExecutionTime), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_TrId", nameof(TraceId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_ClId", nameof(ClientId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_ApId", nameof(AppId), OrderByType.Asc)]
public partial class SysApiLog : BasicAppCreationEntity, ISplitTableEntity, ITraceableEntity
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
    /// 会话ID
    /// </summary>
    [SugarColumn(ColumnDescription = "会话ID", Length = 100, IsNullable = true)]
    public virtual string? SessionId { get; set; }

    /// <summary>
    /// 请求ID
    /// </summary>
    [SugarColumn(ColumnDescription = "请求ID", Length = 100, IsNullable = true)]
    public virtual string? RequestId { get; set; }

    /// <summary>
    /// 链路追踪ID，用于串联整个请求生命周期
    /// </summary>
    [SugarColumn(ColumnDescription = "链路追踪ID", Length = 64, IsNullable = true)]
    public virtual string? TraceId { get; set; }

    /// <summary>
    /// 客户端标识，用于区分不同的 API 调用方
    /// </summary>
    [SugarColumn(ColumnDescription = "客户端标识", Length = 100, IsNullable = true)]
    public virtual string? ClientId { get; set; }

    /// <summary>
    /// 应用标识，用于区分不同的接入应用
    /// </summary>
    [SugarColumn(ColumnDescription = "应用标识", Length = 100, IsNullable = true)]
    public virtual string? AppId { get; set; }

    /// <summary>
    /// 签名是否有效
    /// </summary>
    [SugarColumn(ColumnDescription = "签名是否有效")]
    public virtual bool IsSignatureValid { get; set; } = true;

    /// <summary>
    /// 签名类型
    /// </summary>
    [SugarColumn(ColumnDescription = "签名类型")]
    public virtual SignatureType SignatureType { get; set; } = SignatureType.None;

    /// <summary>
    /// API路径
    /// </summary>
    [SugarColumn(ColumnDescription = "API路径", Length = 500, IsNullable = false)]
    public virtual string ApiPath { get; set; } = string.Empty;

    /// <summary>
    /// API名称
    /// </summary>
    [SugarColumn(ColumnDescription = "API名称", Length = 200, IsNullable = true)]
    public virtual string? ApiName { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    [SugarColumn(ColumnDescription = "请求方法", Length = 10, IsNullable = false)]
    public virtual string Method { get; set; } = string.Empty;

    /// <summary>
    /// 控制器名称
    /// </summary>
    [SugarColumn(ColumnDescription = "控制器名称", Length = 100, IsNullable = true)]
    public virtual string? ControllerName { get; set; }

    /// <summary>
    /// 操作名称
    /// </summary>
    [SugarColumn(ColumnDescription = "操作名称", Length = 100, IsNullable = true)]
    public virtual string? ActionName { get; set; }

    /// <summary>
    /// 请求参数
    /// </summary>
    [SugarColumn(ColumnDescription = "请求参数", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? RequestParams { get; set; }

    /// <summary>
    /// 请求体
    /// </summary>
    [SugarColumn(ColumnDescription = "请求体", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? RequestBody { get; set; }

    /// <summary>
    /// 响应结果
    /// </summary>
    [SugarColumn(ColumnDescription = "响应结果", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ResponseBody { get; set; }

    /// <summary>
    /// 响应状态码
    /// </summary>
    [SugarColumn(ColumnDescription = "响应状态码")]
    public virtual int StatusCode { get; set; } = 200;

    /// <summary>
    /// 请求头
    /// </summary>
    [SugarColumn(ColumnDescription = "请求头", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? RequestHeaders { get; set; }

    /// <summary>
    /// 响应头
    /// </summary>
    [SugarColumn(ColumnDescription = "响应头", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ResponseHeaders { get; set; }

    /// <summary>
    /// 请求IP
    /// </summary>
    [SugarColumn(ColumnDescription = "请求IP", Length = 50, IsNullable = true)]
    public virtual string? RequestIp { get; set; }

    /// <summary>
    /// 请求地址
    /// </summary>
    [SugarColumn(ColumnDescription = "请求地址", Length = 200, IsNullable = true)]
    public virtual string? RequestLocation { get; set; }

    /// <summary>
    /// User-Agent
    /// </summary>
    [SugarColumn(ColumnDescription = "User-Agent", Length = 500, IsNullable = true)]
    public virtual string? UserAgent { get; set; }

    /// <summary>
    /// 浏览器类型
    /// </summary>
    [SugarColumn(ColumnDescription = "浏览器类型", Length = 100, IsNullable = true)]
    public virtual string? Browser { get; set; }

    /// <summary>
    /// 请求来源
    /// </summary>
    [SugarColumn(ColumnDescription = "请求来源", Length = 500, IsNullable = true)]
    public virtual string? Referer { get; set; }

    /// <summary>
    /// 请求时间
    /// </summary>
    [SugarColumn(ColumnDescription = "请求时间")]
    public virtual DateTimeOffset RequestTime { get; set; }

    /// <summary>
    /// 响应时间
    /// </summary>
    [SugarColumn(ColumnDescription = "响应时间", IsNullable = true)]
    public virtual DateTimeOffset? ResponseTime { get; set; }

    /// <summary>
    /// 执行时长（毫秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "执行时长（毫秒）")]
    public virtual long ExecutionTime { get; set; } = 0;

    /// <summary>
    /// 请求大小（字节）
    /// </summary>
    [SugarColumn(ColumnDescription = "请求大小（字节）")]
    public virtual long RequestSize { get; set; } = 0;

    /// <summary>
    /// 响应大小（字节）
    /// </summary>
    [SugarColumn(ColumnDescription = "响应大小（字节）")]
    public virtual long ResponseSize { get; set; } = 0;

    /// <summary>
    /// 是否成功
    /// </summary>
    [SugarColumn(ColumnDescription = "是否成功")]
    public virtual bool IsSuccess { get; set; } = true;

    /// <summary>
    /// 错误信息
    /// </summary>
    [SugarColumn(ColumnDescription = "错误信息", Length = 2000, IsNullable = true)]
    public virtual string? ErrorMessage { get; set; }

    /// <summary>
    /// 异常堆栈
    /// </summary>
    [SugarColumn(ColumnDescription = "异常堆栈", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ExceptionStackTrace { get; set; }

    /// <summary>
    /// API版本
    /// </summary>
    [SugarColumn(ColumnDescription = "API版本", Length = 20, IsNullable = true)]
    public virtual string? ApiVersion { get; set; }

    /// <summary>
    /// 扩展数据（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "扩展数据", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ExtendData { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false, ColumnDescription = "创建时间")]
    [SplitField]
    public override DateTimeOffset CreatedTime { get; set; }
}
