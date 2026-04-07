#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFileDomainService
// Guid:6e7f8091-0213-2345-ef01-23456789ab03
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 文件领域服务接口
/// </summary>
public interface IFileDomainService
{
    /// <summary>
    /// 创建文件
    /// </summary>
    Task<SysFile> CreateAsync(SysFile file);

    /// <summary>
    /// 更新文件
    /// </summary>
    Task<SysFile> UpdateAsync(SysFile file);

    /// <summary>
    /// 删除文件
    /// </summary>
    Task<bool> DeleteAsync(long id);
}
