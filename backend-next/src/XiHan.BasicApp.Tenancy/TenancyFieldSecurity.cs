// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Operations.Editions;
using XiHan.BasicApp.Tenancy.Operations.Tenants;

namespace XiHan.BasicApp.Tenancy;

/// <summary>
/// 租户包登记的字段安全实体。
/// </summary>
/// <remarks>
/// 4.x 同两个实体：租户与套餐。字段由响应类型生成（租户的 <c>tenantId</c> 是租户代码，不按框架列排除），规则由 Access 的字段安全中间件在读、建、改处统一套用；
/// 套餐的修改按套餐本身比对（绑定租户数与权限数不参与）。
/// </remarks>
public static class TenancyFieldSecurity
{
    /// <summary>
    /// 登记本包的实体与操作。
    /// </summary>
    /// <param name="services">服务集合。</param>
    public static void Register(IServiceCollection services)
        => services.AddFieldSecurity("tenancy", TenancyJsonContext.Default, fields => fields
            .Entity<TenantResponse>("tenancy.tenants", "tenancy.tenants.title", "tenancy.tenants.fields.")
                .Include("tenantId")
                .Reads(ListTenants.Id)
                .ReadsAt(GetTenant.Id, "tenant")
                .Creates(CreateTenant.Id)
                .Updates<UpdateTenantRequest>(UpdateTenant.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysTenant>(context, request.Id, TenantResponse.From))
                .Keyword("tenantId", "name")
            .Entity<EditionResponse>("tenancy.editions", "tenancy.editions.title", "tenancy.editions.fields.")
                .ReadsAt(ListEditions.Id, "items")
                .ReadsAt(GetEdition.Id, "edition")
                .CreatesAt(CreateEdition.Id, "edition")
                .Updates<UpdateEditionRequest>(UpdateEdition.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysEdition>(context, request.Id, static edition => EditionResponse.From(edition, 0, 0)), "edition"));
}
