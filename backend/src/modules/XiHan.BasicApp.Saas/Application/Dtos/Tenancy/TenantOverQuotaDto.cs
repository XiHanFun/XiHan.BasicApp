// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户超配额告警 DTO
/// </summary>
/// <remarks>
/// 用于一次性核对存量：配额执行是从某个版本起才生效的，在那之前建立的租户可能早已超出上限。
/// 拦截只作用于新增、不追溯存量，所以这些租户不会被动暴露出来，需要主动查一次。
/// </remarks>
public sealed class TenantOverQuotaDto
{
    /// <summary>
    /// 租户主键
    /// </summary>
    public long TenantId { get; set; }

    /// <summary>
    /// 租户编码
    /// </summary>
    public string TenantCode { get; set; } = string.Empty;

    /// <summary>
    /// 租户名称
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// 席位是否已超出上限
    /// </summary>
    public bool SeatExceeded { get; set; }

    /// <summary>
    /// 生效席位上限（此处必然有值，不限的租户不会出现在告警里）
    /// </summary>
    public int? UserLimit { get; set; }

    /// <summary>
    /// 已占用席位数
    /// </summary>
    public long UsedUserCount { get; set; }

    /// <summary>
    /// 存储是否已超出上限
    /// </summary>
    public bool StorageExceeded { get; set; }

    /// <summary>
    /// 生效存储上限(MB)
    /// </summary>
    public long? StorageLimit { get; set; }

    /// <summary>
    /// 已占用存储空间(字节)
    /// </summary>
    public long UsedStorageBytes { get; set; }
}
