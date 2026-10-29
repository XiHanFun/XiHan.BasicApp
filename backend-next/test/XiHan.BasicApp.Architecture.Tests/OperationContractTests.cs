// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.Architecture.Tests.Model;
using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Architecture.Tests;

/// <summary>
/// 检查每个操作的暴露声明、权限码与匿名白名单。
/// </summary>
/// <remarks>
/// 方案 5.4 / 第 10 节第 8 项：每个操作显式写 <c>Expose</c>（编译期 XHG004）；权限码点分三段且资源段与操作 ID 同词；匿名操作只能在白名单里。
/// </remarks>
public sealed class OperationContractTests
{
    /// <summary>
    /// 不写 Permission 也不写 Policy 的操作白名单。
    /// </summary>
    /// <remarks>
    /// 这类操作（登录、刷新令牌；之后的验证码、找回密码）在这里登记。签名开放接口（<c>open.*</c>）的调用方不成为主体，验签在 Authenticate 阶段做，也登记在这里。
    /// </remarks>
    private static readonly HashSet<string> _anonymousOperations = new(StringComparer.Ordinal) { "auth.login", "auth.refresh", "auth.verify-two-factor", "auth.captcha", "auth.login-config", "auth.external-login", "open.ping", "open.echo" };

    internal static IEnumerable<(Type Type, OperationAttribute Attribute, string Package)> Operations()
        => Packages.Assemblies
            .SelectMany(static a => a.GetTypes().Select(t => (Type: t, Attribute: t.GetCustomAttribute<OperationAttribute>(), Package: Packages.ShortName(a.GetName().Name!))))
            .Where(static x => x.Attribute is not null)
            .Select(static x => (x.Type, x.Attribute!, x.Package));

    [Fact]
    public void ThereAreOperations()
    {
        Assert.NotEmpty(Operations());
    }

    [Fact]
    public void EveryOperation_WritesExposeExplicitly()
    {
        // 显式写 Expose 由框架生成器诊断 XHG004（.editorconfig 设为 error）在编译期强制；程序集级 OperationDefaults 会让它不再报告，所以不许声明
        foreach (var assembly in Packages.Assemblies)
        {
            Assert.True(assembly.GetCustomAttribute<OperationDefaultsAttribute>() is null, $"{assembly.GetName().Name}: BasicApp writes Expose on every operation (XHG004); an assembly-level OperationDefaults would silence that check");
        }

        foreach (var (type, attribute, _) in Operations())
        {
            Assert.True(attribute.Expose != Expose.None, $"{type.FullName}: Expose.None publishes nothing");
        }
    }

    [Fact]
    public void EveryOperation_LivesUnderOperationsResourceNamespace()
    {
        foreach (var (type, attribute, package) in Operations())
        {
            var resource = ToPascal(OperationId.ResourceOf(attribute.Id));
            var expected = $"{Packages.Prefix}{package}.Operations.{resource}";
            Assert.True(string.Equals(type.Namespace, expected, StringComparison.Ordinal), $"{type.FullName}: operation '{attribute.Id}' must live in namespace {expected} (Operations/{resource}/)");
        }
    }

    [Fact]
    public void EveryOperation_HasAThreeSegmentPermission_OrIsWhitelistedAnonymous()
    {
        foreach (var (type, attribute, package) in Operations())
        {
            if (attribute.Permission is null && attribute.Policy is null)
            {
                Assert.True(_anonymousOperations.Contains(attribute.Id), $"{type.FullName}: operation '{attribute.Id}' has neither Permission nor Policy; anonymous operations must be whitelisted in {nameof(OperationContractTests)}");
                continue;
            }

            if (attribute.Permission is null)
            {
                continue;
            }

            Assert.True(PermissionDescriptor.IsValidCode(attribute.Permission), $"{type.FullName}: permission '{attribute.Permission}' is not '<package>.<resource>.<action>'");
            var descriptor = new PermissionDescriptor(attribute.Permission, "x", PermissionScope.Both);
            Assert.True(string.Equals(descriptor.Package, package.ToLowerInvariant(), StringComparison.Ordinal), $"{type.FullName}: permission '{attribute.Permission}' must start with the owning package '{package.ToLowerInvariant()}'");
            Assert.True(string.Equals(descriptor.Resource, OperationId.ResourceOf(attribute.Id), StringComparison.Ordinal), $"{type.FullName}: permission resource '{descriptor.Resource}' must equal the operation resource '{OperationId.ResourceOf(attribute.Id)}'");
            Assert.True(PermissionCatalog.Of(package).Any(p => string.Equals(p.Code, attribute.Permission, StringComparison.Ordinal)), $"{type.FullName}: permission '{attribute.Permission}' is not declared in {package}Permissions.All");
        }
    }

    [Fact]
    public void PermissionCatalogs_DeclareOnlyTheirOwnPackage_WithUniqueCodes()
    {
        foreach (var package in Packages.Assemblies.Select(static a => Packages.ShortName(a.GetName().Name!)))
        {
            var all = PermissionCatalog.Of(package);
            Assert.Equal(all.Count, all.Select(static p => p.Code).Distinct(StringComparer.Ordinal).Count());
            foreach (var descriptor in all)
            {
                Assert.True(string.Equals(descriptor.Package, package.ToLowerInvariant(), StringComparison.Ordinal), $"{package}Permissions declares '{descriptor.Code}' of another package");
            }
        }
    }

    private static string ToPascal(string kebab)
        => string.Concat(kebab.Split('-').Select(static part => char.ToUpperInvariant(part[0]) + part[1..]));
}
