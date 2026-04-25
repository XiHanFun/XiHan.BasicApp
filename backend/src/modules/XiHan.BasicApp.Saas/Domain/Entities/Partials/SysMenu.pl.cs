#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysMenu.pl
// Guid:dc28152c-d6e9-4396-addb-b479254bad17
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 03:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统菜单实体扩展
/// </summary>
public partial class SysMenu : IValidatableObject
{
    /// <summary>
    /// 父级菜单
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToOne, nameof(ParentId))]
    public virtual SysMenu? ParentMenu { get; set; }

    /// <summary>
    /// 子菜单列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(ParentId))]
    public virtual List<SysMenu>? Children { get; set; }

    /// <summary>
    /// 关联权限（菜单可见性所依赖的权限）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(PermissionId))]
    public virtual SysPermission? Permission { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsGlobal && TenantId != 0)
        {
            yield return new ValidationResult("全局菜单必须使用 TenantId = 0。", [nameof(IsGlobal), nameof(TenantId)]);
        }

        if (TenantId == 0 && !IsGlobal)
        {
            yield return new ValidationResult("平台租户（TenantId=0）的菜单必须标记为全局。", [nameof(TenantId), nameof(IsGlobal)]);
        }

        if (string.IsNullOrWhiteSpace(MenuCode))
        {
            yield return new ValidationResult("MenuCode 不能为空。", [nameof(MenuCode)]);
        }

        if (string.IsNullOrWhiteSpace(MenuName))
        {
            yield return new ValidationResult("MenuName 不能为空。", [nameof(MenuName)]);
        }
    }
}
