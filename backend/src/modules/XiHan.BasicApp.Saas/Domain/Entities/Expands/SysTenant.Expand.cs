#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenant.Expand
// Guid:2d28152c-d6e9-4396-addb-b479254bad36
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 05:51:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
