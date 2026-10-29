// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Auditing;

/// <summary>
/// 审计包的错误码：<c>AUDITING_&lt;KIND&gt;_&lt;NNN&gt;</c>。
/// </summary>
public static class AuditingErrors
{
    /// <summary>
    /// 日志不存在（或不在当前一侧）。
    /// </summary>
    public const string LogNotFound = "AUDITING_NOTFOUND_001";

    /// <summary>
    /// 不支持的筛选或排序。
    /// </summary>
    public const string QueryUnsupported = "AUDITING_VALIDATION_001";

    /// <summary>
    /// 筛选值不合法（时间不是 ISO 8601、主键不是 UUID、枚举不认识）。
    /// </summary>
    public const string FilterValueInvalid = "AUDITING_VALIDATION_002";

    /// <summary>
    /// 链路查询不合法（维度值为空、起止颠倒）。
    /// </summary>
    public const string TraceQueryInvalid = "AUDITING_VALIDATION_003";

    /// <summary>
    /// 保留天数参数不合法。
    /// </summary>
    public const string RetentionInvalid = "AUDITING_VALIDATION_004";

    /// <summary>
    /// 时区不是认得的 IANA 名称；参数 <c>timeZone</c>。
    /// </summary>
    public const string TimeZoneInvalid = "AUDITING_VALIDATION_005";
}
