// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Tenancy.Domain.Entities;

/// <summary>
/// 租户套餐（平台库，表 <c>sys_edition</c>）
/// </summary>
/// <remarks>
/// 用权限白名单（<see cref="SysEditionGrant"/>）决定绑定它的租户能生效哪些权限：Access 在授予命中之后再求交，租户管理员持有 <c>*</c> 也被收窄到白名单；换套餐只改租户上的引用，不动任何授予
/// 落平台库、只在平台侧管理；编码由部分唯一索引 <c>ux_sys_edition_code</c> 只约束未删除的行，创建后不可改，种子按编码只首写
/// 至多一个缺省套餐，开通租户不指定套餐时用它；互斥靠写路径清掉其余的缺省标记，库里没有约束；缺省套餐必须启用
/// 停用只挡新的分配（开通、换套餐），已绑定的租户照旧按它的白名单与配额
/// 席位与存储上限是缺省值，租户自己的上限优先、都没有即不限，由 Contracts 的 <c>ITenantQuota</c> 在租户主体建用户与上传文件时约束；价格与计费周期只作展示，没有计费流程
/// 还有租户绑定即不能删，删除时先物理删掉白名单再软删套餐；门控遇到绑定的套餐已不存在时按空白名单处理，宁可拒绝
/// </remarks>
[SugarTable(TableName = "sys_edition", TableDescription = "租户套餐（平台库，表 sys_edition）")]
[ActiveUniqueIndex(nameof(Code), Name = "ux_sys_edition_code")]
public sealed class SysEdition : AppAuditedEntity
{
    /// <summary>
    /// 套餐编码
    /// </summary>
    /// <remarks>
    /// 小写字母开头，只含小写字母、数字与 <c>-</c>，最长 32；创建后不可改
    /// </remarks>
    [SugarColumn(ColumnName = "code", Length = 512, IsNullable = false, ColumnDescription = "套餐编码")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 512, IsNullable = false, ColumnDescription = "名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 说明
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 512, IsNullable = true, ColumnDescription = "说明")]
    public string? Description { get; set; }

    /// <summary>
    /// 是否新租户的缺省套餐
    /// </summary>
    /// <remarks>
    /// 设为缺省时其余套餐的缺省标记被清掉；缺省套餐必须启用
    /// </remarks>
    [SugarColumn(ColumnName = "is_default", IsNullable = false, ColumnDescription = "是否新租户的缺省套餐")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    /// <remarks>
    /// 停用的套餐不能再分配给租户（开通或换套餐），已绑定的租户照旧按它的白名单与配额
    /// </remarks>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public EditionStatus Status { get; set; } = EditionStatus.Active;

    /// <summary>
    /// 排序号
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "排序号")]
    public int Order { get; set; }

    /// <summary>
    /// 席位上限（注册地是该租户的用户与租户成员）
    /// </summary>
    /// <remarks>
    /// 为空不限
    /// </remarks>
    [SugarColumn(ColumnName = "user_limit", IsNullable = true, ColumnDescription = "席位上限（注册地是该租户的用户与租户成员）")]
    public int? UserLimit { get; set; }

    /// <summary>
    /// 存储空间上限（MB）
    /// </summary>
    /// <remarks>
    /// 为空不限
    /// </remarks>
    [SugarColumn(ColumnName = "storage_limit_mb", IsNullable = true, ColumnDescription = "存储空间上限（MB）")]
    public long? StorageLimitMb { get; set; }

    /// <summary>
    /// 价格（两位小数）
    /// </summary>
    /// <remarks>
    /// 为空即不标价
    /// </remarks>
    [SugarColumn(ColumnName = "price", IsNullable = true, ColumnDescription = "价格（两位小数）")]
    public decimal? Price { get; set; }

    /// <summary>
    /// 计费周期（月）
    /// </summary>
    /// <remarks>
    /// 为空即不标周期
    /// </remarks>
    [SugarColumn(ColumnName = "billing_period_months", IsNullable = true, ColumnDescription = "计费周期（月）")]
    public int? BillingPeriodMonths { get; set; }

    /// <summary>
    /// 是否免费套餐（免费套餐不能标价）
    /// </summary>
    [SugarColumn(ColumnName = "is_free", IsNullable = false, ColumnDescription = "是否免费套餐（免费套餐不能标价）")]
    public bool IsFree { get; set; }
}

/// <summary>
/// 套餐的启停状态
/// </summary>
public enum EditionStatus
{
    /// <summary>
    /// 启用
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用：不能再分配给租户，已绑定的租户不受影响
    /// </summary>
    Disabled = 1,
}
