// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统租户实体扩展（精简导航：只保留核心业务关联，其他数据通过各自仓储按需查询）
/// </summary>
public partial class SysTenant
{
    /// <summary>
    /// 版本/套餐信息
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(EditionId))]
    public virtual SysTenantEdition? Edition { get; set; }

    /// <summary>
    /// 租户成员关系列表（多对多核心：承载用户在本租户的成员身份）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysTenantUser.TenantId))]
    public virtual List<SysTenantUser>? TenantUsers { get; set; }

    /// <summary>
    /// 租户配置列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysConfig.TenantId))]
    public virtual List<SysConfig>? Configs { get; set; }
}
