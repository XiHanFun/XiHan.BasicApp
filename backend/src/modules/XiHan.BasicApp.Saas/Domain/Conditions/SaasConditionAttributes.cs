// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Authorization.Conditions;

namespace XiHan.BasicApp.Saas.Domain.Conditions;

/// <summary>
/// 条件属性目录：权限条件与访问策略共用的属性名与登记项
/// </summary>
/// <remarks>
/// 属性名带 <c>subject.</c> / <c>resource.</c> / <c>environment.</c> 前缀，大小写不敏感。
/// 旧写法 <c>subject.role</c>、<c>subject.department</c>、<c>resource.permissionCode</c> 登记为别名；
/// 数据行属性（如 <c>resource.status</c>）不在这里，由业务模块按权限码另行登记。
/// </remarks>
public static class SaasConditionAttributes
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public const string SubjectUserId = "subject.userId";

    /// <summary>
    /// 用户名
    /// </summary>
    public const string SubjectUserName = "subject.userName";

    /// <summary>
    /// 租户编码，平台上下文取不到
    /// </summary>
    public const string SubjectTenantCode = "subject.tenantCode";

    /// <summary>
    /// 当前上下文里有效的角色编码（多值）
    /// </summary>
    public const string SubjectRoleCodes = "subject.roleCodes";

    /// <summary>
    /// 有效任职所在的部门编码（多值，含主部门）
    /// </summary>
    public const string SubjectDepartmentCodes = "subject.departmentCodes";

    /// <summary>
    /// 主部门编码
    /// </summary>
    public const string SubjectMainDepartmentCode = "subject.mainDepartmentCode";

    /// <summary>
    /// 有效任职上的岗位编码（多值）
    /// </summary>
    public const string SubjectPositionCodes = "subject.positionCodes";

    /// <summary>
    /// 租户成员类型（Owner、Admin、Member、External、Guest、Consultant、PlatformAdmin）
    /// </summary>
    public const string SubjectMemberType = "subject.memberType";

    /// <summary>
    /// 是否处于模仿登录
    /// </summary>
    public const string SubjectIsImpersonating = "subject.isImpersonating";

    /// <summary>
    /// 当前端点上声明的权限码（多值）
    /// </summary>
    public const string ResourcePermissionCodes = "resource.permissionCodes";

    /// <summary>
    /// 请求路径，不含查询串
    /// </summary>
    public const string ResourcePath = "resource.path";

    /// <summary>
    /// HTTP 方法
    /// </summary>
    public const string ResourceMethod = "resource.method";

    /// <summary>
    /// 动态 API 服务名
    /// </summary>
    public const string ResourceService = "resource.service";

    /// <summary>
    /// 动态 API 方法名
    /// </summary>
    public const string ResourceAction = "resource.action";

    /// <summary>
    /// 路由值一族，如 <c>resource.route.id</c>
    /// </summary>
    public const string ResourceRouteFamily = "resource.route.*";

    /// <summary>
    /// 查询串一族，如 <c>resource.query.status</c>；同名多值只取第一个
    /// </summary>
    public const string ResourceQueryFamily = "resource.query.*";

    /// <summary>
    /// 客户端 IP
    /// </summary>
    public const string EnvironmentIp = "environment.ip";

    /// <summary>
    /// IP 归属国家
    /// </summary>
    public const string EnvironmentGeoCountry = "environment.geo.country";

    /// <summary>
    /// IP 归属省份
    /// </summary>
    public const string EnvironmentGeoProvince = "environment.geo.province";

    /// <summary>
    /// IP 归属城市
    /// </summary>
    public const string EnvironmentGeoCity = "environment.geo.city";

    /// <summary>
    /// IP 归属运营商
    /// </summary>
    public const string EnvironmentGeoIsp = "environment.geo.isp";

    /// <summary>
    /// 旧写法：国家、省份、城市依次拼成的一个字符串，如「中国广东省深圳市」
    /// </summary>
    public const string EnvironmentLocation = "environment.location";

    /// <summary>
    /// 当前时刻（时分）
    /// </summary>
    public const string EnvironmentTime = "environment.time";

    /// <summary>
    /// 当前小时 0–23
    /// </summary>
    public const string EnvironmentHour = "environment.hour";

    /// <summary>
    /// 星期几 1–7，周一为 1
    /// </summary>
    public const string EnvironmentWeekday = "environment.weekday";

    /// <summary>
    /// 当前日期
    /// </summary>
    public const string EnvironmentDate = "environment.date";

    /// <summary>
    /// User-Agent 请求头
    /// </summary>
    public const string EnvironmentClientUserAgent = "environment.client.userAgent";

    /// <summary>
    /// 客户端类型，取值见 <see cref="SaasConditionClientKinds"/>
    /// </summary>
    public const string EnvironmentClientKind = "environment.client.kind";

    /// <summary>
    /// 开放接口应用的 AccessKey 或 OAuth 应用的 client_id
    /// </summary>
    public const string EnvironmentClientAppId = "environment.client.appId";

    /// <summary>
    /// 全部登记项
    /// </summary>
    public static IReadOnlyList<ConditionAttributeDescriptor> All { get; } =
    [
        new(SubjectUserId, ConditionValueKind.String, DisplayName: "用户主键"),
        new(SubjectUserName, ConditionValueKind.String, DisplayName: "用户名"),
        new(SubjectTenantCode, ConditionValueKind.String, DisplayName: "租户编码"),
        new(SubjectRoleCodes, ConditionValueKind.String, IsMultiValued: true, DisplayName: "角色编码", Aliases: ["subject.role"]),
        new(SubjectDepartmentCodes, ConditionValueKind.String, IsMultiValued: true, DisplayName: "部门编码", Aliases: ["subject.department"]),
        new(SubjectMainDepartmentCode, ConditionValueKind.String, DisplayName: "主部门编码"),
        new(SubjectPositionCodes, ConditionValueKind.String, IsMultiValued: true, DisplayName: "岗位编码"),
        new(SubjectMemberType, ConditionValueKind.String, DisplayName: "成员类型"),
        new(SubjectIsImpersonating, ConditionValueKind.Boolean, DisplayName: "模仿登录"),
        new(ResourcePermissionCodes, ConditionValueKind.String, IsMultiValued: true, DisplayName: "权限码", Aliases: ["resource.permissionCode"]),
        new(ResourcePath, ConditionValueKind.String, DisplayName: "请求路径"),
        new(ResourceMethod, ConditionValueKind.String, DisplayName: "请求方法"),
        new(ResourceService, ConditionValueKind.String, DisplayName: "接口服务"),
        new(ResourceAction, ConditionValueKind.String, DisplayName: "接口方法"),
        new(ResourceRouteFamily, ConditionValueKind.String, DisplayName: "路由值"),
        new(ResourceQueryFamily, ConditionValueKind.String, DisplayName: "查询参数"),
        new(EnvironmentIp, ConditionValueKind.IpAddress, DisplayName: "客户端 IP"),
        new(EnvironmentGeoCountry, ConditionValueKind.String, DisplayName: "国家"),
        new(EnvironmentGeoProvince, ConditionValueKind.String, DisplayName: "省份"),
        new(EnvironmentGeoCity, ConditionValueKind.String, DisplayName: "城市"),
        new(EnvironmentGeoIsp, ConditionValueKind.String, DisplayName: "运营商"),
        new(EnvironmentLocation, ConditionValueKind.String, DisplayName: "归属地（旧写法）"),
        new(EnvironmentTime, ConditionValueKind.TimeOfDay, DisplayName: "时刻"),
        new(EnvironmentHour, ConditionValueKind.Number, DisplayName: "小时"),
        new(EnvironmentWeekday, ConditionValueKind.Number, DisplayName: "星期"),
        new(EnvironmentDate, ConditionValueKind.Date, DisplayName: "日期"),
        new(EnvironmentClientUserAgent, ConditionValueKind.String, DisplayName: "User-Agent"),
        new(EnvironmentClientKind, ConditionValueKind.String, DisplayName: "客户端类型"),
        new(EnvironmentClientAppId, ConditionValueKind.String, DisplayName: "客户端应用")
    ];

    /// <summary>
    /// 是否旧写法：用了别名，或是保留下来的旧属性（如 <see cref="EnvironmentLocation"/>）
    /// </summary>
    public static bool IsLegacy(ConditionAttributeResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        return resolution.IsAlias || string.Equals(resolution.CanonicalName, EnvironmentLocation, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// <see cref="SaasConditionAttributes.EnvironmentClientKind"/> 的取值
/// </summary>
public static class SaasConditionClientKinds
{
    /// <summary>
    /// 网页登录会话签发的令牌
    /// </summary>
    public const string Web = "web";

    /// <summary>
    /// 开放接口签名调用
    /// </summary>
    public const string OpenApi = "openapi";

    /// <summary>
    /// OAuth 应用签发的令牌
    /// </summary>
    public const string OAuth = "oauth";

    /// <summary>
    /// 未登录
    /// </summary>
    public const string Anonymous = "anonymous";
}
