#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IConfigDomainService
// Guid:c4d5e6f7-a8b9-0123-def0-123456789abc
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 配置领域服务接口
/// </summary>
public interface IConfigDomainService
{
    /// <summary>
    /// 创建配置
    /// </summary>
    Task<SysConfig> CreateAsync(SysConfig config);

    /// <summary>
    /// 更新配置
    /// </summary>
    Task<SysConfig> UpdateAsync(SysConfig config);

    /// <summary>
    /// 删除配置
    /// </summary>
    Task<bool> DeleteAsync(long id);
}
