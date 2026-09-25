// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// 一个操作的种子声明（资源型权限的动作维度）
/// </summary>
/// <param name="Code">操作编码</param>
/// <param name="Name">操作名称</param>
/// <param name="Type">操作类型</param>
/// <param name="Category">操作分类</param>
/// <param name="HttpMethod">对应的 HTTP 方法</param>
/// <param name="IsRequireAudit">是否需要审计</param>
/// <param name="IsDangerous">是否危险操作</param>
/// <param name="Sort">排序</param>
public sealed record OperationSeed(
    string Code,
    string Name,
    OperationTypeCode Type,
    OperationCategory Category,
    HttpMethodType HttpMethod,
    bool IsRequireAudit,
    bool IsDangerous,
    int Sort);

/// <summary>
/// 平台操作字典：资源型权限的动作都从这里取，由 <see cref="SaasOperationSeeder"/> 落库
/// </summary>
public static class OperationSeeds
{
    /// <summary>
    /// 读取
    /// </summary>
    public static readonly OperationSeed Read = new("read", "读取", OperationTypeCode.Read, OperationCategory.Crud, HttpMethodType.GET, false, false, 10);

    /// <summary>
    /// 创建
    /// </summary>
    public static readonly OperationSeed Create = new("create", "创建", OperationTypeCode.Create, OperationCategory.Crud, HttpMethodType.POST, true, false, 20);

    /// <summary>
    /// 更新
    /// </summary>
    public static readonly OperationSeed Update = new("update", "更新", OperationTypeCode.Update, OperationCategory.Crud, HttpMethodType.PUT, true, false, 30);

    /// <summary>
    /// 删除
    /// </summary>
    public static readonly OperationSeed Delete = new("delete", "删除", OperationTypeCode.Delete, OperationCategory.Crud, HttpMethodType.DELETE, true, true, 40);

    /// <summary>
    /// 导出
    /// </summary>
    public static readonly OperationSeed Export = new("export", "导出", OperationTypeCode.Export, OperationCategory.Business, HttpMethodType.GET, false, false, 50);

    /// <summary>
    /// 导入
    /// </summary>
    public static readonly OperationSeed Import = new("import", "导入", OperationTypeCode.Import, OperationCategory.Business, HttpMethodType.POST, true, false, 60);

    /// <summary>
    /// 执行
    /// </summary>
    public static readonly OperationSeed Execute = new("execute", "执行", OperationTypeCode.Execute, OperationCategory.Business, HttpMethodType.POST, true, false, 70);

    /// <summary>
    /// 全部操作
    /// </summary>
    public static IReadOnlyList<OperationSeed> All { get; } = [Read, Create, Update, Delete, Export, Import, Execute];
}

/// <summary>
/// 一个资源的种子声明（资源型权限挂在资源上，资源编码即权限码的资源段）
/// </summary>
/// <param name="Code">资源编码</param>
/// <param name="Name">资源名称</param>
/// <param name="Path">接口路径</param>
/// <param name="Description">说明</param>
/// <param name="Sort">排序</param>
public sealed record ResourceSeed(string Code, string Name, string Path, string Description, int Sort);

/// <summary>
/// 一条权限的种子声明
/// </summary>
/// <param name="Code">权限码</param>
/// <param name="Name">权限名称</param>
/// <param name="Description">说明</param>
/// <param name="Group">分组编码（落到标签，权限页据此归类）</param>
/// <param name="Side">作用侧：权限在平台、租户还是两侧生效</param>
/// <param name="IsRequireAudit">是否需要审计</param>
/// <param name="Sort">排序（同时作为优先级）</param>
/// <param name="Resource">资源型权限所属的资源；功能权限为空</param>
/// <param name="Operation">资源型权限的操作；功能权限为空</param>
public sealed record PermissionSeed(
    string Code,
    string Name,
    string Description,
    string Group,
    PermissionSide Side,
    bool IsRequireAudit,
    int Sort,
    ResourceSeed? Resource = null,
    OperationSeed? Operation = null)
{
    /// <summary>
    /// 资源型权限：码为「资源:操作」，名称、说明、是否审计随操作
    /// </summary>
    public static PermissionSeed Of(ResourceSeed resource, OperationSeed operation, PermissionSide side, int sort)
    {
        return new PermissionSeed(
            $"{resource.Code}:{operation.Code}",
            $"{resource.Name}-{operation.Name}",
            $"对{resource.Name}执行{operation.Name}操作",
            resource.Code,
            side,
            operation.IsRequireAudit,
            sort,
            resource,
            operation);
    }

    /// <summary>
    /// 一个资源的一组操作：按操作顺序从起始排序号起依次编号
    /// </summary>
    public static IEnumerable<PermissionSeed> Of(ResourceSeed resource, PermissionSide side, int firstSort, params OperationSeed[] operations)
    {
        return operations.Select((operation, index) => Of(resource, operation, side, firstSort + index));
    }
}
