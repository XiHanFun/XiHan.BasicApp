// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// <c>users.update</c> 的请求：修改资料（登录名与注册地不可改）。标识由路由值 <c>{id}</c> 绑定。
/// </summary>
/// <param name="Id">用户 ID。</param>
/// <param name="DisplayName">显示名。</param>
/// <param name="Email">邮箱，可空。</param>
/// <param name="PhoneNumber">手机号，可空。</param>
public sealed record UpdateUserRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string DisplayName,
    [StringLength(256)] string? Email = null,
    [StringLength(32)] string? PhoneNumber = null) : IValidatableObject
{
    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            yield return new ValidationResult(IdentityMessages.DisplayNameBlank.L(), [nameof(DisplayName)]);
        }

        if (Email is { Length: > 0 } && (Email.IndexOf('@', StringComparison.Ordinal) is var at && (at <= 0 || at == Email.Length - 1 || Email.IndexOf('@', at + 1) >= 0)))
        {
            yield return new ValidationResult(IdentityMessages.EmailInvalid.L(), [nameof(Email)]);
        }
    }
}
