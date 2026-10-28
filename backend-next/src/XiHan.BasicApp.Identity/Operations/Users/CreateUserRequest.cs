// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// <c>users.create</c> 的请求。单字段约束用 DataAnnotations（由生成器折进请求 Schema，Validate 阶段按 Schema 拒绝并带字段级 <c>errors</c>）；
/// 跨字段与格式规则在 <see cref="Validate"/> 里自校验，文案按消息键取调用方语言。
/// </summary>
/// <param name="UserName">登录名：3 到 64 个字符，小写字母开头，只含小写字母、数字、<c>.</c>、<c>_</c>、<c>-</c>。</param>
/// <param name="DisplayName">显示名：1 到 64 个字符。</param>
/// <param name="Email">邮箱，可空。</param>
/// <param name="PhoneNumber">手机号，可空。</param>
/// <param name="Password">初始密码；不填则账号在管理员重置前不能登录。</param>
/// <param name="HomeTenantId">注册地租户：租户主体只能是当前租户（可不填）；平台主体不填即建平台账号。</param>
public sealed record CreateUserRequest(
    [Required, StringLength(64, MinimumLength = 3), RegularExpression("^[a-z][a-z0-9._-]*$")] string UserName,
    [Required, StringLength(64, MinimumLength = 1)] string DisplayName,
    [StringLength(256)] string? Email = null,
    [StringLength(32)] string? PhoneNumber = null,
    [StringLength(128, MinimumLength = 8)] string? Password = null,
    [RegularExpression("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$")] string? HomeTenantId = null) : IValidatableObject
{
    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            yield return new ValidationResult(IdentityMessages.DisplayNameBlank.L(), [nameof(DisplayName)]);
        }

        if (Email is { Length: > 0 } && !UserEmail.IsValid(Email))
        {
            yield return new ValidationResult(IdentityMessages.EmailInvalid.L(), [nameof(Email)]);
        }
    }
}
