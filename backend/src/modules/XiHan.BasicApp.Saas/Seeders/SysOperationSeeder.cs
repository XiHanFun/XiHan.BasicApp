#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOperationSeeder
// Guid:1a2b3c4d-5e6f-7890-abcd-001122334455
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统操作种子数据。
/// </summary>
public class SysOperationSeeder : DataSeederBase
{
    public SysOperationSeeder(
        ISqlSugarClientResolver clientResolver,
        ILogger<SysOperationSeeder> logger,
        IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    public override int Order => SaasSeedOrder.Operations;

    public override string Name => "[Saas]系统操作种子数据";

    protected override async Task SeedInternalAsync()
    {
        var templates = BuildTemplates();
        var operationCodes = templates.Select(item => item.OperationCode).ToArray();
        var existingOperations = await DbClient
            .Queryable<SysOperation>()
            .Where(operation => operationCodes.Contains(operation.OperationCode))
            .ToListAsync();

        var existingMap = existingOperations.ToDictionary(operation => operation.OperationCode, StringComparer.OrdinalIgnoreCase);
        var toInsert = templates
            .Where(template => !existingMap.ContainsKey(template.OperationCode))
            .ToList();

        if (toInsert.Count > 0)
        {
            await BulkInsertAsync(toInsert);
        }

        var toEnableIds = existingOperations
            .Where(operation =>
                operation.TenantId == SaasSeedDefaults.PlatformTenantId
                && operation.IsGlobal
                && operation.Status != YesOrNo.Yes)
            .Select(operation => operation.BasicId)
            .ToArray();

        if (toEnableIds.Length > 0)
        {
            await DbClient
                .Updateable<SysOperation>()
                .SetColumns(operation => operation.Status == YesOrNo.Yes)
                .Where(operation => toEnableIds.Contains(operation.BasicId))
                .ExecuteCommandAsync();
        }

        Logger.LogInformation(
            "系统操作模板种子完成：新增 {InsertCount} 项，启用 {EnableCount} 项",
            toInsert.Count,
            toEnableIds.Length);
    }

    private static List<SysOperation> BuildTemplates()
    {
        return
        [
            Create("create", "创建", OperationTypeCode.Create, OperationCategory.Crud, HttpMethodType.POST, "创建新记录", "plus", "success", false, true, 10),
            Create("read", "读取", OperationTypeCode.Read, OperationCategory.Crud, HttpMethodType.GET, "读取列表或概览", "eye", "info", false, false, 20),
            Create("view", "详情", OperationTypeCode.View, OperationCategory.Crud, HttpMethodType.GET, "读取详情数据", "file-text", "info", false, false, 30),
            Create("update", "更新", OperationTypeCode.Update, OperationCategory.Crud, HttpMethodType.PUT, "更新既有记录", "edit", "primary", false, true, 40),
            Create("delete", "删除", OperationTypeCode.Delete, OperationCategory.Crud, HttpMethodType.DELETE, "删除或归档记录", "trash-2", "danger", true, true, 50),
            Create("approve", "审批", OperationTypeCode.Approve, OperationCategory.Business, HttpMethodType.POST, "执行审批或通过动作", "check-circle", "success", false, true, 60),
            Create("execute", "执行", OperationTypeCode.Execute, OperationCategory.Business, HttpMethodType.POST, "执行任务或流程动作", "play-circle", "primary", false, true, 70),
            Create("grant", "授权", OperationTypeCode.Grant, OperationCategory.Admin, HttpMethodType.POST, "授予角色、权限或成员能力", "key-round", "warning", false, true, 80),
            Create("revoke", "撤销", OperationTypeCode.Revoke, OperationCategory.Admin, HttpMethodType.DELETE, "撤销角色、权限或成员能力", "shield-x", "danger", true, true, 90),
            Create("enable", "启用", OperationTypeCode.Enable, OperationCategory.Admin, HttpMethodType.PUT, "启用实体或功能", "check", "success", false, true, 100),
            Create("disable", "禁用", OperationTypeCode.Disable, OperationCategory.Admin, HttpMethodType.PUT, "禁用实体或功能", "ban", "danger", true, true, 110),
            Create("import", "导入", OperationTypeCode.Import, OperationCategory.System, HttpMethodType.POST, "导入批量数据", "upload", "info", false, true, 120),
            Create("export", "导出", OperationTypeCode.Export, OperationCategory.System, HttpMethodType.GET, "导出批量数据", "download", "success", false, true, 130),
            Create("upload", "上传", OperationTypeCode.Upload, OperationCategory.System, HttpMethodType.POST, "上传文件内容", "upload-cloud", "info", false, true, 140),
            Create("download", "下载", OperationTypeCode.Download, OperationCategory.System, HttpMethodType.GET, "下载文件内容", "download-cloud", "success", false, false, 150),
            Create("print", "打印", OperationTypeCode.Print, OperationCategory.System, HttpMethodType.GET, "打印输出内容", "printer", "primary", false, false, 160),
            Create("share", "分享", OperationTypeCode.Share, OperationCategory.System, HttpMethodType.POST, "分享链接或内容", "share-2", "warning", false, true, 170)
        ];
    }

    private static SysOperation Create(
        string code,
        string name,
        OperationTypeCode operationTypeCode,
        OperationCategory category,
        HttpMethodType httpMethod,
        string description,
        string icon,
        string color,
        bool isDangerous,
        bool isRequireAudit,
        int sort)
    {
        return new SysOperation
        {
            TenantId = SaasSeedDefaults.PlatformTenantId,
            IsGlobal = true,
            OperationCode = code,
            OperationName = name,
            OperationTypeCode = operationTypeCode,
            Category = category,
            HttpMethod = httpMethod,
            Description = description,
            Icon = icon,
            Color = color,
            IsDangerous = isDangerous,
            IsRequireAudit = isRequireAudit,
            Status = YesOrNo.Yes,
            Sort = sort
        };
    }
}
