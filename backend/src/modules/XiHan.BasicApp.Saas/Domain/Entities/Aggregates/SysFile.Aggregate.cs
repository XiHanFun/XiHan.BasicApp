#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysFile.Aggregate.cs
// Guid:c5e9a3b6-0d4f-4c8a-b123-f7e0d9c4a6b8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 文件领域行为
/// </summary>
public partial class SysFile
{
    /// <summary>
    /// 标记文件过期
    /// </summary>
    public void MarkAsExpired()
    {
        Status = FileStatus.Expired;
    }
}
