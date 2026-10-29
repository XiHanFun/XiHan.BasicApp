// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;
using XiHan.Framework.Kernel.Security;

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 应用层的权限描述。
/// </summary>
/// <remarks>
/// 在内核 <see cref="PermissionDefinition"/>（码、显示名、说明）之上加作用侧与是否审计。
/// 权限码文法 <c>&lt;包&gt;.&lt;资源&gt;.&lt;动作&gt;</c> 点分三段小写；资源段与操作 ID 的资源段同词。权限目录种子从各包的描述表派生。
/// </remarks>
public sealed partial record PermissionDescriptor
{
    /// <summary>
    /// 构造权限描述。
    /// </summary>
    /// <param name="code">权限码。</param>
    /// <param name="displayName">显示名。</param>
    /// <param name="scope">作用侧。</param>
    /// <param name="audited">该权限保护的操作是否进审计。</param>
    /// <param name="description">说明。</param>
    /// <exception cref="ArgumentException">码不符合三段文法时抛出。</exception>
    public PermissionDescriptor(string code, string displayName, PermissionScope scope, bool audited = false, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (!CodePattern().IsMatch(code))
        {
            throw new ArgumentException($"Permission code '{code}' must be '<package>.<resource>.<action>' in lowercase.", nameof(code));
        }

        Code = code;
        DisplayName = displayName;
        Scope = scope;
        Audited = audited;
        Description = description;
    }

    /// <summary>
    /// 权限码。
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// 显示名。
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// 作用侧。
    /// </summary>
    public PermissionScope Scope { get; }

    /// <summary>
    /// 该权限保护的操作是否进审计。
    /// </summary>
    public bool Audited { get; }

    /// <summary>
    /// 说明。
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// 包段，如 <c>identity</c>。
    /// </summary>
    public string Package => Code[..Code.IndexOf('.', StringComparison.Ordinal)];

    /// <summary>
    /// 资源段，如 <c>users</c>。
    /// </summary>
    public string Resource => Code.Split('.')[1];

    /// <summary>
    /// 动作段，如 <c>read</c>。
    /// </summary>
    public string Action => Code[(Code.LastIndexOf('.') + 1)..];

    /// <summary>
    /// 换算成内核权限定义。
    /// </summary>
    /// <remarks>
    /// 供 <c>FeatureContext.AddPermission</c> 登记。
    /// </remarks>
    public PermissionDefinition ToDefinition() => new(Code, DisplayName, Description);

    /// <summary>
    /// 判断一个字符串是否符合三段权限码文法。
    /// </summary>
    public static bool IsValidCode(string? code) => code is not null && CodePattern().IsMatch(code);

    [GeneratedRegex("^[a-z][a-z0-9-]*\\.[a-z][a-z0-9-]*\\.[a-z][a-z0-9-]*$")]
    private static partial Regex CodePattern();
}
