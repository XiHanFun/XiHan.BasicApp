#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenTemplateRepository
// Guid:c0de9e00-0204-4a00-9000-000000000204
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Repositories;

/// <summary>
/// 代码生成模板仓储实现
/// </summary>
public sealed class CodeGenTemplateRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysCodeGenTemplate>(clientResolver), ICodeGenTemplateRepository
{
    /// <inheritdoc />
    public async Task<SysCodeGenTemplate?> GetByCodeAsync(string templateCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateCode);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(template => template.TemplateCode == templateCode.Trim())
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsCodeAsync(string templateCode, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateCode);
        cancellationToken.ThrowIfCancellationRequested();

        var query = CreateQueryable().Where(template => template.TemplateCode == templateCode.Trim());
        if (excludeId.HasValue)
        {
            query = query.Where(template => template.BasicId != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysCodeGenTemplate>> GetEnabledByGroupAsync(string? templateGroup, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = CreateQueryable()
            .Where(template => template.IsEnabled && template.Status == EnableStatus.Enabled);

        if (!string.IsNullOrWhiteSpace(templateGroup))
        {
            var group = templateGroup.Trim();
            query = query.Where(template => template.TemplateGroup == group);
        }

        return await query.OrderBy(template => template.Sort).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysCodeGenTemplate>> GetEnabledByTypeAsync(TemplateType templateType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(template => template.IsEnabled && template.Status == EnableStatus.Enabled && template.TemplateType == templateType)
            .OrderBy(template => template.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysCodeGenTemplate>> GetByCodesAsync(IEnumerable<string> templateCodes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateCodes);
        cancellationToken.ThrowIfCancellationRequested();

        var codes = templateCodes.Where(code => !string.IsNullOrWhiteSpace(code)).Select(code => code.Trim()).Distinct().ToList();
        if (codes.Count == 0)
        {
            return [];
        }

        return await CreateQueryable()
            .Where(template => codes.Contains(template.TemplateCode))
            .OrderBy(template => template.Sort)
            .ToListAsync(cancellationToken);
    }
}
