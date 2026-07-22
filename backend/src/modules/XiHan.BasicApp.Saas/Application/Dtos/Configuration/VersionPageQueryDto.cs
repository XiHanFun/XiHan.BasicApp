// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统版本分页查询 DTO
/// </summary>
public sealed class VersionPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 应用版本
    /// </summary>
    public string? AppVersion { get; set; }

    /// <summary>
    /// 数据库版本
    /// </summary>
    public string? DbVersion { get; set; }

    /// <summary>
    /// 最小支持版本
    /// </summary>
    public string? MinSupportVersion { get; set; }

    /// <summary>
    /// 是否升级中
    /// </summary>
    public bool? IsUpgrading { get; set; }

    /// <summary>
    /// 升级节点
    /// </summary>
    public string? UpgradeNode { get; set; }

    /// <summary>
    /// 升级开始时间起始
    /// </summary>
    public DateTimeOffset? UpgradeStartTimeStart { get; set; }

    /// <summary>
    /// 升级开始时间结束
    /// </summary>
    public DateTimeOffset? UpgradeStartTimeEnd { get; set; }
}
