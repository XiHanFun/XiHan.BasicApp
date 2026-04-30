#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ExternalLoginApplicationMapper
// Guid:93ff2a7b-27cc-4810-9917-8cdaff70bd5f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 第三方登录绑定应用层映射器
/// </summary>
public static class ExternalLoginApplicationMapper
{
    /// <summary>
    /// 映射第三方登录绑定列表项
    /// </summary>
    /// <param name="externalLogin">第三方登录绑定实体</param>
    /// <param name="user">用户实体</param>
    /// <returns>第三方登录绑定列表项 DTO</returns>
    public static ExternalLoginListItemDto ToListItemDto(SysExternalLogin externalLogin, SysUser? user)
    {
        ArgumentNullException.ThrowIfNull(externalLogin);

        return new ExternalLoginListItemDto
        {
            BasicId = externalLogin.BasicId,
            UserId = externalLogin.UserId,
            UserName = user?.UserName,
            RealName = user?.RealName,
            NickName = user?.NickName,
            Provider = externalLogin.Provider,
            ExternalAccountMasked = MaskExternalIdentifier(externalLogin.ProviderKey),
            ProviderDisplayName = externalLogin.ProviderDisplayName,
            ExternalEmailMasked = MaskEmail(externalLogin.Email),
            HasAvatar = !string.IsNullOrWhiteSpace(externalLogin.AvatarUrl),
            LastLoginTime = externalLogin.LastLoginTime,
            CreatedTime = externalLogin.CreatedTime,
            ModifiedTime = externalLogin.ModifiedTime
        };
    }

    /// <summary>
    /// 映射第三方登录绑定详情
    /// </summary>
    /// <param name="externalLogin">第三方登录绑定实体</param>
    /// <param name="user">用户实体</param>
    /// <returns>第三方登录绑定详情 DTO</returns>
    public static ExternalLoginDetailDto ToDetailDto(SysExternalLogin externalLogin, SysUser? user)
    {
        ArgumentNullException.ThrowIfNull(externalLogin);

        var item = ToListItemDto(externalLogin, user);
        return new ExternalLoginDetailDto
        {
            BasicId = item.BasicId,
            UserId = item.UserId,
            UserName = item.UserName,
            RealName = item.RealName,
            NickName = item.NickName,
            Provider = item.Provider,
            ExternalAccountMasked = item.ExternalAccountMasked,
            ProviderDisplayName = item.ProviderDisplayName,
            ExternalEmailMasked = item.ExternalEmailMasked,
            HasAvatar = item.HasAvatar,
            LastLoginTime = item.LastLoginTime,
            CreatedTime = item.CreatedTime,
            CreatedId = externalLogin.CreatedId,
            CreatedBy = externalLogin.CreatedBy,
            ModifiedTime = item.ModifiedTime,
            ModifiedId = externalLogin.ModifiedId,
            ModifiedBy = externalLogin.ModifiedBy
        };
    }

    /// <summary>
    /// 脱敏外部账号标识
    /// </summary>
    private static string MaskExternalIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= 8
            ? new string('*', value.Length)
            : string.Concat(value.AsSpan(0, 4), "****", value.AsSpan(value.Length - 4, 4));
    }

    /// <summary>
    /// 脱敏邮箱
    /// </summary>
    private static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var atIndex = email.IndexOf('@', StringComparison.Ordinal);
        if (atIndex <= 0)
        {
            return "***";
        }

        var local = email[..atIndex];
        var domain = email[(atIndex + 1)..];
        var localMasked = local.Length <= 2
            ? string.Concat(local[0], "***")
            : string.Concat(local.AsSpan(0, 2), "***");

        return string.Concat(localMasked, "@", domain);
    }
}
