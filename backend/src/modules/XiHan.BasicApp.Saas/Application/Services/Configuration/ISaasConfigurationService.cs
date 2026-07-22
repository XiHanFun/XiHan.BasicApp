// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// SaaS 运行时配置服务。
/// </summary>
public interface ISaasConfigurationService
{
    /// <summary>
    /// 获取字符串配置。
    /// </summary>
    Task<string?> GetStringAsync(string configKey, string? defaultValue = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取布尔配置。
    /// </summary>
    Task<bool> GetBooleanAsync(string configKey, bool defaultValue = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取整型配置。
    /// </summary>
    Task<int> GetInt32Async(string configKey, int defaultValue = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取字符串列表配置。
    /// </summary>
    Task<IReadOnlyList<string>> GetStringListAsync(string configKey, IReadOnlyList<string> defaultValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取登录配置。
    /// </summary>
    Task<LoginConfigDto> GetLoginConfigAsync(CancellationToken cancellationToken = default);
}
