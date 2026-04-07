#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IConfigQueryService
// Guid:f1a2b3c4-d5e6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Queries;
using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 配置查询服务接口
/// </summary>
public interface IConfigQueryService : IQueryService
{
    /// <summary>
    /// 按键获取配置值（最高频调用，自动回退到默认值）
    /// </summary>
    Task<string?> GetValueAsync(string configKey, long? tenantId = null);

    /// <summary>
    /// 按键获取完整配置
    /// </summary>
    Task<ConfigDto?> GetByKeyAsync(string configKey, long? tenantId = null);

    /// <summary>
    /// 按分组获取配置列表
    /// </summary>
    Task<IReadOnlyList<ConfigDto>> GetByGroupAsync(string configGroup, long? tenantId = null);
}
