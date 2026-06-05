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
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 当前用户字段脱敏规则下发服务。
/// 按 (用户 + 有效角色) 解析指定资源的有效 FLS 规则；前端据此对字段做脱敏渲染。
/// 无规则即不脱敏（安全降级）；同字段去重：用户级优先于角色级，其次取高 Priority。
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "字段安全")]
public sealed class MyFieldSecurityAppService
    : SaasApplicationService, IMyFieldSecurityAppService
{
    private readonly ICurrentUser _currentUser;

    private readonly IFieldLevelSecurityRepository _fieldLevelSecurityRepository;

    private readonly IResourceRepository _resourceRepository;

    private readonly IUserRoleRepository _userRoleRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MyFieldSecurityAppService(
        IFieldLevelSecurityRepository fieldLevelSecurityRepository,
        IResourceRepository resourceRepository,
        IUserRoleRepository userRoleRepository,
        ICurrentUser currentUser)
    {
        _fieldLevelSecurityRepository = fieldLevelSecurityRepository;
        _resourceRepository = resourceRepository;
        _userRoleRepository = userRoleRepository;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<List<MyFieldSecurityRuleDto>> GetMineAsync(string resourceCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resourceCode))
        {
            return [];
        }

        var userId = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");

        var resource = await _resourceRepository.GetByCodeAsync(resourceCode, cancellationToken);
        if (resource is null)
        {
            return [];
        }

        var roleIds = (await _userRoleRepository.GetValidByUserIdAsync(userId, DateTimeOffset.UtcNow, cancellationToken))
            .Select(userRole => userRole.RoleId)
            .ToHashSet();

        // 先在库内按资源+启用状态收敛（小集合），再于内存判定目标归属，规避复杂表达式翻译
        var rules = await _fieldLevelSecurityRepository.GetListAsync(
            rule => rule.ResourceId == resource.BasicId && rule.Status == EnableStatus.Enabled,
            cancellationToken);

        var applicable = rules.Where(rule =>
            (rule.TargetType == FieldSecurityTargetType.User && rule.TargetId == userId)
            || (rule.TargetType == FieldSecurityTargetType.Role && roleIds.Contains(rule.TargetId)));

        return
        [
            .. applicable
                .GroupBy(rule => rule.FieldName)
                .Select(group => group
                    .OrderByDescending(rule => rule.TargetType == FieldSecurityTargetType.User ? 1 : 0)
                    .ThenByDescending(rule => rule.Priority)
                    .First())
                .Where(rule => !rule.IsReadable || rule.MaskStrategy != FieldMaskStrategy.None)
                .Select(rule => new MyFieldSecurityRuleDto
                {
                    FieldName = rule.FieldName,
                    IsReadable = rule.IsReadable,
                    MaskStrategy = (int)rule.MaskStrategy,
                    MaskPattern = rule.MaskPattern,
                }),
        ];
    }
}
