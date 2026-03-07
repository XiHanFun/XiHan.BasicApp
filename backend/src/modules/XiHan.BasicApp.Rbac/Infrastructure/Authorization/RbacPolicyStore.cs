#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacPolicyStore
// Guid:4fe77f03-8fe1-4766-bda2-33a9eb7e9297
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/08 17:24:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Authorization.Policies;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Rbac.Infrastructure.Authorization;

/// <summary>
/// 基于 RBAC 数据库的策略存储实现
/// </summary>
public class RbacPolicyStore : IPolicyStore
{
    private const string PolicyConfigGroup = "Framework.Authorization.Policy";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IConfigRepository _configRepository;
    private readonly ISqlSugarDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;

    private ISqlSugarClient DbClient => _dbContext.GetClient();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="configRepository">配置仓储</param>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="currentTenant">当前租户</param>
    public RbacPolicyStore(
        IConfigRepository configRepository,
        ISqlSugarDbContext dbContext,
        ICurrentTenant currentTenant)
    {
        _configRepository = configRepository;
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 获取所有策略
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>策略列表</returns>
    public async Task<List<PolicyDefinition>> GetAllPoliciesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _currentTenant.Id;
        IReadOnlyList<SysConfig> configs = tenantId.HasValue
            ? await _configRepository.GetListAsync(
                config => config.TenantId == tenantId.Value && config.ConfigGroup == PolicyConfigGroup,
                cancellationToken)
            : await _configRepository.GetListAsync(
                config => config.TenantId == null && config.ConfigGroup == PolicyConfigGroup,
                cancellationToken);

        var policies = new List<PolicyDefinition>(configs.Count);
        foreach (var config in configs)
        {
            var policy = ParsePolicy(config.ConfigValue);
            if (policy is not null)
            {
                policies.Add(policy);
            }
        }

        return policies
            .OrderBy(static policy => policy.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// 根据名称获取策略
    /// </summary>
    /// <param name="policyName">策略名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>策略定义</returns>
    public async Task<PolicyDefinition?> GetPolicyByNameAsync(string policyName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(policyName))
        {
            return null;
        }

        var configKey = BuildPolicyConfigKey(policyName);
        var config = await _configRepository.GetByConfigKeyAsync(configKey, _currentTenant.Id, cancellationToken);
        return config is null ? null : ParsePolicy(config.ConfigValue);
    }

    /// <summary>
    /// 创建策略
    /// </summary>
    /// <param name="policy">策略定义</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task CreatePolicyAsync(PolicyDefinition policy, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (string.IsNullOrWhiteSpace(policy.Name))
        {
            throw new ArgumentException("策略名称不能为空", nameof(policy));
        }

        var tenantId = _currentTenant.Id;
        var configKey = BuildPolicyConfigKey(policy.Name);
        var existing = await _configRepository.GetByConfigKeyAsync(configKey, tenantId, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"策略 '{policy.Name}' 已存在");
        }

        var payload = SerializePolicy(policy);
        var config = new SysConfig
        {
            TenantId = tenantId,
            ConfigName = NormalizeName(policy.Name),
            ConfigGroup = PolicyConfigGroup,
            ConfigKey = configKey,
            ConfigValue = payload,
            ConfigType = ConfigType.Application,
            DataType = ConfigDataType.Json,
            ConfigDescription = BuildDescription(policy.Name),
            IsBuiltIn = false,
            IsEncrypted = false,
            Status = policy.IsEnabled ? YesOrNo.Yes : YesOrNo.No
        };

        await DbClient.Insertable(config).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 更新策略
    /// </summary>
    /// <param name="policy">策略定义</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdatePolicyAsync(PolicyDefinition policy, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (string.IsNullOrWhiteSpace(policy.Name))
        {
            throw new ArgumentException("策略名称不能为空", nameof(policy));
        }

        var configKey = BuildPolicyConfigKey(policy.Name);
        var existing = await _configRepository.GetByConfigKeyAsync(configKey, _currentTenant.Id, cancellationToken)
            ?? throw new InvalidOperationException($"策略 '{policy.Name}' 不存在");

        existing.ConfigName = NormalizeName(policy.Name);
        existing.ConfigValue = SerializePolicy(policy);
        existing.ConfigDescription = BuildDescription(policy.Name);
        existing.Status = policy.IsEnabled ? YesOrNo.Yes : YesOrNo.No;
        existing.DataType = ConfigDataType.Json;

        await DbClient.Updateable(existing).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除策略
    /// </summary>
    /// <param name="policyName">策略名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeletePolicyAsync(string policyName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(policyName))
        {
            return;
        }

        var configKey = BuildPolicyConfigKey(policyName);
        var existing = await _configRepository.GetByConfigKeyAsync(configKey, _currentTenant.Id, cancellationToken);
        if (existing is null)
        {
            return;
        }

        await DbClient.Deleteable<SysConfig>()
            .Where(config => config.BasicId == existing.BasicId)
            .ExecuteCommandAsync(cancellationToken);
    }

    private static string BuildPolicyConfigKey(string policyName)
    {
        var normalizedName = policyName.Trim().ToUpperInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedName)));
        return $"policy:{hash}";
    }

    private static string NormalizeName(string policyName)
    {
        var trimmed = string.IsNullOrWhiteSpace(policyName) ? "Policy" : policyName.Trim();
        return trimmed.Length <= 100 ? trimmed : trimmed[..100];
    }

    private static string BuildDescription(string policyName)
    {
        var description = $"Framework policy definition: {policyName}";
        return description.Length <= 500 ? description : description[..500];
    }

    private static string SerializePolicy(PolicyDefinition policy)
    {
        var payload = new PolicyPayload
        {
            Name = policy.Name,
            DisplayName = policy.DisplayName,
            Description = policy.Description,
            RequiredRoles = [.. policy.RequiredRoles],
            RequiredPermissions = [.. policy.RequiredPermissions],
            RequiredClaims = new Dictionary<string, string>(policy.RequiredClaims, StringComparer.Ordinal),
            IsEnabled = policy.IsEnabled,
            Properties = policy.Properties?.ToDictionary(
                static item => item.Key,
                static item => item.Value?.ToString() ?? string.Empty,
                StringComparer.Ordinal)
        };

        return JsonSerializer.Serialize(payload, JsonSerializerOptions);
    }

    private static PolicyDefinition? ParsePolicy(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        PolicyPayload? model;
        try
        {
            model = JsonSerializer.Deserialize<PolicyPayload>(payload, JsonSerializerOptions);
        }
        catch
        {
            return null;
        }

        if (model is null || string.IsNullOrWhiteSpace(model.Name))
        {
            return null;
        }

        return new PolicyDefinition
        {
            Name = model.Name,
            DisplayName = model.DisplayName,
            Description = model.Description,
            RequiredRoles = model.RequiredRoles ?? [],
            RequiredPermissions = model.RequiredPermissions ?? [],
            RequiredClaims = model.RequiredClaims ?? [],
            IsEnabled = model.IsEnabled,
            Properties = model.Properties?.ToDictionary(
                static item => item.Key,
                static item => (object)item.Value,
                StringComparer.Ordinal)
        };
    }

    private sealed class PolicyPayload
    {
        public string Name { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<string>? RequiredRoles { get; set; }

        public List<string>? RequiredPermissions { get; set; }

        public Dictionary<string, string>? RequiredClaims { get; set; }

        public bool IsEnabled { get; set; } = true;

        public Dictionary<string, string>? Properties { get; set; }
    }
}
