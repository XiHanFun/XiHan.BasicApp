#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ICodeGenTemplateRepository
// Guid:c0de9e00-0104-4a00-9000-000000000104
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.CodeGeneration.Domain.Repositories;

/// <summary>
/// 代码生成模板仓储接口
/// </summary>
public interface ICodeGenTemplateRepository : ISaasRepository<SysCodeGenTemplate>
{
    /// <summary>
    /// 根据模板编码获取
    /// </summary>
    Task<SysCodeGenTemplate?> GetByCodeAsync(string templateCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查模板编码是否存在
    /// </summary>
    Task<bool> ExistsCodeAsync(string templateCode, long? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按分组获取启用模板（用于生成时批量加载）
    /// </summary>
    Task<IReadOnlyList<SysCodeGenTemplate>> GetEnabledByGroupAsync(string? templateGroup, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据模板编码集合批量获取
    /// </summary>
    Task<IReadOnlyList<SysCodeGenTemplate>> GetByCodesAsync(IEnumerable<string> templateCodes, CancellationToken cancellationToken = default);
}
