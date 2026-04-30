#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OperationAppService
// Guid:5359fc91-d364-4f64-9f7d-4e00c3b32712
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 操作定义命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "操作定义")]
public sealed class OperationAppService(
    IOperationRepository operationRepository,
    IPermissionRepository permissionRepository)
    : SaasApplicationService, IOperationAppService
{
    /// <summary>
    /// 操作仓储
    /// </summary>
    private readonly IOperationRepository _operationRepository = operationRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    /// <summary>
    /// 创建操作定义
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Operation.Create)]
    public async Task<OperationDetailDto> CreateOperationAsync(OperationCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateInput(input);

        var operationCode = input.OperationCode.Trim();
        if (await _operationRepository.GetByCodeAsync(operationCode, cancellationToken) is not null)
        {
            throw new InvalidOperationException("操作编码已存在。");
        }

        var operation = new SysOperation
        {
            OperationCode = operationCode,
            OperationName = input.OperationName.Trim(),
            OperationTypeCode = input.OperationTypeCode,
            Category = input.Category,
            HttpMethod = input.HttpMethod,
            Description = NormalizeNullable(input.Description),
            Icon = NormalizeNullable(input.Icon),
            Color = NormalizeNullable(input.Color),
            IsDangerous = input.IsDangerous,
            IsRequireAudit = input.IsRequireAudit,
            IsGlobal = false,
            Status = input.Status,
            Sort = input.Sort,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedOperation = await _operationRepository.AddAsync(operation, cancellationToken);
        return OperationApplicationMapper.ToDetailDto(savedOperation);
    }

    /// <summary>
    /// 更新操作定义
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Operation.Update)]
    public async Task<OperationDetailDto> UpdateOperationAsync(OperationUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);

        var operation = await GetEditableOperationOrThrowAsync(input.BasicId, cancellationToken);
        operation.OperationName = input.OperationName.Trim();
        operation.OperationTypeCode = input.OperationTypeCode;
        operation.Category = input.Category;
        operation.HttpMethod = input.HttpMethod;
        operation.Description = NormalizeNullable(input.Description);
        operation.Icon = NormalizeNullable(input.Icon);
        operation.Color = NormalizeNullable(input.Color);
        operation.IsDangerous = input.IsDangerous;
        operation.IsRequireAudit = input.IsRequireAudit;
        operation.Sort = input.Sort;
        operation.Remark = NormalizeNullable(input.Remark);

        var savedOperation = await _operationRepository.UpdateAsync(operation, cancellationToken);
        return OperationApplicationMapper.ToDetailDto(savedOperation);
    }

    /// <summary>
    /// 更新操作定义状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Operation.Status)]
    public async Task<OperationDetailDto> UpdateOperationStatusAsync(OperationStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "操作主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var operation = await GetEditableOperationOrThrowAsync(input.BasicId, cancellationToken);
        operation.Status = input.Status;
        operation.Remark = NormalizeNullable(input.Remark) ?? operation.Remark;

        var savedOperation = await _operationRepository.UpdateAsync(operation, cancellationToken);
        return OperationApplicationMapper.ToDetailDto(savedOperation);
    }

    /// <summary>
    /// 删除操作定义
    /// </summary>
    /// <param name="id">操作主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Operation.Delete)]
    public async Task DeleteOperationAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var operation = await GetEditableOperationOrThrowAsync(id, cancellationToken);
        await EnsureOperationNotReferencedAsync(operation.BasicId, cancellationToken);

        if (!await _operationRepository.DeleteAsync(operation, cancellationToken))
        {
            throw new InvalidOperationException("操作定义删除失败。");
        }
    }

    /// <summary>
    /// 获取可维护操作定义
    /// </summary>
    private async Task<SysOperation> GetEditableOperationOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "操作主键必须大于 0。");
        }

        var operation = await _operationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("操作定义不存在。");

        if (operation.IsGlobal)
        {
            throw new InvalidOperationException("平台级全局操作必须通过平台运维流程维护。");
        }

        return operation;
    }

    /// <summary>
    /// 校验操作未被引用
    /// </summary>
    private async Task EnsureOperationNotReferencedAsync(long operationId, CancellationToken cancellationToken)
    {
        if (await _permissionRepository.AnyAsync(permission => permission.OperationId == operationId, cancellationToken))
        {
            throw new InvalidOperationException("操作已被权限定义引用，不能删除。");
        }
    }

    /// <summary>
    /// 校验创建参数
    /// </summary>
    private static void ValidateCreateInput(OperationCreateDto input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.OperationCode);
        ValidateOperationCode(input.OperationCode);
        ValidateCommonInput(
            input.OperationName,
            input.OperationTypeCode,
            input.Category,
            input.HttpMethod,
            input.Description,
            input.Icon,
            input.Color,
            input.Remark);
        ValidateEnum(input.Status, nameof(input.Status));
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    private static void ValidateUpdateInput(OperationUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "操作主键必须大于 0。");
        }

        ValidateCommonInput(
            input.OperationName,
            input.OperationTypeCode,
            input.Category,
            input.HttpMethod,
            input.Description,
            input.Icon,
            input.Color,
            input.Remark);
    }

    /// <summary>
    /// 校验通用参数
    /// </summary>
    private static void ValidateCommonInput(
        string operationName,
        OperationTypeCode operationTypeCode,
        OperationCategory category,
        HttpMethodType? httpMethod,
        string? description,
        string? icon,
        string? color,
        string? remark)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ValidateEnum(operationTypeCode, nameof(operationTypeCode));
        ValidateEnum(category, nameof(category));
        if (httpMethod.HasValue)
        {
            ValidateEnum(httpMethod.Value, nameof(httpMethod));
        }

        ValidateLength(operationName, 100, nameof(operationName), "操作名称不能超过 100 个字符。");
        ValidateOptionalLength(description, 500, nameof(description), "操作描述不能超过 500 个字符。");
        ValidateOptionalLength(icon, 100, nameof(icon), "操作图标不能超过 100 个字符。");
        ValidateOptionalLength(color, 20, nameof(color), "操作颜色不能超过 20 个字符。");
        ValidateOptionalLength(remark, 500, nameof(remark), "备注不能超过 500 个字符。");
    }

    /// <summary>
    /// 校验操作编码
    /// </summary>
    private static void ValidateOperationCode(string operationCode)
    {
        var normalizedOperationCode = operationCode.Trim();
        ValidateLength(normalizedOperationCode, 50, nameof(operationCode), "操作编码不能超过 50 个字符。");
        if (normalizedOperationCode.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException("操作编码不能包含空白字符。");
        }

        if (normalizedOperationCode.Any(static code => !IsValidOperationCodeChar(code)))
        {
            throw new InvalidOperationException("操作编码只能包含小写英文、数字、连字符或下划线。");
        }
    }

    /// <summary>
    /// 判断操作编码字符是否合法
    /// </summary>
    private static bool IsValidOperationCodeChar(char code)
    {
        return code is >= 'a' and <= 'z'
            || code is >= '0' and <= '9'
            || code is '-' or '_';
    }

    /// <summary>
    /// 校验枚举值
    /// </summary>
    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    /// <summary>
    /// 校验字符串长度
    /// </summary>
    private static void ValidateLength(string value, int maxLength, string paramName, string message)
    {
        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 校验可空字符串长度
    /// </summary>
    private static void ValidateOptionalLength(string? value, int maxLength, string paramName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 规范化可空字符串
    /// </summary>
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
