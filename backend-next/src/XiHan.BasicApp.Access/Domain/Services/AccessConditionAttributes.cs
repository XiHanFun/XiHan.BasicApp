// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 条件属性目录（权限条件与访问策略共用，方案 5.9）：4.x 的主体、资源、环境三族，名称不区分大小写，旧写法登记为别名。
/// 6.0 新增 <c>resource.operation</c>（操作 ID）替代 4.x 的 <c>resource.service</c> / <c>resource.action</c>；
/// 不登记路由值与查询串两族（调用来源不带它们）、开放接口的应用 ID（等 OpenPlatform）、成员类型（Access 不引用 Tenancy）、旧写法 <c>environment.location</c> 与数据行属性。
/// </summary>
public static class AccessConditionAttributes
{
    /// <summary>
    /// 用户 ID。
    /// </summary>
    public const string SubjectUserId = "subject.userId";

    /// <summary>
    /// 登录名。
    /// </summary>
    public const string SubjectUserName = "subject.userName";

    /// <summary>
    /// 所在租户（平台侧为 <c>platform</c>）。
    /// </summary>
    public const string SubjectTenantCode = "subject.tenantCode";

    /// <summary>
    /// 此刻持有的角色码（多值）。
    /// </summary>
    public const string SubjectRoleCodes = "subject.roleCodes";

    /// <summary>
    /// 所在部门码（多值，租户侧）。
    /// </summary>
    public const string SubjectDepartmentCodes = "subject.departmentCodes";

    /// <summary>
    /// 主部门码（租户侧）。
    /// </summary>
    public const string SubjectMainDepartmentCode = "subject.mainDepartmentCode";

    /// <summary>
    /// 岗位码（多值，租户侧）。
    /// </summary>
    public const string SubjectPositionCodes = "subject.positionCodes";

    /// <summary>
    /// 是否模仿登录。
    /// </summary>
    public const string SubjectIsImpersonating = "subject.isImpersonating";

    /// <summary>
    /// 本次调用要求的权限码（多值）。
    /// </summary>
    public const string ResourcePermissionCodes = "resource.permissionCodes";

    /// <summary>
    /// 请求路径。
    /// </summary>
    public const string ResourcePath = "resource.path";

    /// <summary>
    /// 请求方法。
    /// </summary>
    public const string ResourceMethod = "resource.method";

    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string ResourceOperation = "resource.operation";

    /// <summary>
    /// 客户端地址。
    /// </summary>
    public const string EnvironmentIp = "environment.ip";

    /// <summary>
    /// 归属地国家。
    /// </summary>
    public const string EnvironmentGeoCountry = "environment.geo.country";

    /// <summary>
    /// 归属地省份。
    /// </summary>
    public const string EnvironmentGeoProvince = "environment.geo.province";

    /// <summary>
    /// 归属地城市。
    /// </summary>
    public const string EnvironmentGeoCity = "environment.geo.city";

    /// <summary>
    /// 归属地运营商。
    /// </summary>
    public const string EnvironmentGeoIsp = "environment.geo.isp";

    /// <summary>
    /// 当地时刻。
    /// </summary>
    public const string EnvironmentTime = "environment.time";

    /// <summary>
    /// 当地小时（0 到 23）。
    /// </summary>
    public const string EnvironmentHour = "environment.hour";

    /// <summary>
    /// 当地星期（周一为 1）。
    /// </summary>
    public const string EnvironmentWeekday = "environment.weekday";

    /// <summary>
    /// 当地日期。
    /// </summary>
    public const string EnvironmentDate = "environment.date";

    /// <summary>
    /// 客户端 User-Agent。
    /// </summary>
    public const string EnvironmentUserAgent = "environment.client.userAgent";

    /// <summary>
    /// 客户端类型：未登录为 <c>anonymous</c>，否则为调用协议（<c>http</c>、<c>grpc</c>、<c>mcp</c> 等）。
    /// </summary>
    public const string EnvironmentClientKind = "environment.client.kind";

    /// <summary>
    /// 认证前就取得到的属性（访问策略的认证前时机只能用它们）。
    /// </summary>
    public static IReadOnlySet<string> BeforeAuthentication { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        EnvironmentIp, EnvironmentGeoCountry, EnvironmentGeoProvince, EnvironmentGeoCity, EnvironmentGeoIsp,
        EnvironmentTime, EnvironmentHour, EnvironmentWeekday, EnvironmentDate, EnvironmentUserAgent, ResourcePath, ResourceMethod,
    };

    /// <summary>
    /// 全部登记项。
    /// </summary>
    public static IReadOnlyList<ConditionAttributeDescriptor> All { get; } =
    [
        new(SubjectUserId, ConditionValueKind.String),
        new(SubjectUserName, ConditionValueKind.String),
        new(SubjectTenantCode, ConditionValueKind.String),
        new(SubjectRoleCodes, ConditionValueKind.String, IsMultiValued: true, Aliases: ["subject.role"]),
        new(SubjectDepartmentCodes, ConditionValueKind.String, IsMultiValued: true, Aliases: ["subject.department"]),
        new(SubjectMainDepartmentCode, ConditionValueKind.String),
        new(SubjectPositionCodes, ConditionValueKind.String, IsMultiValued: true),
        new(SubjectIsImpersonating, ConditionValueKind.Boolean),
        new(ResourcePermissionCodes, ConditionValueKind.String, IsMultiValued: true, Aliases: ["resource.permissionCode"]),
        new(ResourcePath, ConditionValueKind.String),
        new(ResourceMethod, ConditionValueKind.String),
        new(ResourceOperation, ConditionValueKind.String),
        new(EnvironmentIp, ConditionValueKind.IpAddress),
        new(EnvironmentGeoCountry, ConditionValueKind.String),
        new(EnvironmentGeoProvince, ConditionValueKind.String),
        new(EnvironmentGeoCity, ConditionValueKind.String),
        new(EnvironmentGeoIsp, ConditionValueKind.String),
        new(EnvironmentTime, ConditionValueKind.TimeOfDay),
        new(EnvironmentHour, ConditionValueKind.Number),
        new(EnvironmentWeekday, ConditionValueKind.Number),
        new(EnvironmentDate, ConditionValueKind.Date),
        new(EnvironmentUserAgent, ConditionValueKind.String),
        new(EnvironmentClientKind, ConditionValueKind.String),
    ];
}
