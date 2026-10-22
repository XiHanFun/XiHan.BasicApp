// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authorization.Conditions;
using ConditionOperator = XiHan.BasicApp.Saas.Domain.Entities.ConditionOperator;

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 权限条件的写路径校验：保存、启用前确认这条条件生效后能正常求值
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>属性要登记过：目录里的属性（含旧名），或所绑权限码登记的数据行属性</item>
/// <item>运算符要适用于属性的值种类，条件值要能按值种类解析</item>
/// <item>值类型只决定书写格式：布尔、数字两种值类型只能配同种类的属性；字符串、JSON、数组按条件值本身解析</item>
/// </list>
/// 存量里不合规的条件不改写，但改不了、也重新启用不了，直到改对为止。
/// </remarks>
public sealed class PermissionConditionValidator : IPermissionConditionRuleChecker
{
    private readonly SaasConditionSetCompiler _compiler;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IUserPermissionRepository _userPermissionRepository;
    private readonly IPermissionRepository _permissionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionConditionValidator(
        SaasConditionSetCompiler compiler,
        IRolePermissionRepository rolePermissionRepository,
        IUserPermissionRepository userPermissionRepository,
        IPermissionRepository permissionRepository)
    {
        _compiler = compiler;
        _rolePermissionRepository = rolePermissionRepository;
        _userPermissionRepository = userPermissionRepository;
        _permissionRepository = permissionRepository;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">属性未登记、运算符不适用、值解析失败或值类型与属性不符</exception>
    public async Task ValidateAsync(
        long? rolePermissionId,
        long? userPermissionId,
        string attributeName,
        ConditionOperator conditionOperator,
        bool isNegated,
        ConfigDataType valueType,
        string conditionValue,
        CancellationToken cancellationToken = default)
    {
        var permissionCode = await ResolvePermissionCodeAsync(rolePermissionId, userPermissionId, cancellationToken);
        var compiled = _compiler.CompileClauses(permissionCode,
        [
            new AuthorizationConditionClause
            {
                AttributeName = attributeName.Trim(),
                Operator = (int)conditionOperator,
                IsNegated = isNegated,
                Value = conditionValue.Trim()
            }
        ]);
        if (compiled.Errors.Count > 0)
        {
            throw new InvalidOperationException($"条件无法求值：{compiled.Errors[0].Message}");
        }

        var kind = compiled.Groups[0].Clauses[0].Resolution!.Descriptor.Kind;
        var mismatched = valueType switch
        {
            ConfigDataType.Boolean => kind != ConditionValueKind.Boolean,
            ConfigDataType.Number => kind != ConditionValueKind.Number,
            _ => false
        };
        if (mismatched)
        {
            throw new InvalidOperationException($"值类型 {valueType} 与属性 {attributeName.Trim()} 的值种类 {kind} 不符。");
        }
    }

    private async Task<string> ResolvePermissionCodeAsync(long? rolePermissionId, long? userPermissionId, CancellationToken cancellationToken)
    {
        long? permissionId = null;
        if (rolePermissionId is > 0)
        {
            permissionId = (await _rolePermissionRepository.GetByIdAsync(rolePermissionId.Value, cancellationToken))?.PermissionId;
        }
        else if (userPermissionId is > 0)
        {
            permissionId = (await _userPermissionRepository.GetByIdAsync(userPermissionId.Value, cancellationToken))?.PermissionId;
        }

        // 绑定不存在由领域服务报错；这里只缺数据行属性的登记
        return permissionId is > 0
            ? (await _permissionRepository.GetByIdAsync(permissionId.Value, cancellationToken))?.PermissionCode ?? string.Empty
            : string.Empty;
    }
}
