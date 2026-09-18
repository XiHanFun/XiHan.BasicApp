// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 手机号码写入口径实现
/// </summary>
/// <remarks>
/// 先正规化再查重，顺序不能反：库里若存着 0912345678 与 +886912345678 两种写法，
/// 按原始字符串查重永远查不出它们是同一个号码。
/// 前端统一提交 E.164，故此处不传默认地区；带本地写法的旧数据由升级脚本处理。
/// </remarks>
public sealed class PhoneIdentityService : IPhoneIdentityService
{
    private readonly IUserRepository _userRepository;
    private readonly IPhoneNumberNormalizer _phoneNumberNormalizer;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PhoneIdentityService(IUserRepository userRepository, IPhoneNumberNormalizer phoneNumberNormalizer)
    {
        _userRepository = userRepository;
        _phoneNumberNormalizer = phoneNumberNormalizer;
    }

    /// <inheritdoc />
    public async Task<string?> ResolveForWriteAsync(string? rawPhone, long? excludeUserId, CancellationToken cancellationToken = default)
    {
        var normalized = _phoneNumberNormalizer.NormalizeOrThrow(rawPhone, defaultRegion: null);
        if (normalized is null)
        {
            return null;
        }

        if (await _userRepository.ExistsPhoneGloballyAsync(normalized, excludeUserId, cancellationToken))
        {
            throw new InvalidOperationException("手机号码已被其他账号使用。");
        }

        return normalized;
    }
}
