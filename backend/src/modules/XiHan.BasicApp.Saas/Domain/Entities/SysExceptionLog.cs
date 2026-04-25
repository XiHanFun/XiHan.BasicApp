#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysExceptionLog
// Guid:7e28152c-d6e9-4396-addb-b479254bad77
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统异常日志实体
/// 集中记录应用运行时异常（堆栈/请求上下文/严重级别），是技术排障与稳定性改进的数据源
/// </summary>
/// <remarks>
/// 分表策略：
/// - 按月分表；查询/清理必带时间范围
///
/// 关联：
/// - UserId → SysUser（可空，匿名请求时无）；TraceId 串联请求链
///
/// 写入：
/// - 由全局异常过滤器/中间件统一写入，业务代码不应手工写
/// - 堆栈 StackTrace 可较长，建议限制总长度（如 32KB）或截断
/// - IsHandled=false 表示未被代码捕获/处理；IsHandled=true 表示已妥善处理仅记录
/// - SeverityLevel 由异常类型映射（Warn/Error/Critical）
///
/// 查询：
/// - 按严重级别排序：IX_SeLe + ORDER BY SeverityLevel DESC
/// - 未处理异常清单：IX_IsHa + WHERE IsHandled=false
/// - 按业务模块统计：IX_BuMo
/// - 按类型聚类：IX_ExTy
///
/// 删除：
/// - 不支持业务删除；按保留策略清理
///
/// 场景：
/// - 生产环境问题排查
/// - 异常趋势分析 → 稳定性改进
/// - 关联 Sentry/AppInsights 告警
/// </remarks>
[SugarTable("SysExceptionLog_{year}{month}{day}", "系统异常日志表"), SplitTable(SplitType.Month)]
[SugarIndex("IX_{split_table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_UsId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_ExTy", nameof(ExceptionType), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_SeLe", nameof(SeverityLevel), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_IsHa", nameof(IsHandled), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_StCo", nameof(StatusCode), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_TrId", nameof(TraceId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_TeId_ExTi", nameof(TenantId), OrderByType.Asc, nameof(ExceptionTime), OrderByType.Desc)]
public partial class SysExceptionLog : BasicAppCreationEntity, ISplitTableEntity, ITraceableEntity
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
    /// 异常类型
    /// </summary>
    [SugarColumn(ColumnDescription = "异常类型", Length = 200, IsNullable = false)]
    public virtual string ExceptionType { get; set; } = string.Empty;

    /// <summary>
    /// 异常消息
    /// </summary>
    [SugarColumn(ColumnDescription = "异常消息", Length = 2000, IsNullable = false)]
    public virtual string ExceptionMessage { get; set; } = string.Empty;

    /// <summary>
    /// 异常堆栈（含内部异常堆栈）
    /// </summary>
    [SugarColumn(ColumnDescription = "异常堆栈", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ExceptionStackTrace { get; set; }

    /// <summary>
    /// 异常源
    /// </summary>
    [SugarColumn(ColumnDescription = "异常源", Length = 200, IsNullable = true)]
    public virtual string? ExceptionSource { get; set; }

    /// <summary>
    /// 异常发生位置（类名.方法名）
    /// </summary>
    [SugarColumn(ColumnDescription = "异常发生位置", Length = 300, IsNullable = true)]
    public virtual string? ExceptionLocation { get; set; }

    /// <summary>
    /// 严重级别（1-5，数字越大越严重）
    /// </summary>
    [SugarColumn(ColumnDescription = "严重级别")]
    public virtual int SeverityLevel { get; set; } = 3;

    /// <summary>
    /// 请求路径
    /// </summary>
    [SugarColumn(ColumnDescription = "请求路径", Length = 500, IsNullable = true)]
    public virtual string? RequestPath { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    [SugarColumn(ColumnDescription = "请求方法", Length = 10, IsNullable = true)]
    public virtual string? RequestMethod { get; set; }

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
    /// 请求参数（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "请求参数", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? RequestParams { get; set; }

    /// <summary>
    /// 请求体（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "请求体", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? RequestBody { get; set; }

    /// <summary>
    /// 请求头（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "请求头", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? RequestHeaders { get; set; }

    /// <summary>
    /// 响应状态码
    /// </summary>
    [SugarColumn(ColumnDescription = "响应状态码")]
    public virtual int StatusCode { get; set; } = 500;

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
    /// 操作系统
    /// </summary>
    [SugarColumn(ColumnDescription = "操作系统", Length = 100, IsNullable = true)]
    public virtual string? Os { get; set; }

    /// <summary>
    /// 设备类型
    /// </summary>
    [SugarColumn(ColumnDescription = "设备类型")]
    public virtual DeviceType DeviceType { get; set; } = DeviceType.Unknown;

    /// <summary>
    /// 设备信息
    /// </summary>
    [SugarColumn(ColumnDescription = "设备信息", Length = 200, IsNullable = true)]
    public virtual string? DeviceInfo { get; set; }

    /// <summary>
    /// 应用程序名称
    /// </summary>
    [SugarColumn(ColumnDescription = "应用程序名称", Length = 100, IsNullable = true)]
    public virtual string? ApplicationName { get; set; }

    /// <summary>
    /// 应用程序版本
    /// </summary>
    [SugarColumn(ColumnDescription = "应用程序版本", Length = 50, IsNullable = true)]
    public virtual string? ApplicationVersion { get; set; }

    /// <summary>
    /// 环境名称（Development/Staging/Production）
    /// </summary>
    [SugarColumn(ColumnDescription = "环境名称", Length = 50, IsNullable = true)]
    public virtual string? EnvironmentName { get; set; }

    /// <summary>
    /// 服务器主机名
    /// </summary>
    [SugarColumn(ColumnDescription = "服务器主机名", Length = 100, IsNullable = true)]
    public virtual string? ServerHostName { get; set; }

    /// <summary>
    /// 线程ID
    /// </summary>
    [SugarColumn(ColumnDescription = "线程ID")]
    public virtual int ThreadId { get; set; } = 0;

    /// <summary>
    /// 进程ID
    /// </summary>
    [SugarColumn(ColumnDescription = "进程ID")]
    public virtual int ProcessId { get; set; } = 0;

    /// <summary>
    /// 异常时间
    /// </summary>
    [SugarColumn(ColumnDescription = "异常时间")]
    public virtual DateTimeOffset ExceptionTime { get; set; }

    /// <summary>
    /// 是否已处理
    /// </summary>
    [SugarColumn(ColumnDescription = "是否已处理")]
    public virtual bool IsHandled { get; set; } = false;

    /// <summary>
    /// 处理时间
    /// </summary>
    [SugarColumn(ColumnDescription = "处理时间", IsNullable = true)]
    public virtual DateTimeOffset? HandledTime { get; set; }

    /// <summary>
    /// 处理人ID
    /// </summary>
    [SugarColumn(ColumnDescription = "处理人ID", IsNullable = true)]
    public virtual long? HandledBy { get; set; }

    /// <summary>
    /// 处理备注
    /// </summary>
    [SugarColumn(ColumnDescription = "处理备注", Length = 1000, IsNullable = true)]
    public virtual string? HandledRemark { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    [SugarColumn(ColumnDescription = "错误代码", Length = 50, IsNullable = true)]
    public virtual string? ErrorCode { get; set; }

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
