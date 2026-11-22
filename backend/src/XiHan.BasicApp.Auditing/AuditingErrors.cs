// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Auditing;

/// <summary>
/// 审计包的错误码
/// </summary>
/// <remarks>
/// 与框架审计的错误码同一个前缀 <c>AUDITING_</c>：框架占 001 段，BasicApp 自己的各类从 101 起编（照 Workflow 的做法），前端按码就分得清是谁报的
/// </remarks>
public static class AuditingErrors
{
    /// <summary>
    /// 日志不存在（或不在当前一侧）
    /// </summary>
    public const string LogNotFound = "AUDITING_NOTFOUND_101";

    /// <summary>
    /// 不支持的筛选或排序
    /// </summary>
    public const string QueryUnsupported = "AUDITING_VALIDATION_101";

    /// <summary>
    /// 筛选值不合法（时间不是 ISO 8601、主键不是 UUID、枚举不认识）
    /// </summary>
    public const string FilterValueInvalid = "AUDITING_VALIDATION_102";

    /// <summary>
    /// 链路查询不合法（维度值为空、起止颠倒）
    /// </summary>
    public const string TraceQueryInvalid = "AUDITING_VALIDATION_103";

    /// <summary>
    /// 保留天数参数不合法
    /// </summary>
    public const string RetentionInvalid = "AUDITING_VALIDATION_104";

    /// <summary>
    /// 时区不是认得的 IANA 名称
    /// </summary>
    /// <remarks>
    /// 参数 <c>timeZone</c>
    /// </remarks>
    public const string TimeZoneInvalid = "AUDITING_VALIDATION_105";
}
