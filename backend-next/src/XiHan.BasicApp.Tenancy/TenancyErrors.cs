// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Tenancy;

/// <summary>
/// 租户包的错误码：<c>TENANCY_&lt;KIND&gt;_&lt;NNN&gt;</c>。
/// </summary>
public static class TenancyErrors
{
    /// <summary>
    /// 租户不存在。
    /// </summary>
    public const string TenantNotFound = "TENANCY_NOTFOUND_001";

    /// <summary>
    /// 成员行不存在。
    /// </summary>
    public const string MemberNotFound = "TENANCY_NOTFOUND_002";

    /// <summary>
    /// 要加为成员的用户不存在。
    /// </summary>
    public const string MemberUserNotFound = "TENANCY_NOTFOUND_003";

    /// <summary>
    /// 租户 ID 已被占用。
    /// </summary>
    public const string TenantIdTaken = "TENANCY_CONFLICT_001";

    /// <summary>
    /// 租户管理员已初始化过。
    /// </summary>
    public const string AdminInitialized = "TENANCY_CONFLICT_002";

    /// <summary>
    /// 租户下还有用户，不能删除。
    /// </summary>
    public const string HasUsers = "TENANCY_CONFLICT_003";

    /// <summary>
    /// 管理员登录名在该租户内已被占用。
    /// </summary>
    public const string AdminNameTaken = "TENANCY_CONFLICT_004";

    /// <summary>
    /// 用户已是该租户的成员（或注册地就在该租户）。
    /// </summary>
    public const string MemberExists = "TENANCY_CONFLICT_005";

    /// <summary>
    /// 只有平台主体能管理租户。
    /// </summary>
    public const string PlatformOnly = "TENANCY_DENIED_001";

    /// <summary>
    /// 列表的筛选或排序字段不受支持。
    /// </summary>
    public const string QueryUnsupported = "TENANCY_VALIDATION_001";

    /// <summary>
    /// 没有任何租户初始化扩展（上层包未装配），开通管理员会没有权限。
    /// </summary>
    public const string NoInitializer = "TENANCY_UNAVAILABLE_001";
}
