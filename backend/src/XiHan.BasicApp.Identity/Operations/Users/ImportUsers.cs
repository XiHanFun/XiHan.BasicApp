// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Kernel.Validation;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 批量导入用户
/// </summary>
/// <remarks>
/// <c>POST /api/users:import</c>，与新建用户同一权限。用户注册在当前一侧（租户主体即本租户，平台主体即平台账号），共用一个初始密码并标记为须改密
/// 先逐行校验：按 <c>users.create</c> 的请求契约、这批里重复的与已被占用的登录名，再交各包校验部门与角色（<see cref="IUserImportExtension"/>）；
/// 有问题的行记为失败、不写入，其余的行一次写入，部门与角色在同一事务里写好。导入人缺少加入部门或指派角色的权限、席位不够、写入出错时整批失败
/// </remarks>
/// <param name="users">用户仓储</param>
/// <param name="statistics">用户统计仓储</param>
/// <param name="accounts">账号域服务</param>
/// <param name="operations">操作登记（取 <c>users.create</c> 的请求 Schema）</param>
/// <param name="ids">标识生成器</param>
/// <param name="quotas">租户配额</param>
/// <param name="extensions">部门、角色这些列的校验与写入</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = IdentityPermissions.UsersCreate)]
public sealed class ImportUsers(
    IRepository<SysUser, Guid> users,
    IRepository<SysUserStatistics, Guid> statistics,
    UserAccountService accounts,
    OperationRegistry operations,
    IIdGenerator<Guid> ids,
    IEnumerable<ITenantQuota> quotas,
    IEnumerable<IUserImportExtension> extensions) : IOperation<ImportUsersRequest, ImportUsersResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "users.import";

    /// <inheritdoc />
    public async ValueTask<Result<ImportUsersResponse>> HandleAsync(ImportUsersRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var password = accounts.CheckPassword(request.Password);
        if (password.IsFailure)
        {
            return password.Error!;
        }

        var steps = extensions.ToList();
        var currentTenant = context.GetSecurity().Tenant?.Id;
        var homeTenant = currentTenant ?? TenantIds.Platform;
        var failed = new List<ImportUserFailure>();
        var candidates = new List<(int Index, ImportUserRow Row, string UserName)>();
        using (var schema = CreateUserSchema())
        {
            for (var index = 0; index < request.Rows.Count; index++)
            {
                var problems = Validate(index, request.Rows[index], schema.RootElement, context);
                if (problems.Count > 0)
                {
                    failed.AddRange(problems);
                }
                else
                {
                    candidates.Add((index, request.Rows[index], request.Rows[index].UserName!.Trim()));
                }
            }
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var taken = await accounts.TakenNamesAsync(homeTenant, candidates.Select(static c => c.UserName).Distinct().ToList(), context.CancellationToken).ConfigureAwait(false);
        var lines = new List<(UserImportLine Line, ImportUserRow Row)>();
        foreach (var (index, row, userName) in candidates)
        {
            var arguments = MessageArguments.Of(("userName", userName), ("tenant", homeTenant));
            if (!seen.Add(userName))
            {
                failed.Add(Failure(index, "userName", Error.Conflict(IdentityErrors.UserNameDuplicated, IdentityMessages.UserNameDuplicated, arguments), context));
            }
            else if (taken.Contains(userName))
            {
                failed.Add(Failure(index, "userName", Error.Conflict(IdentityErrors.UserNameTaken, IdentityMessages.UserNameTaken, arguments), context));
            }
            else
            {
                var roleCodes = (row.RoleCodes ?? []).Where(static c => !string.IsNullOrWhiteSpace(c)).Select(static c => c.Trim()).Distinct(StringComparer.Ordinal).ToList();
                var departmentCode = string.IsNullOrWhiteSpace(row.DepartmentCode) ? null : row.DepartmentCode.Trim();
                lines.Add((new UserImportLine(index, ids.NewId(), userName, departmentCode, roleCodes), row));
            }
        }

        // 部门与角色由各包依次校验，后一个包只看前面都通过的行（角色的成员数基数按这些行累计）
        foreach (var extension in steps)
        {
            var checkedLines = await extension.ValidateAsync(lines.Select(static l => l.Line).ToList(), context).ConfigureAwait(false);
            if (!checkedLines.TryGetValue(out var issues))
            {
                return checkedLines.Error!;
            }

            failed.AddRange(issues.Select(i => Failure(i.Index, i.Field, i.Reason, context)));
            var rejected = issues.Select(static i => i.Index).ToHashSet();
            lines.RemoveAll(l => rejected.Contains(l.Line.Index));
        }

        var response = new ImportUsersResponse([.. lines.Select(static l => new ImportedUser(l.Line.Index, l.Line.UserId))], [.. failed.OrderBy(static f => f.Index)]);
        if (lines.Count == 0)
        {
            return response;
        }

        // 席位配额：只约束租户主体的调用，按这次要建的人数一次判定
        foreach (var quota in quotas)
        {
            var seats = await quota.EnsureSeatsAsync(context, homeTenant, lines.Count).ConfigureAwait(false);
            if (seats.IsFailure)
            {
                return seats.Error!;
            }
        }

        var written = await WriteAsync(lines, homeTenant, currentTenant, request.Password, context).ConfigureAwait(false);
        if (written.IsFailure)
        {
            return written.Error!;
        }

        foreach (var extension in steps)
        {
            var applied = await extension.ApplyAsync(lines.Select(static l => l.Line).ToList(), context).ConfigureAwait(false);
            if (applied.IsFailure)
            {
                return applied.Error!;
            }
        }

        return response;
    }

    /// <summary>
    /// 写用户、安全行（初始密码）与租户一侧的统计行
    /// </summary>
    private async Task<Result> WriteAsync(IReadOnlyList<(UserImportLine Line, ImportUserRow Row)> lines, string homeTenant, string? currentTenant, string password, InvocationContext context)
    {
        var rows = lines.Select(l => new SysUser
        {
            Id = l.Line.UserId,
            UserName = l.Line.UserName,
            DisplayName = l.Row.DisplayName!.Trim(),
            Email = Blank(l.Row.Email),
            PhoneNumber = Blank(l.Row.PhoneNumber),
            HomeTenantId = homeTenant,
            Status = UserStatus.Active,
            RealName = Blank(l.Row.RealName),
            Gender = l.Row.Gender ?? UserGender.Unknown,
            Birthday = l.Row.Birthday,
            Country = Blank(l.Row.Country),
            Remark = Blank(l.Row.Remark),
        }).ToList();
        foreach (var outcome in await users.AddRangeAsync(rows, context.CancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        var userIds = rows.Select(static u => u.Id).ToList();
        var passwords = await accounts.AddInitialPasswordsAsync(userIds, password, mustChange: true, context.CancellationToken).ConfigureAwait(false);
        if (passwords.IsFailure || currentTenant is null)
        {
            return passwords;
        }

        // 统计行在租户库：只有当前租户就是注册地时才写（与新建用户相同）
        foreach (var outcome in await statistics.AddRangeAsync([.. userIds.Select(id => new SysUserStatistics { Id = ids.NewId(), UserId = id })], context.CancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 一行按 <c>users.create</c> 的请求契约校验
    /// </summary>
    /// <remarks>
    /// 先过请求 Schema（必填、长度、格式），再过请求的自校验（显示名不能全是空白、邮箱格式），与新建用户同一份规则、同样的字段级文案
    /// </remarks>
    private static List<ImportUserFailure> Validate(int index, ImportUserRow row, JsonElement schema, InvocationContext context)
    {
        // 必填的登录名与显示名缺了先占位，序列化后连同其他空字段一起去掉，由 Schema 报「缺少必填属性」
        var create = new CreateUserRequest(row.UserName!, row.DisplayName!, row.Email, row.PhoneNumber, RealName: row.RealName, Gender: row.Gender, Birthday: row.Birthday, Country: row.Country, Remark: row.Remark);
        var payload = JsonSerializer.SerializeToNode(create, IdentityJsonContext.Default.CreateUserRequest)!.AsObject();
        foreach (var name in payload.Where(static p => p.Value is null).Select(static p => p.Key).ToList())
        {
            payload.Remove(name);
        }

        using var instance = JsonDocument.Parse(payload.ToJsonString());
        var violations = JsonSchemaValidator.Validate(schema, instance.RootElement);
        if (violations.Count > 0)
        {
            return [.. violations.Select(v => new ImportUserFailure(index, FieldOf(v.Field), KernelErrors.SchemaViolation, v.MessageKey is { } key ? context.Localize(key, v.Arguments) : v.Reason))];
        }

        var failures = new List<ImportUserFailure>();
        foreach (var result in create.Validate(new ValidationContext(create)))
        {
            var message = result.ErrorMessage ?? context.Localize(KernelMessages.ValidationInvalid);
            var members = result.MemberNames.Where(static m => !string.IsNullOrWhiteSpace(m)).ToList();
            failures.AddRange(members.Count == 0
                ? [new ImportUserFailure(index, null, KernelErrors.RequestInvalid, message)]
                : members.Select(m => new ImportUserFailure(index, JsonNamingPolicy.CamelCase.ConvertName(m), KernelErrors.RequestInvalid, message)));
        }

        return failures;
    }

    private JsonDocument CreateUserSchema()
    {
        // users.create 由本包登记，取不到是装配错误
        var schema = operations.Find(CreateUser.Id).TryGetValue(out var descriptor) && descriptor.Schema is { } found
            ? found
            : throw new InvalidOperationException($"Operation '{CreateUser.Id}' has no request schema.");
        return JsonDocument.Parse(schema.Request);
    }

    private static ImportUserFailure Failure(int index, string? field, Error error, InvocationContext context)
        => new(index, field, error.Code, context.Localize(error));

    private static string? FieldOf(string field) => field.Length == 0 ? null : field;

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
