// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// SaaS 运行时配置服务
/// </summary>
/// <remarks>
/// 读的是参数配置（SysConfig），租户里有同键配置时取租户的，否则取平台的。
/// 同一功能的设置合成一条 JSON 配置，用 <see cref="GetJsonAsync{T}"/> 按类型读出。
/// </remarks>
public interface ISaasConfigurationService
{
    /// <summary>
    /// 获取字符串配置（未配置时返回默认值）
    /// </summary>
    Task<string?> GetStringAsync(string configKey, string? defaultValue = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按类型读取 JSON 配置（数字、布尔、数组、对象都按 JSON 解析）
    /// </summary>
    /// <remarks>未配置或值为空时返回默认值；值不是合法的对应类型直接报错，不静默退回默认值。</remarks>
    /// <typeparam name="T">设置类型</typeparam>
    /// <param name="configKey">配置键</param>
    /// <param name="defaultValue">未配置时的默认值</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<T> GetJsonAsync<T>(string configKey, T defaultValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取登录配置
    /// </summary>
    Task<LoginConfigDto> GetLoginConfigAsync(CancellationToken cancellationToken = default);
}
