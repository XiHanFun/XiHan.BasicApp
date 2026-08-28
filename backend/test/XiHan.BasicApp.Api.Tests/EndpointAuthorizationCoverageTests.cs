// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Web.Api.DynamicApi.Helpers;

namespace XiHan.BasicApp.Api.Tests;

/// <summary>
/// 动态 API 暴露面的授权判定完整性测试。
/// 每个被暴露为 HTTP 端点的方法必须命中三者之一：方法级 <see cref="PermissionAuthorizeAttribute"/>、
/// <see cref="AllowAnonymousAttribute"/>、或本文件的自助端点白名单。
/// </summary>
public sealed class EndpointAuthorizationCoverageTests
{
    /// <summary>
    /// 承载动态 API 的业务模块程序集。新增模块须同时登记此处与测试工程的 ProjectReference。
    /// </summary>
    private static readonly Assembly[] ModuleAssemblies =
    [
        typeof(BasicApp.Saas.XiHanBasicAppSaasModule).Assembly,
        typeof(BasicApp.AI.XiHanBasicAppAIModule).Assembly,
        typeof(BasicApp.Chat.XiHanBasicAppChatModule).Assembly,
        typeof(BasicApp.CodeGeneration.XiHanBasicAppCodeGenerationModule).Assembly,
        typeof(BasicApp.Printing.XiHanBasicAppPrintingModule).Assembly,
        typeof(BasicApp.Workflow.XiHanBasicAppWorkflowModule).Assembly
    ];

    /// <summary>
    /// 自助端点白名单，元素为「类名.方法名」。
    /// 列入即声明该端点不靠 <see cref="PermissionAuthorizeAttribute"/> 门控，
    /// 分组注释记录其数据边界由什么锁定：或是按调用者身份限定数据（自助），
    /// 或是在方法体内用 <c>IPermissionChecker</c> 做属性表达不了的命令式判定（如「二者取一」）。
    /// </summary>
    private static readonly IReadOnlySet<string> SelfServiceEndpoints =
        new HashSet<string>(StringComparer.Ordinal)
        {
            // AiAssistantQueryService（1）：聊天页助手选项，助手管理权限属后台配置，不用于门控普通用户使用助手
            "AiAssistantQueryService.GetAvailableAsync",

            // AuthAppService（8）：当前会话自助，只读写调用者自身的登录态
            "AuthAppService.CreateOAuthBindTicketAsync",
            "AuthAppService.GetPermissionsAsync",
            "AuthAppService.GetUserInfoAsync",
            "AuthAppService.LockSessionAsync",
            "AuthAppService.LogoutAsync",
            // 模仿态下当前主体是被模仿者、不持有模仿类权限码，准入靠令牌声明与会话行的模仿者交叉验证
            "AuthAppService.StopImpersonationAsync",
            "AuthAppService.SwitchTenantAsync",
            "AuthAppService.UnlockSessionAsync",

            // ChatAssistantAppService（2）：当前用户与助手的会话，会话归属经 ChatDomainService 按 userId 解析
            "ChatAssistantAppService.OpenConversationAsync",
            "ChatAssistantAppService.ReplyAsync",

            // EnumMetadataAppService（2）：全站字典标签的单一事实源，任何登录用户都要能取，类级 Authorize 已门控
            "EnumMetadataAppService.GetAllEnumsAsync",
            "EnumMetadataAppService.GetEnumAsync",

            // TimeZoneMetadataAppService（1）：时区目录，顶栏/个人中心/编号规则共用，任何登录用户都要能取，类级 Authorize 已门控
            "TimeZoneMetadataAppService.GetTimeZoneOptionsAsync",

            // ExportTaskAppService（3）：导出任务归属当前用户，仓储按 userId 取改，非本人一律拒绝
            "ExportTaskAppService.CancelAsync",
            "ExportTaskAppService.DeleteAsync",
            "ExportTaskAppService.SubmitAsync",

            // ExportTaskQueryService（2）：导出任务归属当前用户，查询按 userId 限定
            "ExportTaskQueryService.GetDetailAsync",
            "ExportTaskQueryService.GetMineAsync",

            // ImportHistoryAppService（1）：导入历史归属当前用户
            "ImportHistoryAppService.CreateAsync",

            // ImportHistoryQueryService（1）：导入历史归属当前用户
            "ImportHistoryQueryService.GetMineAsync",

            // MyFieldSecurityAppService（1）：下发当前主体的字段权限，边界在 IFieldSecurityService.ResolveAsync 内按调用者解析
            "MyFieldSecurityAppService.GetMineAsync",

            // MyOAuthAppAppService（6）：自有 OAuth 应用，写路径经 GetOwnedOrThrowAsync 校验 CreatedId 为本人
            "MyOAuthAppAppService.CreateMyOAuthAppAsync",
            "MyOAuthAppAppService.DeleteMyOAuthAppAsync",
            "MyOAuthAppAppService.GetMyOAuthAppsAsync",
            "MyOAuthAppAppService.RegenerateMyOAuthAppSecretAsync",
            "MyOAuthAppAppService.UpdateMyOAuthAppAsync",
            "MyOAuthAppAppService.UpdateMyOAuthAppStatusAsync",

            // OAuthConsentAppService（2）：当前用户对自己那次授权请求的确认
            "OAuthConsentAppService.AuthorizeAsync",
            "OAuthConsentAppService.ResolveAuthorizationAsync",

            // PrintDataSourceQueryService（1）：并非只按登录态门控 —— 方法内经 IPermissionChecker
            // 命令式校验 PrintingPermissionCodes.Read 或 Use，持有任一即可读，缺失两者抛
            // UserFriendlyException。目录同时服务模板管理与业务打印两条路径，单个
            // PermissionAuthorize 特性表达不了「二者取一」，故权限判定下沉到方法体，属性扫描看不见。
            "PrintDataSourceQueryService.GetListAsync",

            // ProfileAppService（32）：个人中心自助，全部方法经 GetCurrentUserIdOrThrow 锁定当前用户
            "ProfileAppService.ChangePasswordAsync",
            "ProfileAppService.ChangeUserNameAsync",
            "ProfileAppService.ConfirmChangeEmailAsync",
            "ProfileAppService.ConfirmChangePhoneAsync",
            "ProfileAppService.CreateApiCredentialAsync",
            "ProfileAppService.DeactivateAccountAsync",
            "ProfileAppService.DeleteAccountAsync",
            "ProfileAppService.DeleteApiCredentialAsync",
            "ProfileAppService.Disable2FAAsync",
            "ProfileAppService.Enable2FAAsync",
            "ProfileAppService.GetActivityAsync",
            "ProfileAppService.GetApiCredentialsAsync",
            "ProfileAppService.GetLinkedAccountsAsync",
            "ProfileAppService.GetLoginLogsAsync",
            "ProfileAppService.GetNotificationPreferenceAsync",
            "ProfileAppService.GetProfileAsync",
            "ProfileAppService.GetSessionsAsync",
            "ProfileAppService.RevokeOtherSessionsAsync",
            "ProfileAppService.RevokeSessionAsync",
            "ProfileAppService.RotateApiCredentialSecretAsync",
            "ProfileAppService.Send2FASetupCodeAsync",
            "ProfileAppService.SendChangeEmailCodeAsync",
            "ProfileAppService.SendChangePhoneCodeAsync",
            "ProfileAppService.SendEmailVerifyCodeAsync",
            "ProfileAppService.SendPhoneVerifyCodeAsync",
            "ProfileAppService.Setup2FAAsync",
            "ProfileAppService.UnlinkAccountAsync",
            "ProfileAppService.UpdateApiCredentialStatusAsync",
            "ProfileAppService.UpdateNotificationPreferenceAsync",
            "ProfileAppService.UpdateProfileAsync",
            "ProfileAppService.VerifyEmailAsync",
            "ProfileAppService.VerifyPhoneAsync",

            // TenantQueryService（1）：选租户阶段用户尚未进入任何租户、不持有任何权限码，数据限定于本人有效成员关系
            "TenantQueryService.GetMyAvailableTenantsAsync",

            // UserInboxAppService（8）：当前用户收件箱
            "UserInboxAppService.ConfirmAsync",
            "UserInboxAppService.GetBannerAsync",
            "UserInboxAppService.GetListAsync",
            "UserInboxAppService.GetMandatoryUnreadAsync",
            "UserInboxAppService.GetPopupAsync",
            "UserInboxAppService.MarkAllReadAsync",
            "UserInboxAppService.MarkPopupShownAsync",
            "UserInboxAppService.MarkReadAsync",

            // UserSettingAppService（1）：当前用户偏好设置
            "UserSettingAppService.SaveAsync",

            // UserSettingQueryService（1）：当前用户偏好设置
            "UserSettingQueryService.GetAsync",

            // WorkflowTodoAppService（3）：我的待办，办理人服务端锁定为当前登录用户，转办与加签合法性由框架任务服务在实例锁内校验
            "WorkflowTodoAppService.AddAssigneesAsync",
            "WorkflowTodoAppService.CompleteAsync",
            "WorkflowTodoAppService.TransferAsync",

            // WorkflowTodoQueryService（1）：我的待办，查询按当前登录用户限定
            "WorkflowTodoQueryService.GetPageAsync"
        };

    /// <summary>
    /// 每个暴露端点都必须有明确的授权判定。
    /// </summary>
    [Fact]
    public void ExposedEndpoints_ShouldAllHaveExplicitAuthorizationDecision()
    {
        var violations = EnumerateExposedEndpoints()
            .Where(endpoint => !HasExplicitDecision(endpoint.Service, endpoint.Method))
            .Select(endpoint => $"{endpoint.Service.Name}.{endpoint.Method.Name}")
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            $"下列 {violations.Count} 个端点既无 PermissionAuthorize、也无 AllowAnonymous、也不在自助白名单内，" +
            $"将静默降级为「任意已登录用户可访问」：{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 自助端点白名单不得残留失效条目：端点已删除、改名，或已补上权限码后必须同步移除。
    /// </summary>
    [Fact]
    public void SelfServiceAllowList_ShouldNotContainStaleEntries()
    {
        var live = EnumerateExposedEndpoints()
            .Where(endpoint => !HasPermissionCode(endpoint.Method) && !IsAnonymous(endpoint.Service, endpoint.Method))
            .Select(endpoint => $"{endpoint.Service.Name}.{endpoint.Method.Name}")
            .ToHashSet(StringComparer.Ordinal);

        var stale = SelfServiceEndpoints
            .Where(key => !live.Contains(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        Assert.True(stale.Count == 0,
            $"下列 {stale.Count} 个白名单条目已失效（端点不存在，或已补上权限码 / 匿名标记），请从白名单移除：" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, stale)}");
    }

    /// <summary>
    /// 每个暴露端点所在的服务类都必须声明 Authorize 或 AllowAnonymous，不依赖全局兜底策略。
    /// </summary>
    [Fact]
    public void ExposedServices_ShouldDeclareClassLevelAuthorization()
    {
        var violations = EnumerateExposedEndpoints()
            .Select(endpoint => endpoint.Service)
            .Distinct()
            .Where(service => !service.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any()
                              && !service.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any())
            .Select(service => service.FullName ?? service.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            $"下列 {violations.Count} 个应用服务类既无 Authorize 也无 AllowAnonymous，其鉴权取决于 " +
            $"XiHan:Web:Api:Auth:RequireAuthenticatedUser（框架侧默认 false）：" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 凡被本测试工程引用、且承载动态 API 的业务模块，都必须登记进 <see cref="ModuleAssemblies"/>。
    /// </summary>
    /// <remarks>
    /// 起因：Chat 与 Printing 早就被 csproj 引用、各自暴露 3 个动态 API 服务，却漏登记在
    /// <see cref="ModuleAssemblies"/> 里，导致这两个模块的全部端点静默逃出上面三条授权守卫——
    /// 测试照常全绿，没有任何信号。csproj 里那句「新增模块时必须在此登记」的注释显然没兜住。
    /// <para>
    /// 这里把它换成会红的断言：扫描输出目录中全部 <c>XiHan.BasicApp.*.dll</c>，
    /// 凡含动态 API 服务却不在册的，直接失败。判据复用框架的 <see cref="TypeHelper"/>，
    /// 与运行期生成控制器的集合保持一致。
    /// </para>
    /// </remarks>
    [Fact]
    public void ModuleAssemblies_ShouldCoverEveryReferencedModuleCarryingDynamicApi()
    {
        var registered = ModuleAssemblies
            .Select(assembly => assembly.GetName().Name)
            .ToHashSet(StringComparer.Ordinal);

        var missing = Directory
            .EnumerateFiles(AppContext.BaseDirectory, "XiHan.BasicApp.*.dll")
            .Select(path => (Path: path, Name: Path.GetFileNameWithoutExtension(path)))
            .Where(candidate => !registered.Contains(candidate.Name))
            .Where(candidate => !candidate.Name.EndsWith(".Tests", StringComparison.Ordinal))
            .Where(candidate => CarriesDynamicApi(candidate.Path))
            .Select(candidate => candidate.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0,
            $"下列 {missing.Count} 个模块承载动态 API 却未登记进 ModuleAssemblies，其端点不受本文件任何授权守卫约束：" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    /// <summary>
    /// 判断指定程序集文件里是否存在被启用的动态 API 应用服务。
    /// </summary>
    /// <remarks>
    /// 只吞 <see cref="BadImageFormatException"/>（本机镜像不是托管程序集）；
    /// 其余加载异常一律外抛，避免"加载不了就当没有"把本守卫悄悄架空。
    /// </remarks>
    /// <param name="assemblyPath">程序集文件路径。</param>
    private static bool CarriesDynamicApi(string assemblyPath)
    {
        Assembly assembly;
        try
        {
            assembly = Assembly.LoadFrom(assemblyPath);
        }
        catch (BadImageFormatException)
        {
            return false;
        }

        Type?[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // 部分类型加载失败时按已加载出来的那部分判断，宁可误报也不漏报
            types = ex.Types;
        }

        return types
            .Where(type => type is not null)
            .Select(type => type!)
            .Where(TypeHelper.IsApplicationService)
            .Any(service => DynamicApiAttributeMergeHelper.IsEnabled(service));
    }

    /// <summary>
    /// 按框架的动态 API 生成规则枚举全部被暴露为 HTTP 端点的方法。
    /// 类型判据与方法判据直接调用框架的 <see cref="TypeHelper"/>，确保与运行期生成的控制器同集合。
    /// </summary>
    private static IReadOnlyList<(Type Service, MethodInfo Method)> EnumerateExposedEndpoints()
    {
        return ModuleAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(TypeHelper.IsApplicationService)
            .Where(service => DynamicApiAttributeMergeHelper.IsEnabled(service))
            .SelectMany(service => service
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(TypeHelper.ShouldExposeAsApi)
                .Where(method => DynamicApiAttributeMergeHelper.IsEnabled(service, method))
                .Select(method => (Service: service, Method: method)))
            .ToList();
    }

    /// <summary>
    /// 判断端点是否有明确的授权判定。
    /// </summary>
    private static bool HasExplicitDecision(Type service, MethodInfo method)
    {
        return HasPermissionCode(method)
               || IsAnonymous(service, method)
               || SelfServiceEndpoints.Contains($"{service.Name}.{method.Name}");
    }

    /// <summary>
    /// 判断端点是否挂了权限码。
    /// </summary>
    private static bool HasPermissionCode(MethodInfo method)
    {
        return method.GetCustomAttributes<PermissionAuthorizeAttribute>(inherit: true).Any();
    }

    /// <summary>
    /// 判断端点是否被标记为匿名（方法级优先，类级兜底）。
    /// </summary>
    private static bool IsAnonymous(Type service, MethodInfo method)
    {
        return method.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any()
               || service.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any();
    }
}
