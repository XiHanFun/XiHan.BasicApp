// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Tenancy;

/// <summary>
/// 租户包的错误码。
/// </summary>
/// <remarks>
/// 格式为 <c>TENANCY_&lt;KIND&gt;_&lt;NNN&gt;</c>。
/// </remarks>
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
    /// 套餐不存在。
    /// </summary>
    public const string EditionNotFound = "TENANCY_NOTFOUND_004";

    /// <summary>
    /// 套餐编码已被占用。
    /// </summary>
    public const string EditionCodeTaken = "TENANCY_CONFLICT_006";

    /// <summary>
    /// 套餐还有租户在用，不能删除。
    /// </summary>
    public const string EditionInUse = "TENANCY_CONFLICT_007";

    /// <summary>
    /// 套餐白名单里有不是租户能生效的权限。
    /// </summary>
    public const string EditionPermissionInvalid = "TENANCY_VALIDATION_002";

    /// <summary>
    /// 开通租户没指定套餐，也没有缺省套餐。
    /// </summary>
    public const string NoDefaultEdition = "TENANCY_VALIDATION_003";

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

    /// <summary>
    /// 免费套餐不能标价。
    /// </summary>
    public const string FreeEditionPriced = "TENANCY_VALIDATION_004";

    /// <summary>
    /// 租户存储空间不足。
    /// </summary>
    public const string StorageQuotaExceeded = "TENANCY_CONFLICT_008";

    /// <summary>
    /// 租户席位已达上限。
    /// </summary>
    public const string SeatQuotaExceeded = "TENANCY_CONFLICT_009";

    /// <summary>
    /// 订阅属于租户，平台没有订阅。
    /// </summary>
    public const string SubscriptionTenantOnly = "TENANCY_DENIED_002";
    /// <summary>
    /// 租户还没有所有者（未初始化管理员）。
    /// </summary>
    public const string OwnerMissing = "TENANCY_CONFLICT_010";

    /// <summary>
    /// 该用户已经是这个租户的所有者。
    /// </summary>
    public const string AlreadyOwner = "TENANCY_CONFLICT_011";

    /// <summary>
    /// 所有者不能移出成员，要先转移所有权。
    /// </summary>
    public const string OwnerRemoval = "TENANCY_CONFLICT_012";

    /// <summary>
    /// 新所有者不合格。
    /// </summary>
    /// <remarks>
    /// 须是该租户注册地的用户或生效中的普通成员，且启用、未锁定。
    /// </remarks>
    public const string OwnerIneligible = "TENANCY_VALIDATION_005";

    /// <summary>
    /// 成员类型与账号不符。
    /// </summary>
    /// <remarks>
    /// 平台账号只能以运维支持加入，运维支持只能是平台账号。
    /// </remarks>
    public const string MemberTypeMismatch = "TENANCY_VALIDATION_006";

    /// <summary>
    /// 成员的到期时间不晚于生效时间。
    /// </summary>
    public const string MemberWindowInvalid = "TENANCY_VALIDATION_007";

    /// <summary>
    /// 独立库已就绪，不能再改隔离方式与连接，也不用再初始化。
    /// </summary>
    public const string DatabaseInitialized = "TENANCY_CONFLICT_013";

    /// <summary>
    /// 独立库还没就绪，先初始化数据库。
    /// </summary>
    public const string DatabaseNotReady = "TENANCY_CONFLICT_014";

    /// <summary>
    /// 租户已过到期时间，先延长到期时间再恢复。
    /// </summary>
    public const string TenantExpired = "TENANCY_CONFLICT_015";

    /// <summary>
    /// 独立库要给连接串。
    /// </summary>
    public const string ConnectionRequired = "TENANCY_VALIDATION_008";

    /// <summary>
    /// 租户是共库，没有独立库可初始化。
    /// </summary>
    public const string NotIsolated = "TENANCY_VALIDATION_009";
}
