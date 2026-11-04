// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// <c>users.update</c> 的请求：修改资料
/// </summary>
/// <remarks>
/// 整体覆盖（4.x 同）。登录名与注册地不可改。标识由路由值 <c>{id}</c> 绑定
/// </remarks>
/// <param name="Id">用户 ID</param>
/// <param name="DisplayName">显示名</param>
/// <param name="Email">邮箱，可空；格式在操作里按字段安全决定之后才校验（交回的脱敏值不像邮箱，但视为没改）</param>
/// <param name="PhoneNumber">手机号，可空</param>
/// <param name="RealName">真实姓名，可空</param>
/// <param name="Gender">性别</param>
/// <param name="Birthday">生日，可空</param>
/// <param name="Country">国家或地区，可空</param>
/// <param name="Remark">备注，可空</param>
public sealed record UpdateUserRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string DisplayName,
    [StringLength(256)] string? Email = null,
    [StringLength(32)] string? PhoneNumber = null,
    [StringLength(50)] string? RealName = null,
    UserGender Gender = UserGender.Unknown,
    DateTimeOffset? Birthday = null,
    [StringLength(50)] string? Country = null,
    [StringLength(500)] string? Remark = null) : IValidatableObject
{
    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            yield return new ValidationResult(IdentityMessages.DisplayNameBlank.L(), [nameof(DisplayName)]);
        }
    }
}
