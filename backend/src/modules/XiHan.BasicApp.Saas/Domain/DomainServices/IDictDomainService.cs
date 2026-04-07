#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDictDomainService
// Guid:2a3b4c5d-6e7f-8901-abcd-ef0123456703
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 字典领域服务接口
/// </summary>
public interface IDictDomainService
{
    /// <summary>
    /// 创建字典
    /// </summary>
    Task<SysDict> CreateAsync(SysDict dict);

    /// <summary>
    /// 更新字典
    /// </summary>
    Task<SysDict> UpdateAsync(SysDict dict);

    /// <summary>
    /// 删除字典
    /// </summary>
    Task<bool> DeleteAsync(long id);
}
