// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 手机号码正规化服务：把任意书写形式统一成 E.164（+国码+号码），作为存储与比对的唯一口径
/// </summary>
public interface IPhoneNumberNormalizer
{
    /// <summary>
    /// 尝试正规化（不抛异常）
    /// </summary>
    /// <param name="raw">原始输入</param>
    /// <param name="defaultRegion">无国码时按此地区解析（ISO 3166-1 两位码，如 TW / CN）</param>
    /// <param name="normalized">正规化后的 E.164；失败为空字符串</param>
    /// <returns>是否为该地区下成立的手机号码</returns>
    bool TryNormalize(string? raw, string? defaultRegion, out string normalized);

    /// <summary>
    /// 正规化，空白视为「未填写」返回 null，格式无效抛出 <see cref="InvalidOperationException"/>
    /// </summary>
    /// <param name="raw">原始输入</param>
    /// <param name="defaultRegion">无国码时按此地区解析</param>
    /// <returns>E.164 字符串；未填写时为 null</returns>
    string? NormalizeOrThrow(string? raw, string? defaultRegion);
}
