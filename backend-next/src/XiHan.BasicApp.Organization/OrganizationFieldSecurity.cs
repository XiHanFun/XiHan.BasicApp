// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Organization.Operations.Positions;
using XiHan.BasicApp.Runtime.Queries;

namespace XiHan.BasicApp.Organization;

/// <summary>
/// 组织包登记的字段安全实体。
/// </summary>
/// <remarks>
/// 4.x 同：岗位（部门 4.x 也没登记）。字段由响应类型生成，规则由 Access 的字段安全中间件在读、建、改处统一套用。
/// </remarks>
public static class OrganizationFieldSecurity
{
    /// <summary>
    /// 登记本包的实体与操作。
    /// </summary>
    /// <param name="services">服务集合。</param>
    public static void Register(IServiceCollection services)
        => services.AddFieldSecurity("organization", OrganizationJsonContext.Default, fields => fields
            .Entity<PositionResponse>("organization.positions", "organization.positions.title", "organization.positions.fields.")
                .Reads(ListPositions.Id, GetPosition.Id)
                .Creates(CreatePosition.Id)
                .Updates<UpdatePositionRequest>(UpdatePosition.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysPosition>(context, request.Id, PositionResponse.From))
                .Keyword("code", "name"));
}
