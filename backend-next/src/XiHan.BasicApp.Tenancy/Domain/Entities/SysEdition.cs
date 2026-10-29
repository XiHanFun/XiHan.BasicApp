// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Tenancy.Domain.Entities;

/// <summary>
/// 租户套餐（平台库，表 <c>sys_edition</c>）：权限白名单（<see cref="SysEditionGrant"/>）决定绑定它的租户能生效哪些权限，租户管理员持有 <c>*</c> 也被收窄到白名单。
/// <see cref="Code"/> 在平台内唯一（含已软删的行之外）；至多一个缺省套餐，开通租户不指定套餐时用它。价格与计费周期只作展示（没有计费流程），用户与存储配额由 Contracts 的 <c>ITenantQuota</c> 约束（4.x 同）。
/// </summary>
public sealed class SysEdition : AppAuditedEntity
{
    /// <summary>
    /// 编码：小写字母开头，只含小写字母、数字与 <c>-</c>，最长 32；创建后不可改。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 说明。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否新租户的缺省套餐；设为缺省时其余套餐的缺省标记被清掉。
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 排序号。
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 席位上限（注册地是该租户的用户与租户成员）；为空不限。
    /// </summary>
    public int? UserLimit { get; set; }

    /// <summary>
    /// 存储空间上限（MB）；为空不限。
    /// </summary>
    public long? StorageLimitMb { get; set; }

    /// <summary>
    /// 价格（两位小数）；为空即不标价。
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// 计费周期（月）；为空即不标周期。
    /// </summary>
    public int? BillingPeriodMonths { get; set; }

    /// <summary>
    /// 是否免费套餐（免费套餐不能标价）。
    /// </summary>
    public bool IsFree { get; set; }
}
