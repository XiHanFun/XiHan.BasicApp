#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysFile.pl
// Guid:8d28152c-d6e9-4396-addb-b479254bad42
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 5:57:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统文件实体扩展
/// </summary>
public partial class SysFile
{
    /// <summary>
    /// 租户信息
    /// </summary>
    [Navigate(NavigateType.ManyToOne, nameof(TenantId))]
    public virtual SysTenant? Tenant { get; set; }

    /// <summary>
    /// 上传者信息
    /// </summary>
    [Navigate(NavigateType.ManyToOne, nameof(UploaderId))]
    public virtual SysUser? Uploader { get; set; }
}
