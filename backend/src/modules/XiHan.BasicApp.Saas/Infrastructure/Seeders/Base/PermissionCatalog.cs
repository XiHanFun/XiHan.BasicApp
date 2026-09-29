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
    /// 状态变更（启停、上下架、生命周期）
    /// </summary>
    public static readonly OperationSeed Status = new("status", "状态", OperationTypeCode.Update, OperationCategory.Business, HttpMethodType.PUT, true, false, 80);

    /// <summary>
    /// 授权
    /// </summary>
    public static readonly OperationSeed Grant = new("grant", "授权", OperationTypeCode.Grant, OperationCategory.Admin, HttpMethodType.POST, true, false, 90);

    /// <summary>
    /// 撤销（授权、成员身份、会话、委托）
    /// </summary>
    public static readonly OperationSeed Revoke = new("revoke", "撤销", OperationTypeCode.Revoke, OperationCategory.Admin, HttpMethodType.POST, true, true, 100);

    /// <summary>
    /// 审核 / 审计：同一个编码既用于审查单的审核处理（saas:review:audit），也用于聊天的合规审计（chat:audit）
    /// </summary>
    public static readonly OperationSeed Audit = new("audit", "审核/审计", OperationTypeCode.Approve, OperationCategory.Business, HttpMethodType.POST, true, false, 110);

    /// <summary>
    /// 撤回
    /// </summary>
    public static readonly OperationSeed Withdraw = new("withdraw", "撤回", OperationTypeCode.Custom, OperationCategory.Business, HttpMethodType.POST, true, false, 120);

    /// <summary>
    /// 发布
    /// </summary>
    public static readonly OperationSeed Publish = new("publish", "发布", OperationTypeCode.Custom, OperationCategory.Business, HttpMethodType.POST, true, false, 130);

    /// <summary>
    /// 发送
    /// </summary>
    public static readonly OperationSeed Send = new("send", "发送", OperationTypeCode.Custom, OperationCategory.Business, HttpMethodType.POST, true, false, 140);

    /// <summary>
    /// 使用（按编码解析并预览或打印）
    /// </summary>
    public static readonly OperationSeed Use = new("use", "使用", OperationTypeCode.Print, OperationCategory.Business, HttpMethodType.POST, true, false, 150);

    /// <summary>
    /// 管理（会话、群成员这类整体维护）
    /// </summary>
    public static readonly OperationSeed Manage = new("manage", "管理", OperationTypeCode.Custom, OperationCategory.Admin, HttpMethodType.POST, true, false, 160);

    /// <summary>
    /// 设为默认
    /// </summary>
    public static readonly OperationSeed Default = new("default", "设为默认", OperationTypeCode.Update, OperationCategory.Business, HttpMethodType.PUT, true, false, 170);

    /// <summary>
    /// 邀请
    /// </summary>
    public static readonly OperationSeed Invite = new("invite", "邀请", OperationTypeCode.Custom, OperationCategory.Business, HttpMethodType.POST, true, false, 180);

    /// <summary>
    /// 邀请状态
    /// </summary>
    public static readonly OperationSeed InviteStatus = new("invite-status", "邀请状态", OperationTypeCode.Update, OperationCategory.Business, HttpMethodType.PUT, true, false, 190);

    /// <summary>
    /// 支持人员入驻
    /// </summary>
    public static readonly OperationSeed SupportMember = new("support-member", "支持人员入驻", OperationTypeCode.Custom, OperationCategory.Admin, HttpMethodType.POST, true, false, 200);

    /// <summary>
    /// 所有权转移
    /// </summary>
    public static readonly OperationSeed TransferOwner = new("transfer-owner", "所有权转移", OperationTypeCode.Custom, OperationCategory.Admin, HttpMethodType.POST, true, true, 210);

    /// <summary>
    /// 初始化数据库
    /// </summary>
    public static readonly OperationSeed InitDb = new("initdb", "初始化数据库", OperationTypeCode.Execute, OperationCategory.System, HttpMethodType.POST, true, true, 220);

    /// <summary>
    /// 重置密码
    /// </summary>
    public static readonly OperationSeed ResetPassword = new("reset-password", "重置密码", OperationTypeCode.Custom, OperationCategory.Admin, HttpMethodType.POST, true, true, 230);

    /// <summary>
    /// 重置双因素
    /// </summary>
    public static readonly OperationSeed ResetTwoFactor = new("reset-two-factor", "重置双因素", OperationTypeCode.Custom, OperationCategory.Admin, HttpMethodType.POST, true, true, 240);

    /// <summary>
    /// 锁定 / 解锁
    /// </summary>
    public static readonly OperationSeed Lock = new("lock", "锁定", OperationTypeCode.Update, OperationCategory.Admin, HttpMethodType.PUT, true, true, 250);

    /// <summary>
    /// 登录策略
    /// </summary>
    public static readonly OperationSeed LoginPolicy = new("login-policy", "登录策略", OperationTypeCode.Update, OperationCategory.Admin, HttpMethodType.PUT, true, false, 260);

    /// <summary>
    /// 重置密钥
    /// </summary>
    public static readonly OperationSeed Secret = new("secret", "重置密钥", OperationTypeCode.Custom, OperationCategory.Admin, HttpMethodType.POST, true, true, 270);

    /// <summary>
    /// 运行状态（启动、暂停、恢复这类运行控制）
    /// </summary>
    public static readonly OperationSeed RunStatus = new("run-status", "运行状态", OperationTypeCode.Execute, OperationCategory.Business, HttpMethodType.POST, true, false, 280);

    /// <summary>
    /// 重置
    /// </summary>
    public static readonly OperationSeed Reset = new("reset", "重置", OperationTypeCode.Custom, OperationCategory.Business, HttpMethodType.POST, true, true, 290);

    /// <summary>
    /// 生成
    /// </summary>
    public static readonly OperationSeed Generate = new("generate", "生成", OperationTypeCode.Execute, OperationCategory.Business, HttpMethodType.POST, true, false, 300);

    /// <summary>
    /// 发号记录查看
    /// </summary>
    public static readonly OperationSeed AllocationRead = new("allocation-read", "发号记录查看", OperationTypeCode.Read, OperationCategory.Business, HttpMethodType.GET, false, false, 310);

    /// <summary>
    /// 全局管理（平台全局数据及其对租户的开放）
    /// </summary>
    public static readonly OperationSeed GlobalManage = new("global-manage", "全局管理", OperationTypeCode.Custom, OperationCategory.Admin, HttpMethodType.POST, true, false, 320);

    /// <summary>
    /// 清理
    /// </summary>
    public static readonly OperationSeed Clear = new("clear", "清理", OperationTypeCode.Delete, OperationCategory.System, HttpMethodType.DELETE, true, true, 330);

    /// <summary>
    /// 发起
    /// </summary>
    public static readonly OperationSeed Start = new("start", "发起", OperationTypeCode.Execute, OperationCategory.Admin, HttpMethodType.POST, true, true, 340);

    /// <summary>
    /// 跨租户
    /// </summary>
    public static readonly OperationSeed CrossTenant = new("cross-tenant", "跨租户", OperationTypeCode.Custom, OperationCategory.Admin, HttpMethodType.POST, true, true, 350);

    /// <summary>
    /// 全部操作
    /// </summary>
    public static IReadOnlyList<OperationSeed> All { get; } =
    [
        Read, Create, Update, Delete, Export, Import, Execute,
        Status, Grant, Revoke, Audit, Withdraw, Publish, Send, Use, Manage, Default, Invite, InviteStatus,
        SupportMember, TransferOwner, InitDb, ResetPassword, ResetTwoFactor, Lock, LoginPolicy, Secret,
        RunStatus, Reset, Generate, AllocationRead, GlobalManage, Clear, Start, CrossTenant,
    ];

    private static readonly Dictionary<string, OperationSeed> ByCode = All.ToDictionary(static operation => operation.Code, StringComparer.Ordinal);

    /// <summary>
    /// 按操作编码取字典里的操作（权限码的末段即操作编码）
    /// </summary>
    /// <param name="code">操作编码</param>
    /// <returns>操作种子</returns>
    /// <exception cref="InvalidOperationException">字典里没有该操作</exception>
    public static OperationSeed Get(string code)
    {
        return ByCode.TryGetValue(code, out var operation)
            ? operation
            : throw new InvalidOperationException($"操作字典里没有 {code}，先在 {nameof(OperationSeeds)} 登记再引用。");
    }
}

/// <summary>
/// 一个资源的种子声明（资源型权限挂在资源上，资源编码即权限码的资源段）
/// </summary>
/// <param name="Code">资源编码</param>
/// <param name="Name">资源名称</param>
/// <param name="Path">接口路径；一个资源横跨多个接口服务（如 SaaS 的权限分组）时留空，不硬凑一个前缀</param>
/// <param name="Description">说明</param>
/// <param name="Sort">排序</param>
public sealed record ResourceSeed(string Code, string Name, string? Path, string Description, int Sort);

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
