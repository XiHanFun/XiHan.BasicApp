#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PositionDomainService
// Guid:8f10e720-d39a-4e6f-f2c7-708192031426
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 岗位领域服务实现
/// </summary>
public sealed class PositionDomainService
    : IPositionDomainService
{
    private readonly IPositionRepository _positionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PositionDomainService(IPositionRepository positionRepository)
    {
        _positionRepository = positionRepository;
    }

    /// <inheritdoc />
    public async Task<PositionCommandResult> CreatePositionAsync(PositionCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureEnum(command.Status, nameof(command.Status));
        var positionCode = Required(command.PositionCode, 100, nameof(command.PositionCode), "岗位编码不能超过 100 个字符。");
        EnsureCodeHasNoWhitespace(positionCode, "岗位编码不能包含空白字符。");
        if (await _positionRepository.ExistsCodeAsync(positionCode, null, cancellationToken))
        {
            throw new InvalidOperationException("岗位编码已存在。");
        }

        var position = new SysPosition
        {
            PositionCode = positionCode,
            PositionName = Required(command.PositionName, 100, nameof(command.PositionName), "岗位名称不能超过 100 个字符。"),
            Status = command.Status,
            Sort = command.Sort,
            Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。")
        };

        return new PositionCommandResult(await _positionRepository.AddAsync(position, cancellationToken));
    }

    /// <inheritdoc />
    public async Task DeletePositionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var position = await GetPositionOrThrowAsync(id, cancellationToken);
        if (!await _positionRepository.DeleteAsync(position, cancellationToken))
        {
            throw new InvalidOperationException("岗位删除失败。");
        }
    }

    /// <inheritdoc />
    public async Task<PositionCommandResult> UpdatePositionAsync(PositionUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "岗位主键必须大于 0。");
        var position = await GetPositionOrThrowAsync(command.BasicId, cancellationToken);
        position.PositionName = Required(command.PositionName, 100, nameof(command.PositionName), "岗位名称不能超过 100 个字符。");
        position.Sort = command.Sort;
        position.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");

        return new PositionCommandResult(await _positionRepository.UpdateAsync(position, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<PositionCommandResult> UpdatePositionStatusAsync(PositionStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "岗位主键必须大于 0。");
        EnsureEnum(command.Status, nameof(command.Status));

        var position = await GetPositionOrThrowAsync(command.BasicId, cancellationToken);
        position.Status = command.Status;
        position.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。") ?? position.Remark;

        return new PositionCommandResult(await _positionRepository.UpdateAsync(position, cancellationToken));
    }

    private static void EnsureCodeHasNoWhitespace(string value, string message)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void EnsureEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }

    private static string? Optional(string? value, int maxLength, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private static string Required(string? value, int maxLength, string paramName, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private async Task<SysPosition> GetPositionOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "岗位主键必须大于 0。");
        return await _positionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("岗位不存在。");
    }
}
