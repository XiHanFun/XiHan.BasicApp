// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;


using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 应用服务层结构约束测试（反射 + IL 扫描）。
/// </summary>
/// <remarks>
/// 应用服务是动态 API 的暴露面，它上面挂的每一个特性都是运行期才生效的"约定"，
/// 漏挂不会有任何编译告警：漏 <c>[PermissionAuthorize]</c> = 接口裸奔，
/// 漏 <c>[UnitOfWork]</c> = 多表写操作各写各的、失败不回滚，
/// 漏缓存失效 = 改完配置/权限半天不生效（源码注释里写着的
/// "修复此前 InvalidateConfiguration 无调用点的失效空转"就是这么来的）。
/// <para>
/// 缓存失效那条无法靠特性判断，本类改为直接反编译方法体 IL：async 方法自动跟进其状态机
/// <c>MoveNext</c>，并在同一类型内做有限深度的调用链展开，判断是否真的调到了
/// <see cref="ISaasCacheInvalidator"/>。
/// </para>
/// </remarks>
public sealed class SaasAppServiceStructureTests
{
    /// <summary>
    /// 写操作方法名前缀（应用服务层的命令方法命名约定）。
    /// </summary>
    private static readonly string[] WritePrefixes =
    [
        "Create", "Update", "Delete", "Remove", "Add", "Batch", "Assign", "Grant", "Revoke",
        "Reset", "Rotate", "Regenerate", "Set", "Publish", "Withdraw", "Approve", "Reject",
        "Audit", "Cancel", "Submit", "Confirm", "Switch", "Save", "Mark", "Unlink", "Deactivate",
        "Enable", "Disable", "Initialize", "Verify", "Consume", "Upload", "FastUpload", "Run", "Generate"
    ];

    /// <summary>
    /// 允许不带 <c>[UnitOfWork]</c> 的写方法（已核对过的既有例外，各有明确理由）。
    /// </summary>
    /// <remarks>
    /// 名单是"当前事实快照"：新增的写方法只要漏挂 <c>[UnitOfWork]</c> 就会落在名单外而变红。
    /// </remarks>
    private static readonly HashSet<string> WritesWithoutUnitOfWorkAttribute = new(StringComparer.Ordinal)
    {
        // 自行注入 IUnitOfWorkManager 手工管事务（提交后才允许消息入队，避免任务先于数据可见）
        "ExportTaskAppService.SubmitAsync",
        "ExportTaskAppService.CancelAsync",
        "ExportTaskAppService.DeleteAsync",
        // 日志型追加：单行只写不改，无需事务边界
        "ImportHistoryAppService.CreateAsync",
        // 发号走独立事务/独立并发控制，套外层事务反而会拉长持锁时间
        "NumberingAppService.GenerateNumberAsync",
        "NumberingAppService.GenerateNumberBatchAsync",
        // 触发型：只是把执行请求投给调度器，本身不落业务数据
        "TaskAppService.RunTaskAsync",
        "NotificationAppService.RemindAsync",
        // 用户设置保存后立即失效缓存，不与其它表同事务
        "UserSettingAppService.SaveAsync"
    };

    /// <summary>
    /// 缓存失效必须"当场执行"的应用服务。
    /// </summary>
    /// <remarks>
    /// 这些是定义型/授权型资源（配置、字典、菜单、组织、权限、角色、版本、模板）：
    /// 它们的读侧全部走分布式缓存，写完不清缓存就等于改动不生效。
    /// <para>
    /// 身份/租户/个人中心四个服务不在此列——它们有相当一部分失效是通过领域事件
    /// （<c>UserSessionRevokedDomainEvent</c> / <c>AuthorizationChangedDomainEvent</c> 等）
    /// 转交给事件处理器完成的，故不能按"方法体内必须直接调失效器"来卡。
    /// </para>
    /// </remarks>
    private static readonly HashSet<string> ServicesInvalidatingInline = new(StringComparer.Ordinal)
    {
        "ConfigAppService",
        "DictAppService",
        "MenuAppService",
        "DepartmentAppService",
        "PositionAppService",
        "OperationAppService",
        "PermissionAppService",
        "PermissionDelegationAppService",
        "PermissionRequestAppService",
        "ResourceAppService",
        "RoleAppService",
        "UserRoleAppService",
        "UserPermissionAppService",
        "MessageTemplateAppService",
        "TenantEditionAppService"
    };

    /// <summary>
    /// 应用服务必须能被发现，否则后续结构断言全是空跑。
    /// </summary>
    [Fact]
    public void AppServices_ShouldBeDiscoverable()
    {
        Assert.True(AppServiceTypes().Count > 30, $"只发现了 {AppServiceTypes().Count} 个应用服务，扫描条件可能失效了。");
    }

    /// <summary>
    /// 应用服务一律 sealed：它们是编排端点，不设计成可继承。
    /// </summary>
    [Fact]
    public void AppServices_ShouldBeSealed()
    {
        var offenders = AppServiceTypes().Where(type => !type.IsSealed).Select(type => type.Name).ToList();

        Assert.True(offenders.Count == 0, $"以下应用服务未声明为 sealed：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 动态 API 暴露面必须带 <c>[DynamicApi]</c> 且归入统一分组，否则接口不会被注册出来。
    /// </summary>
    [Fact]
    public void AppServices_ShouldCarryDynamicApiInTheSaasGroup()
    {
        var offenders = AppServiceTypes()
            .Select(type => (type.Name, Attribute: type.GetCustomAttributes<DynamicApiAttribute>(inherit: true).FirstOrDefault()))
            .Where(item => item.Attribute is null || !string.Equals(item.Attribute.Group, "BasicApp.Saas", StringComparison.Ordinal))
            .Select(item => $"{item.Name}(Group={item.Attribute?.Group ?? "缺失"})")
            .ToList();

        Assert.True(offenders.Count == 0, $"以下应用服务未挂到 BasicApp.Saas 动态 API 分组：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 动态 API 暴露面必须处于鉴权之下：类型上（含基类）必须能取到 <c>[Authorize]</c>。
    /// </summary>
    /// <remarks>
    /// 这是整套权限体系的兜底闸门：类上没有 <c>[Authorize]</c>，方法上的
    /// <c>[PermissionAuthorize]</c> 之外的所有方法都会变成匿名可调。
    /// </remarks>
    [Fact]
    public void AppServices_ShouldBeAuthorizedByDefault()
    {
        var offenders = AppServiceTypes()
            .Where(type => !type.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any())
            .Select(type => type.Name)
            .ToList();

        Assert.True(offenders.Count == 0, $"以下应用服务未处于 [Authorize] 之下，接口会裸奔：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 每个应用服务都必须实现同名契约接口 <c>I{类型名}</c>，动态 API 与前端按契约对接。
    /// </summary>
    [Fact]
    public void AppServices_ShouldImplementTheirNamedContract()
    {
        var offenders = AppServiceTypes()
            .Where(type => !type.GetInterfaces().Any(contract =>
                string.Equals(contract.Name, "I" + type.Name, StringComparison.Ordinal)))
            .Select(type => type.Name)
            .ToList();

        Assert.True(offenders.Count == 0, $"以下应用服务缺少同名契约接口：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 公开方法一律异步：返回 <c>Task</c> 系并以 Async 结尾。
    /// </summary>
    [Fact]
    public void AppServiceMethods_ShouldBeAsyncAndNamedAsync()
    {
        var offenders = PublicMethods()
            .Where(item => !item.Method.Name.EndsWith("Async", StringComparison.Ordinal)
                           || !typeof(Task).IsAssignableFrom(UnwrapReturnType(item.Method)))
            .Select(item => $"{item.Type.Name}.{item.Method.Name}")
            .ToList();

        Assert.True(offenders.Count == 0, $"以下应用服务方法不是 Async 命名或不返回 Task：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 公开方法必须以带默认值的 <c>CancellationToken</c> 结尾，保证取消能一路透传到仓储。
    /// </summary>
    [Fact]
    public void AppServiceMethods_ShouldEndWithOptionalCancellationToken()
    {
        var offenders = PublicMethods()
            .Select(item => (item.Type, item.Method, Parameters: item.Method.GetParameters()))
            .Where(item => item.Parameters.Length == 0
                           || item.Parameters[^1].ParameterType != typeof(CancellationToken)
                           || !item.Parameters[^1].HasDefaultValue)
            .Select(item => $"{item.Type.Name}.{item.Method.Name}")
            .ToList();

        Assert.True(offenders.Count == 0, $"以下方法缺少带默认值的尾置 CancellationToken：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 方法上声明的权限码必须是会被种子播下去的真实权限，否则该接口永远无人可调。
    /// </summary>
    [Fact]
    public void PermissionAuthorizeCodes_ShouldBeSeededPermissions()
    {
        var seeded = SaasPermissionDefinitions.All
            .Select(definition => definition.PermissionCode)
            .ToHashSet(StringComparer.Ordinal);

        var offenders = PublicMethods()
            .SelectMany(item => item.Method
                .GetCustomAttributes<PermissionAuthorizeAttribute>(inherit: true)
                .Select(attribute => (item.Type, item.Method, attribute.PermissionCode)))
            .Where(item => !seeded.Contains(item.PermissionCode))
            .Select(item => $"{item.Type.Name}.{item.Method.Name}={item.PermissionCode}")
            .ToList();

        Assert.True(offenders.Count == 0, $"以下方法引用了不会被播种的权限码：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 权限码必须小写且遵循 <c>saas:资源:动作</c> 三段式，避免出现只在某处大小写不同的"影子权限"。
    /// </summary>
    [Fact]
    public void PermissionAuthorizeCodes_ShouldBeLowerCaseThreeSegments()
    {
        var offenders = PublicMethods()
            .SelectMany(item => item.Method
                .GetCustomAttributes<PermissionAuthorizeAttribute>(inherit: true)
                .Select(attribute => (item.Type, item.Method, attribute.PermissionCode)))
            .Where(item => item.PermissionCode.Split(':').Length != 3
                           || !string.Equals(item.PermissionCode, item.PermissionCode.ToLowerInvariant(), StringComparison.Ordinal))
            .Select(item => $"{item.Type.Name}.{item.Method.Name}={item.PermissionCode}")
            .ToList();

        Assert.True(offenders.Count == 0, $"以下权限码不符合小写三段式：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 写操作必须落在事务里：命令方法要么带 <c>[UnitOfWork]</c>，要么自行注入
    /// <c>IUnitOfWorkManager</c> 手工管事务，二者必居其一。
    /// </summary>
    [Fact]
    public void WriteMethods_ShouldRunInsideAUnitOfWork()
    {
        var offenders = PublicMethods()
            .Where(item => IsWriteMethod(item.Method))
            .Where(item => !item.Method.GetCustomAttributes<UnitOfWorkAttribute>(inherit: true).Any())
            .Where(item => !ManagesUnitOfWorkManually(item.Type))
            .Where(item => !WritesWithoutUnitOfWorkAttribute.Contains($"{item.Type.Name}.{item.Method.Name}"))
            .Select(item => $"{item.Type.Name}.{item.Method.Name}")
            .ToList();

        Assert.True(offenders.Count == 0, $"以下写方法既没有 [UnitOfWork] 也没有手工事务：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// <c>[UnitOfWork]</c> 必须显式表态，不能只写一个裸特性。
    /// </summary>
    /// <remarks>
    /// 这是一处很隐蔽的语义陷阱：<c>UnitOfWorkAttribute</c> 的 <c>IsTransactional</c> 是可空的，
    /// 不传值就不覆盖选项，而 <c>XiHanUnitOfWorkOptions</c> 的默认值是 <b>非事务</b>。
    /// 也就是说裸 <c>[UnitOfWork]</c> 读起来像"开事务"，实际跑起来<b>不开事务</b>。
    /// 合法写法只有三种：<c>[UnitOfWork(true)]</c>、<c>[UnitOfWork(false)]</c>、
    /// <c>[UnitOfWork(IsDisabled = true)]</c>——后两种都必须在注释里写明为什么不要事务。
    /// <para>
    /// 名单是当前事实快照：新写的裸特性会落在名单外而变红。
    /// </para>
    /// <para>
    /// 回归锚点：名单曾登记过 <c>FileAppService.DownloadFileAsync</c> 与
    /// <c>FileAppService.GenerateFilePresignedUrlAsync</c> 两处裸特性，二者都夹带了一次计数写入。
    /// 它们已显式改成 <c>[UnitOfWork(true)]</c>（下载/取链失败时那次计数一并回滚），名单随之清空——
    /// 名单为空正是本条约定的达成状态，任何人写回裸特性都会立刻变红。
    /// </para>
    /// </remarks>
    [Fact]
    public void UnitOfWorkAttributes_ShouldDeclareTransactionalityExplicitly()
    {
        var known = new HashSet<string>(StringComparer.Ordinal);

        var offenders = PublicMethods()
            .Select(item => (item.Type, item.Method, Attribute: item.Method.GetCustomAttributes<UnitOfWorkAttribute>(inherit: true).FirstOrDefault()))
            .Where(item => item.Attribute is not null && !item.Attribute.IsTransactional.HasValue && !item.Attribute.IsDisabled)
            .Select(item => $"{item.Type.Name}.{item.Method.Name}")
            .Where(name => !known.Contains(name))
            .ToList();

        Assert.True(offenders.Count == 0, $"以下方法写了裸 [UnitOfWork]（实际不开事务，极易误读）：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 显式关闭事务的写路径必须真的是"不能进事务"的那几处，防止有人靠关事务绕过写冲突。
    /// </summary>
    /// <remarks>
    /// 目前只有两类：匿名端点（外层没有 UoW 中间件预留的工作单元）与租户建库（DDL 不能在事务内跑）。
    /// </remarks>
    [Fact]
    public void NonTransactionalWrites_ShouldStayLimitedToKnownCases()
    {
        var known = new HashSet<string>(StringComparer.Ordinal)
        {
            "AuthAppService.ExternalLoginAsync",
            "AuthAppService.CreateOAuthBindTicketAsync",
            "TenantAppService.InitializeDatabaseAsync"
        };

        var offenders = PublicMethods()
            .Select(item => (item.Type, item.Method, Attribute: item.Method.GetCustomAttributes<UnitOfWorkAttribute>(inherit: true).FirstOrDefault()))
            .Where(item => item.Attribute is not null && (item.Attribute.IsDisabled || item.Attribute.IsTransactional == false))
            .Select(item => $"{item.Type.Name}.{item.Method.Name}")
            .Where(name => !known.Contains(name))
            .ToList();

        Assert.True(offenders.Count == 0, $"以下写路径显式关闭了事务，请确认理由并登记：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 注入了缓存失效器就必须真的用它——回归"<c>InvalidateConfiguration</c> 无调用点的失效空转"那次事故。
    /// </summary>
    [Fact]
    public void ServicesInjectingInvalidator_ShouldActuallyCallIt()
    {
        var offenders = AppServiceTypes()
            .Where(InjectsCacheInvalidator)
            .Where(type => !type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Any(method => ReachesCacheInvalidator(method, type)))
            .Select(type => type.Name)
            .ToList();

        Assert.True(offenders.Count == 0, $"以下服务注入了 ISaasCacheInvalidator 却一次都没调用（失效空转）：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 反过来：调用了缓存失效器的服务必然注入了它（防止走静态入口绕过 DI）。
    /// </summary>
    [Fact]
    public void ServicesCallingInvalidator_ShouldInjectItThroughConstructor()
    {
        var offenders = AppServiceTypes()
            .Where(type => type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Any(method => ReachesCacheInvalidator(method, type)))
            .Where(type => !InjectsCacheInvalidator(type))
            .Select(type => type.Name)
            .ToList();

        Assert.True(offenders.Count == 0, $"以下服务调用了缓存失效器却未经构造函数注入：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 定义型/授权型资源的每一条事务写路径都必须当场失效缓存——这是本仓库最容易悄悄退化的一条约定。
    /// </summary>
    /// <remarks>
    /// 读侧全部命中分布式缓存，写完不清就等于"改了不生效"，而且不会有任何报错，
    /// 只会表现成"用户反馈权限/菜单/字典改了没用"。
    /// </remarks>
    [Fact]
    public void DefinitionServices_EveryTransactionalWrite_ShouldInvalidateCache()
    {
        var offenders = AppServiceTypes()
            .Where(type => ServicesInvalidatingInline.Contains(type.Name))
            .SelectMany(type => type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttributes<UnitOfWorkAttribute>(inherit: true).Any())
                .Where(method => !ReachesCacheInvalidator(method, type))
                .Select(method => $"{type.Name}.{method.Name}"))
            .ToList();

        Assert.True(offenders.Count == 0, $"以下写路径没有触发任何缓存失效：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 名单本身必须与代码对得上：名单里写着的服务都得真实存在且真的注入了失效器。
    /// </summary>
    /// <remarks>
    /// 否则名单会随着重命名悄悄失效，上面那条断言就变成空跑。
    /// </remarks>
    [Fact]
    public void InlineInvalidationRoster_ShouldStayInSyncWithCode()
    {
        var byName = AppServiceTypes().ToDictionary(type => type.Name, StringComparer.Ordinal);

        var missing = ServicesInvalidatingInline.Where(name => !byName.ContainsKey(name)).ToList();
        Assert.True(missing.Count == 0, $"名单里的服务已不存在（被重命名或删除？）：{string.Join(", ", missing)}");

        var notInjecting = ServicesInvalidatingInline
            .Where(name => !InjectsCacheInvalidator(byName[name]))
            .ToList();
        Assert.True(notInjecting.Count == 0, $"名单里的服务已不再注入缓存失效器：{string.Join(", ", notInjecting)}");
    }

    /// <summary>
    /// 例外名单同样必须对得上代码，避免名单里躺着早已不存在的方法。
    /// </summary>
    [Fact]
    public void UnitOfWorkExemptionRoster_ShouldStayInSyncWithCode()
    {
        var known = PublicMethods()
            .Select(item => $"{item.Type.Name}.{item.Method.Name}")
            .ToHashSet(StringComparer.Ordinal);

        var stale = WritesWithoutUnitOfWorkAttribute.Where(name => !known.Contains(name)).ToList();

        Assert.True(stale.Count == 0, $"例外名单里的方法已不存在，请一并清理：{string.Join(", ", stale)}");
    }

    /// <summary>
    /// 枚举 Saas 模块的全部应用服务类型。
    /// </summary>
    /// <returns>应用服务类型集合。</returns>
    private static List<Type> AppServiceTypes()
    {
        return typeof(SaasApplicationService).Assembly
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.IsPublic)
            .Where(typeof(SaasApplicationService).IsAssignableFrom)
            .Where(type => type.Name.EndsWith("AppService", StringComparison.Ordinal))
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// 枚举全部应用服务上自身声明的公开实例方法。
    /// </summary>
    /// <returns>类型与方法对。</returns>
    private static List<(Type Type, MethodInfo Method)> PublicMethods()
    {
        return AppServiceTypes()
            .SelectMany(type => type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Select(method => (Type: type, Method: method)))
            .ToList();
    }

    /// <summary>
    /// 判断方法是否属于写操作（按应用服务层的命令方法命名约定）。
    /// </summary>
    /// <param name="method">方法。</param>
    /// <returns>是否写操作。</returns>
    private static bool IsWriteMethod(MethodInfo method)
    {
        return Array.Exists(WritePrefixes, prefix => method.Name.StartsWith(prefix, StringComparison.Ordinal));
    }

    /// <summary>
    /// 判断类型是否自行注入了工作单元管理器（手工管事务）。
    /// </summary>
    /// <param name="type">应用服务类型。</param>
    /// <returns>是否手工管事务。</returns>
    private static bool ManagesUnitOfWorkManually(Type type)
    {
        return type.GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Any(parameter => parameter.ParameterType == typeof(IUnitOfWorkManager));
    }

    /// <summary>
    /// 判断类型是否经构造函数注入了缓存失效器。
    /// </summary>
    /// <param name="type">应用服务类型。</param>
    /// <returns>是否注入。</returns>
    private static bool InjectsCacheInvalidator(Type type)
    {
        return type.GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Any(parameter => parameter.ParameterType == typeof(ISaasCacheInvalidator));
    }

    /// <summary>
    /// 剥掉 <c>ValueTask</c> 之外的返回类型包装，取回可判定的返回类型。
    /// </summary>
    /// <param name="method">方法。</param>
    /// <returns>返回类型。</returns>
    private static Type UnwrapReturnType(MethodInfo method)
    {
        return method.ReturnType;
    }

    /// <summary>
    /// 判断方法（含其在同一类型内调用的私有方法）是否真的调到了缓存失效器。
    /// </summary>
    /// <param name="method">被测方法。</param>
    /// <param name="owner">声明类型。</param>
    /// <returns>是否触达。</returns>
    private static bool ReachesCacheInvalidator(MethodInfo method, Type owner)
    {
        return SaasAppIlCallGraph.Reaches(
            method,
            owner,
            callee => callee.DeclaringType == typeof(ISaasCacheInvalidator));
    }
}
