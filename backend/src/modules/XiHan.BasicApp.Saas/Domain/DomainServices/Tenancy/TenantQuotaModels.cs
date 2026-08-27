// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 租户配额快照
/// </summary>
/// <remarks>
/// 上限已完成"租户级覆盖版本级"的解析：租户自身设了值就用租户的，否则回落到所属版本套餐的值，
/// 两者皆空表示不限（与 SysTenantEdition 的字段约定一致）。
/// 单位保持与各自数据源一致，不在此处换算：<paramref name="StorageLimit" /> 为 MB（来自套餐定义），
/// <paramref name="UsedStorageBytes" /> 为字节（来自文件表求和），比较由调用方按需换算。
/// </remarks>
/// <param name="TenantId">租户主键</param>
/// <param name="UserLimit">生效席位上限，null 表示不限</param>
/// <param name="UsedUserCount">已占用席位数</param>
/// <param name="StorageLimit">生效存储上限(MB)，null 表示不限</param>
/// <param name="UsedStorageBytes">已占用存储(字节)</param>
public sealed record TenantQuotaSnapshot(
    long TenantId,
    int? UserLimit,
    long UsedUserCount,
    long? StorageLimit,
    long UsedStorageBytes);
