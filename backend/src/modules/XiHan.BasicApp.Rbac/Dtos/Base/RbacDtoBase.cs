#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacDtoBase
// Guid:1a2b3c4d-5e6f-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 03:58:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Dtos.Base;

/// <summary>
/// RBAC DTO 基类
/// </summary>
public abstract class RbacDtoBase
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public long BasicId { get; set; }
}
