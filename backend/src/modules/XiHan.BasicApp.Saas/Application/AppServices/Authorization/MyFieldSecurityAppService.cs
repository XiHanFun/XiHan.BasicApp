#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MyFieldSecurityAppService
// Guid:d394567a-1325-4162-9e90-8fa34f7c20d9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/05 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Application.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 当前用户字段权限下发服务。
/// 复用 <see cref="IFieldSecurityService"/> 的 deny-overrides 解析，向前端下发字段「可读/可编辑/脱敏」信息。
/// 脱敏已由服务端在响应里落地，此处主要供前端表单据 IsEditable 置只读、必要时展示脱敏标识。
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "字段安全")]
public sealed class MyFieldSecurityAppService
    : SaasApplicationService, IMyFieldSecurityAppService
{
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MyFieldSecurityAppService(IFieldSecurityService fieldSecurity)
    {
        _fieldSecurity = fieldSecurity;
    }

    /// <inheritdoc />
    public async Task<List<MyFieldSecurityRuleDto>> GetMineAsync(string resourceCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resourceCode))
        {
            return [];
        }

        var rules = await _fieldSecurity.ResolveAsync(resourceCode, cancellationToken);
        return
        [
            .. rules.Values
                .Where(rule => !rule.IsReadable || !rule.IsEditable || rule.MaskStrategy != FieldMaskStrategy.None)
                .Select(rule => new MyFieldSecurityRuleDto
                {
                    FieldName = rule.FieldName,
                    IsReadable = rule.IsReadable,
                    IsEditable = rule.IsEditable,
                    MaskStrategy = (int)rule.MaskStrategy,
                    MaskPattern = rule.MaskPattern,
                }),
        ];
    }
}
