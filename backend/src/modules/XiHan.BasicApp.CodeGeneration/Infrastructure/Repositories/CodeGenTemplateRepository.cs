// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    /// <summary>
    /// 根据模板编码获取
    /// </summary>
    public async Task<SysCodeGenTemplate?> GetByCodeAsync(string templateCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateCode);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(template => template.TemplateCode == templateCode.Trim())
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 检查模板编码是否存在
    /// </summary>
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

    /// <summary>
    /// 按分组获取启用模板（用于生成时批量加载）
    /// </summary>
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

    /// <summary>
    /// 按模板类型获取启用模板（生成时按表的单表/树表/主子表选取匹配模板集；模板通用、不按业务模块过滤）
    /// </summary>
    public async Task<IReadOnlyList<SysCodeGenTemplate>> GetEnabledByTypeAsync(TemplateType templateType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 通用模板（TemplateType 为空）对所有类型生效，故需 "类型匹配 或 为空"。
        // 该判定不下推 SQL：本仓库已知 SqlSugar 把含可空字段的 OR lambda 下推到 PG 会触发 42601，
        // 且模板总量极小（内置 23 条 + 少量自定义），取回后内存过滤代价可忽略。
        var enabled = await CreateQueryable()
            .Where(template => template.IsEnabled && template.Status == EnableStatus.Enabled)
            .OrderBy(template => template.Sort)
            .ToListAsync(cancellationToken);

        return [.. enabled.Where(template => template.TemplateType is null || template.TemplateType == templateType)];
    }

    /// <summary>
    /// 根据模板编码集合批量获取
    /// </summary>
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
