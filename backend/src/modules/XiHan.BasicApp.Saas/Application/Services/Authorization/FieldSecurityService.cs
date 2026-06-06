#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldSecurityService
// Guid:a6c789ad-4658-4495-c123-b2d673204c02
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/05 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Collections.Concurrent;
using System.Reflection;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 字段级安全（FLS）服务端。
/// </summary>
public sealed class FieldSecurityService : IFieldSecurityService
{
    private static readonly IReadOnlyDictionary<string, EffectiveFieldRule> EmptyRules =
        new Dictionary<string, EffectiveFieldRule>(StringComparer.Ordinal);

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> WritablePropertyCache = new();

    private readonly ICurrentUser _currentUser;

    private readonly IFieldLevelSecurityRepository _fieldLevelSecurityRepository;

    private readonly IResourceRepository _resourceRepository;

    private readonly IUserRoleRepository _userRoleRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FieldSecurityService(
        IFieldLevelSecurityRepository fieldLevelSecurityRepository,
        IResourceRepository resourceRepository,
        IUserRoleRepository userRoleRepository,
        ICurrentUser currentUser)
    {
        _fieldLevelSecurityRepository = fieldLevelSecurityRepository;
        _resourceRepository = resourceRepository;
        _userRoleRepository = userRoleRepository;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, EffectiveFieldRule>> ResolveAsync(string resourceCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resourceCode) || !_currentUser.UserId.HasValue)
        {
            return EmptyRules;
        }

        var userId = _currentUser.UserId.Value;
        var resource = await _resourceRepository.GetByCodeAsync(resourceCode, cancellationToken);
        if (resource is null)
        {
            return EmptyRules;
        }

        var roleIds = (await _userRoleRepository.GetValidByUserIdAsync(userId, DateTimeOffset.UtcNow, cancellationToken))
            .Select(userRole => userRole.RoleId)
            .ToHashSet();

        // 库内按资源+启用收敛，内存判定目标归属（用户/角色）
        var rules = await _fieldLevelSecurityRepository.GetListAsync(
            rule => rule.ResourceId == resource.BasicId && rule.Status == EnableStatus.Enabled,
            cancellationToken);

        var applicable = rules.Where(rule =>
            (rule.TargetType == FieldSecurityTargetType.User && rule.TargetId == userId)
            || (rule.TargetType == FieldSecurityTargetType.Role && roleIds.Contains(rule.TargetId)));

        // deny-overrides：同字段任一不可读/不可编辑即不可读/不可编辑；脱敏取最严
        return applicable
            .GroupBy(rule => rule.FieldName)
            .Select(group =>
            {
                var strongest = group.OrderByDescending(rule => MaskRank(rule.MaskStrategy)).First();
                return new EffectiveFieldRule
                {
                    FieldName = group.Key,
                    IsReadable = group.All(rule => rule.IsReadable),
                    IsEditable = group.All(rule => rule.IsEditable),
                    MaskStrategy = strongest.MaskStrategy,
                    MaskPattern = strongest.MaskPattern,
                };
            })
            .ToDictionary(rule => rule.FieldName, StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public async Task ApplyAsync<T>(string resourceCode, T? item, CancellationToken cancellationToken = default)
        where T : class
    {
        if (item is null)
        {
            return;
        }

        var rules = await ResolveAsync(resourceCode, cancellationToken);
        if (rules.Count == 0)
        {
            return;
        }

        MaskInstance(item, rules);
    }

    /// <inheritdoc />
    public async Task ApplyAsync<T>(string resourceCode, IEnumerable<T> items, CancellationToken cancellationToken = default)
        where T : class
    {
        var rules = await ResolveAsync(resourceCode, cancellationToken);
        if (rules.Count == 0)
        {
            return;
        }

        foreach (var item in items)
        {
            if (item is not null)
            {
                MaskInstance(item, rules);
            }
        }
    }

    /// <inheritdoc />
    public async Task EnsureEditableAsync(string resourceCode, IEnumerable<string> changingFields, CancellationToken cancellationToken = default)
    {
        var rules = await ResolveAsync(resourceCode, cancellationToken);
        if (rules.Count == 0)
        {
            return;
        }

        foreach (var field in changingFields)
        {
            if (rules.TryGetValue(field, out var rule) && !rule.IsEditable)
            {
                throw new InvalidOperationException($"字段「{field}」当前用户无修改权限。");
            }
        }
    }

    /// <inheritdoc />
    public async Task EnsureUpdatableAsync<TInput, TCurrent>(string resourceCode, TInput input, TCurrent current, CancellationToken cancellationToken = default)
        where TInput : class
        where TCurrent : class
    {
        var rules = await ResolveAsync(resourceCode, cancellationToken);
        if (rules.Count == 0)
        {
            return;
        }

        var inputType = typeof(TInput);
        var currentType = typeof(TCurrent);
        foreach (var (fieldName, rule) in rules)
        {
            if (rule.IsEditable)
            {
                continue;
            }
            var inputProperty = inputType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
            var currentProperty = currentType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (inputProperty is null || currentProperty is null)
            {
                continue;
            }
            if (!Equals(inputProperty.GetValue(input), currentProperty.GetValue(current)))
            {
                throw new InvalidOperationException($"字段「{fieldName}」当前用户无修改权限。");
            }
        }
    }

    private static void MaskInstance<T>(T item, IReadOnlyDictionary<string, EffectiveFieldRule> rules)
        where T : class
    {
        var properties = WritablePropertyCache.GetOrAdd(typeof(T), static type => type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property is { CanRead: true, CanWrite: true })
            .ToArray());

        foreach (var property in properties)
        {
            if (!rules.TryGetValue(property.Name, out var rule))
            {
                continue;
            }
            if (rule.IsReadable && rule.MaskStrategy == FieldMaskStrategy.None)
            {
                continue;
            }

            if (property.PropertyType == typeof(string))
            {
                var masked = FieldMasker.Mask(property.GetValue(item) as string, rule.IsReadable, rule.MaskStrategy, rule.MaskPattern);
                property.SetValue(item, masked);
            }
            else if (!rule.IsReadable || rule.MaskStrategy == FieldMaskStrategy.Hidden)
            {
                // 非字符串字段：不可读/隐藏 → 置默认值（引用/可空 → null；非空值类型 → default）
                var fallback = property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) is null
                    ? Activator.CreateInstance(property.PropertyType)
                    : null;
                property.SetValue(item, fallback);
            }
        }
    }

    /// <summary>脱敏严格度排序（用于同字段取最严策略）</summary>
    private static int MaskRank(FieldMaskStrategy strategy) => strategy switch
    {
        FieldMaskStrategy.Hidden => 6,
        FieldMaskStrategy.FullMask => 5,
        FieldMaskStrategy.Hash => 4,
        FieldMaskStrategy.Redact => 3,
        FieldMaskStrategy.PartialMask => 2,
        FieldMaskStrategy.Custom => 1,
        _ => 0,
    };
}
