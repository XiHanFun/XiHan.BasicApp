#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IEnumAppService
// Guid:4f9b8f1c-7461-41f8-89f1-d2a2c61f0f11
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 枚举应用服务
/// </summary>
public interface IEnumAppService
    : IApplicationService
{
    /// <summary>
    /// 按枚举名称获取枚举定义
    /// </summary>
    /// <param name="enumName">枚举名称（短名或完整名）</param>
    /// <param name="language">语言（如 zh-CN / en-US）</param>
    /// <param name="includeHidden">是否包含隐藏项</param>
    /// <param name="includeDict">是否启用字典覆盖</param>
    /// <param name="dictCode">字典编码（为空时默认用枚举名）</param>
    /// <param name="tenantId">租户 ID</param>
    /// <returns>枚举定义</returns>
    Task<EnumDefinitionDto> GetByNameAsync(
        string enumName,
        string? language = null,
        bool includeHidden = false,
        bool includeDict = false,
        string? dictCode = null,
        long? tenantId = null);

    /// <summary>
    /// 批量获取枚举定义
    /// </summary>
    /// <param name="input">批量查询参数</param>
    /// <returns>键为枚举名的查询结果</returns>
    Task<IReadOnlyDictionary<string, EnumDefinitionDto>> GetBatchAsync(EnumBatchQueryDto input);

    /// <summary>
    /// 获取系统已注册的枚举名称列表
    /// </summary>
    /// <returns>枚举名称列表</returns>
    Task<IReadOnlyList<string>> GetNamesAsync();
}
