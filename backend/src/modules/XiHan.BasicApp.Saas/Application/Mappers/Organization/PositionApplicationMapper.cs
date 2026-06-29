#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PositionApplicationMapper
// Guid:a132097c-f5bc-4081-14e9-920314253648
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 岗位应用层映射器
/// </summary>
public static class PositionApplicationMapper
{
    /// <summary>
    /// 映射岗位创建命令
    /// </summary>
    public static PositionCreateCommand ToCreateCommand(PositionCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new PositionCreateCommand(
            input.PositionCode,
            input.PositionName,
            input.Status,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射岗位更新命令
    /// </summary>
    public static PositionUpdateCommand ToUpdateCommand(PositionUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new PositionUpdateCommand(
            input.BasicId,
            input.PositionName,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射岗位状态命令
    /// </summary>
    public static PositionStatusChangeCommand ToStatusCommand(PositionStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new PositionStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射岗位列表项
    /// </summary>
    /// <param name="position">岗位实体</param>
    /// <returns>岗位列表项 DTO</returns>
    public static PositionListItemDto ToListItemDto(SysPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);

        return new PositionListItemDto
        {
            BasicId = position.BasicId,
            PositionCode = position.PositionCode,
            PositionName = position.PositionName,
            Status = position.Status,
            Sort = position.Sort,
            CreatedTime = position.CreatedTime,
            ModifiedTime = position.ModifiedTime
        };
    }

    /// <summary>
    /// 映射岗位详情
    /// </summary>
    /// <param name="position">岗位实体</param>
    /// <returns>岗位详情 DTO</returns>
    public static PositionDetailDto ToDetailDto(SysPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);

        return new PositionDetailDto
        {
            BasicId = position.BasicId,
            PositionCode = position.PositionCode,
            PositionName = position.PositionName,
            Status = position.Status,
            Sort = position.Sort,
            Remark = position.Remark,
            CreatedTime = position.CreatedTime,
            CreatedId = position.CreatedId,
            CreatedBy = position.CreatedBy,
            ModifiedTime = position.ModifiedTime,
            ModifiedId = position.ModifiedId,
            ModifiedBy = position.ModifiedBy
        };
    }
}
