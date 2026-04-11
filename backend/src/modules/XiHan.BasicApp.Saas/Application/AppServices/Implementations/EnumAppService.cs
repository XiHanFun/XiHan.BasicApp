#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EnumAppService
// Guid:d40d5897-c4be-4c39-bbc8-2742d028a4e8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Reflection;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Utils.Core;
using XiHan.Framework.Utils.Enums;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 枚举应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
[Authorize]
public class EnumAppService : ApplicationServiceBase, IEnumAppService
{
    private static readonly HybridCacheEntryOptions EnumCacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };

    private static readonly HybridCacheEntryOptions EnumWithDictCacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(3),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };

    private readonly Lock _syncLock = new();
    private readonly HybridCache _hybridCache;
    private readonly IDictRepository _dictRepository;
    private readonly IStringLocalizerFactory _stringLocalizerFactory;
    private readonly Dictionary<string, Type> _enumFullNameMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Type> _enumShortNameMap = new(StringComparer.OrdinalIgnoreCase);
    private bool _enumCacheInitialized;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="hybridCache">缓存</param>
    /// <param name="dictRepository">字典仓储</param>
    /// <param name="stringLocalizerFactory">本地化工厂</param>
    public EnumAppService(
        HybridCache hybridCache,
        IDictRepository dictRepository,
        IStringLocalizerFactory stringLocalizerFactory)
    {
        _hybridCache = hybridCache;
        _dictRepository = dictRepository;
        _stringLocalizerFactory = stringLocalizerFactory;
    }

    /// <inheritdoc />
    [HttpGet]
    public async Task<EnumDefinitionDto> GetByNameAsync(
        [FromQuery] string enumName,
        [FromQuery] string? language = null,
        [FromQuery] bool includeHidden = false,
        [FromQuery] bool includeDict = false,
        [FromQuery] string? dictCode = null,
        [FromQuery] long? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(enumName);

        var resolvedType = ResolveEnumType(enumName.Trim());
        var culture = ResolveCulture(language);
        var normalizedDictCode = string.IsNullOrWhiteSpace(dictCode) ? null : dictCode.Trim();
        var cacheKey = BuildCacheKey(resolvedType, culture.Name, includeHidden, includeDict, normalizedDictCode, tenantId);
        var cacheOptions = includeDict ? EnumWithDictCacheOptions : EnumCacheOptions;

        return await _hybridCache.GetOrCreateAsync(
            cacheKey,
            async _ =>
            {
                EnumDefinitionDto definition;
                using (new CultureScope(culture))
                {
                    definition = BuildEnumDefinition(resolvedType, culture, includeHidden);
                }

                if (includeDict)
                {
                    await ApplyDictOverridesAsync(definition, normalizedDictCode, tenantId);
                }

                return definition;
            },
            cacheOptions,
            cancellationToken: CancellationToken.None);
    }

    /// <inheritdoc />
    [HttpPost]
    public async Task<IReadOnlyDictionary<string, EnumDefinitionDto>> GetBatchAsync([FromBody] EnumBatchQueryDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var enumNames = input.EnumNames
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (enumNames.Count == 0)
        {
            return new Dictionary<string, EnumDefinitionDto>(StringComparer.OrdinalIgnoreCase);
        }

        var dictCodes = input.DictCodes?
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => x.Trim())
            .ToList();

        var tasks = new Dictionary<string, Task<EnumDefinitionDto>>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < enumNames.Count; i++)
        {
            var enumName = enumNames[i];
            var dictCode = input.IncludeDict && dictCodes is { Count: > 0 } && i < dictCodes.Count
                ? dictCodes[i]
                : null;

            tasks[enumName] = GetByNameAsync(
                enumName,
                input.Language,
                input.IncludeHidden,
                input.IncludeDict,
                dictCode,
                input.TenantId);
        }

        await Task.WhenAll(tasks.Values);

        return tasks.ToDictionary(
            static x => x.Key,
            static x => x.Value.Result,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    [HttpGet]
    public Task<IReadOnlyList<string>> GetNamesAsync()
    {
        EnsureEnumTypeCacheInitialized();
        var result = _enumShortNameMap.Keys
            .OrderBy(static x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return Task.FromResult<IReadOnlyList<string>>(result);
    }

    private static object ParseDictValue(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
        {
            return longValue;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue))
        {
            return decimalValue;
        }

        if (bool.TryParse(value, out var boolValue))
        {
            return boolValue;
        }

        return value;
    }

    private static (string Label, string? LocalizationKey) ResolveLabel(
        IStringLocalizer localizer,
        Type enumType,
        EnumItem enumItem)
    {
        var localizationKey = ReadOptionalString(enumItem, "LocalizationKey");
        var candidates = new List<string>(capacity: 5);
        var keySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (keySet.Add(key))
            {
                candidates.Add(key);
            }
        }

        AddKey(localizationKey);
        AddKey($"{enumType.Name}.{enumItem.Key}");
        AddKey($"{enumType.Name}_{enumItem.Key}");
        AddKey(enumItem.Key);

        foreach (var key in candidates)
        {
            var localized = localizer[key];
            if (!localized.ResourceNotFound)
            {
                return (localized.Value, key);
            }
        }

        return (enumItem.Description, candidates.FirstOrDefault());
    }

    private static CultureInfo ResolveCulture(string? language)
    {
        if (!string.IsNullOrWhiteSpace(language))
        {
            try
            {
                return CultureInfo.GetCultureInfo(language);
            }
            catch (CultureNotFoundException)
            {
                // 继续走默认回退
            }
        }

        return string.IsNullOrWhiteSpace(CultureInfo.CurrentUICulture.Name)
            ? CultureInfo.GetCultureInfo("zh-CN")
            : CultureInfo.CurrentUICulture;
    }

    private static string BuildCacheKey(
        Type enumType,
        string culture,
        bool includeHidden,
        bool includeDict,
        string? dictCode,
        long? tenantId)
    {
        var normalizedDictCode = includeDict
            ? (string.IsNullOrWhiteSpace(dictCode) ? enumType.Name : dictCode.Trim())
            : "-";

        return $"enum:{enumType.FullName}:{culture}:{includeHidden}:{includeDict}:{normalizedDictCode}:{tenantId?.ToString() ?? "null"}";
    }

    private static string? ReadOptionalString(EnumItem enumItem, string propertyName)
    {
        var propertyInfo = enumItem.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return propertyInfo?.GetValue(enumItem) as string;
    }

    private static int? ReadOptionalInt(EnumItem enumItem, string propertyName)
    {
        var propertyInfo = enumItem.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (propertyInfo == null)
        {
            return null;
        }

        var value = propertyInfo.GetValue(enumItem);
        if (value == null)
        {
            return null;
        }

        try
        {
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static bool? ReadOptionalBool(EnumItem enumItem, string propertyName)
    {
        var propertyInfo = enumItem.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (propertyInfo == null)
        {
            return null;
        }

        var value = propertyInfo.GetValue(enumItem);
        if (value == null)
        {
            return null;
        }

        try
        {
            return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadExtraString(Dictionary<string, object>? extra, string key)
    {
        if (extra == null || !extra.TryGetValue(key, out var value) || value == null)
        {
            return null;
        }

        return value.ToString();
    }

    private static int? ReadExtraInt(Dictionary<string, object>? extra, string key)
    {
        if (extra == null || !extra.TryGetValue(key, out var value) || value == null)
        {
            return null;
        }

        try
        {
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static bool? ReadExtraBool(Dictionary<string, object>? extra, string key)
    {
        if (extra == null || !extra.TryGetValue(key, out var value) || value == null)
        {
            return null;
        }

        try
        {
            return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private EnumDefinitionDto BuildEnumDefinition(Type enumType, CultureInfo culture, bool includeHidden)
    {
        var enumInfo = EnumHelper.GetEnumInfo(enumType);
        var localizer = _stringLocalizerFactory
            .Create("Enums", enumType.Assembly.GetName().Name ?? enumType.Namespace ?? "XiHan.BasicApp");

        var enumItems = EnumHelper.GetEnumItems(enumType, includeHidden, ordered: true);
        var options = new List<EnumOptionDto>(enumItems.Count);
        foreach (var item in enumItems)
        {
            var (label, localizationKey) = ResolveLabel(localizer, enumType, item);
            var extra = item.Extra is null
                ? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object>(item.Extra, StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(localizationKey))
            {
                extra["LocalizationKey"] = localizationKey;
            }

            var icon = ReadOptionalString(item, "Icon")
                ?? ReadExtraString(item.Extra, "Icon");
            var order = ReadOptionalInt(item, "Order")
                ?? ReadExtraInt(item.Extra, "Order")
                ?? 0;
            var disabled = ReadOptionalBool(item, "Disabled")
                ?? ReadExtraBool(item.Extra, "Disabled")
                ?? false;

            options.Add(new EnumOptionDto
            {
                Name = item.Key,
                Value = item.Value ?? item.Key,
                ValueText = item.Value?.ToString() ?? string.Empty,
                Label = label,
                Description = item.Description,
                Theme = item.Theme,
                Icon = icon,
                Order = order,
                Disabled = disabled,
                Source = "enum",
                Extra = extra.Count == 0 ? null : extra
            });
        }

        return new EnumDefinitionDto
        {
            EnumName = enumType.Name,
            FullName = enumType.FullName ?? enumType.Name,
            DisplayName = enumInfo.Description,
            CultureName = culture.Name,
            IsFlags = enumInfo.IsFlags,
            UnderlyingTypeName = enumInfo.UnderlyingType.FullName ?? enumInfo.UnderlyingType.Name,
            Items = options.OrderBy(static x => x.Order).ThenBy(static x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray()
        };
    }

    private async Task ApplyDictOverridesAsync(EnumDefinitionDto definition, string? dictCode, long? tenantId)
    {
        var actualDictCode = string.IsNullOrWhiteSpace(dictCode) ? definition.EnumName : dictCode;
        var dict = await _dictRepository.GetByDictCodeAsync(actualDictCode, tenantId);
        if (dict is null)
        {
            return;
        }

        var dictItems = await _dictRepository.GetDictItemsAsync(dict.BasicId, tenantId);
        if (dictItems.Count == 0)
        {
            return;
        }

        var mutableItems = definition.Items.ToList();
        var byName = new Dictionary<string, EnumOptionDto>(StringComparer.OrdinalIgnoreCase);
        var byValueText = new Dictionary<string, EnumOptionDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in mutableItems)
        {
            byName.TryAdd(item.Name, item);
            if (!string.IsNullOrWhiteSpace(item.ValueText))
            {
                byValueText.TryAdd(item.ValueText, item);
            }
        }

        foreach (var dictItem in dictItems
                     .OrderBy(static x => x.Sort)
                     .ThenBy(static x => x.ItemCode, StringComparer.OrdinalIgnoreCase))
        {
            EnumOptionDto? matched = null;
            if (byName.TryGetValue(dictItem.ItemCode, out var byNameItem))
            {
                matched = byNameItem;
            }
            else if (!string.IsNullOrWhiteSpace(dictItem.ItemValue)
                     && byValueText.TryGetValue(dictItem.ItemValue, out var byValueItem))
            {
                matched = byValueItem;
            }

            if (matched != null)
            {
                matched.Label = dictItem.ItemName;
                matched.Order = dictItem.Sort;
                matched.Disabled = dictItem.Status != YesOrNo.Yes;
                matched.Source = "dict";
                matched.Extra ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                matched.Extra["DictCode"] = dict.DictCode;
                continue;
            }

            var parsedValue = ParseDictValue(dictItem.ItemValue, dictItem.ItemCode);
            mutableItems.Add(new EnumOptionDto
            {
                Name = dictItem.ItemCode,
                Value = parsedValue,
                ValueText = parsedValue.ToString() ?? string.Empty,
                Label = dictItem.ItemName,
                Description = dictItem.ItemDescription ?? dictItem.ItemName,
                Theme = null,
                Icon = null,
                Order = dictItem.Sort,
                Disabled = dictItem.Status != YesOrNo.Yes,
                Source = "dict",
                Extra = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["DictCode"] = dict.DictCode
                }
            });
        }

        definition.Items = mutableItems
            .OrderBy(static x => x.Order)
            .ThenBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private Type ResolveEnumType(string enumName)
    {
        EnsureEnumTypeCacheInitialized();

        if (_enumFullNameMap.TryGetValue(enumName, out var byFullName))
        {
            return byFullName;
        }

        if (_enumShortNameMap.TryGetValue(enumName, out var byShortName))
        {
            return byShortName;
        }

        throw new KeyNotFoundException($"未找到枚举类型：{enumName}");
    }

    private void EnsureEnumTypeCacheInitialized()
    {
        if (_enumCacheInitialized)
        {
            return;
        }

        lock (_syncLock)
        {
            if (_enumCacheInitialized)
            {
                return;
            }

            BuildEnumTypeCache();
            _enumCacheInitialized = true;
        }
    }

    private void BuildEnumTypeCache()
    {
        _enumFullNameMap.Clear();
        _enumShortNameMap.Clear();
        var duplicatedShortNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(static a =>
            {
                var assemblyName = a.GetName().Name;
                return !string.IsNullOrWhiteSpace(assemblyName)
                       && assemblyName.StartsWith("XiHan.BasicApp.", StringComparison.OrdinalIgnoreCase);
            });

        foreach (var assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(static x => x != null).Cast<Type>().ToArray();
            }
            catch
            {
                continue;
            }

            foreach (var type in types.Where(static t => t.IsEnum))
            {
                if (!string.IsNullOrWhiteSpace(type.FullName))
                {
                    _enumFullNameMap[type.FullName] = type;
                }

                if (duplicatedShortNames.Contains(type.Name))
                {
                    continue;
                }

                if (_enumShortNameMap.TryGetValue(type.Name, out var existedType)
                    && existedType != type)
                {
                    _enumShortNameMap.Remove(type.Name);
                    duplicatedShortNames.Add(type.Name);
                    continue;
                }

                _enumShortNameMap[type.Name] = type;
            }
        }
    }

    private readonly struct CultureScope : IDisposable
    {
        private readonly CultureInfo _previousCulture;
        private readonly CultureInfo _previousUiCulture;

        public CultureScope(CultureInfo culture)
        {
            _previousCulture = CultureInfo.CurrentCulture;
            _previousUiCulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _previousCulture;
            CultureInfo.CurrentUICulture = _previousUiCulture;
        }
    }
}
