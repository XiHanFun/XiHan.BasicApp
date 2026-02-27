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
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Rbac.Seeders;

/// <summary>
/// 系统操作种子数据
/// </summary>
public class SysOperationSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysOperationSeeder(ISqlSugarDbContext dbContext, ILogger<SysOperationSeeder> logger, IServiceProvider serviceProvider)
        : base(dbContext, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 0;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Rbac]系统操作种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        if (await HasDataAsync<SysOperation>(o => true))
        {
            Logger.LogInformation("系统操作数据已存在，跳过种子数据");
            return;
        }

        var operations = new List<SysOperation>
        {
            // CRUD 操作
            new() { OperationCode = "create", OperationName = "创建", OperationTypeCode = OperationTypeCode.Create, Category = OperationCategory.Crud, HttpMethod = HttpMethodType.POST, Description = "创建新记录", Icon = "plus", Color = "success", IsRequireAudit = true, Status = YesOrNo.Yes, Sort = 1 },
            new() { OperationCode = "read", OperationName = "查看", OperationTypeCode = OperationTypeCode.Read, Category = OperationCategory.Crud, HttpMethod = HttpMethodType.GET, Description = "查看记录详情", Icon = "eye", Color = "info", IsRequireAudit = false, Status = YesOrNo.Yes, Sort = 2 },
            new() { OperationCode = "update", OperationName = "更新", OperationTypeCode = OperationTypeCode.Update, Category = OperationCategory.Crud, HttpMethod = HttpMethodType.PUT, Description = "更新记录", Icon = "edit", Color = "primary", IsRequireAudit = true, Status = YesOrNo.Yes, Sort = 3 },
            new() { OperationCode = "delete", OperationName = "删除", OperationTypeCode = OperationTypeCode.Delete, Category = OperationCategory.Crud, HttpMethod = HttpMethodType.DELETE, Description = "删除记录", Icon = "delete", Color = "danger", IsDangerous = true, IsRequireAudit = true, Status = YesOrNo.Yes, Sort = 4 },
            new() { OperationCode = "view", OperationName = "查看详情", OperationTypeCode = OperationTypeCode.View, Category = OperationCategory.Crud, HttpMethod = HttpMethodType.GET, Description = "查看详细信息", Icon = "file-text", Color = "info", IsRequireAudit = false, Status = YesOrNo.Yes, Sort = 5 },

            // 业务操作
            new() { OperationCode = "approve", OperationName = "审批", OperationTypeCode = OperationTypeCode.Approve, Category = OperationCategory.Business, HttpMethod = HttpMethodType.POST, Description = "审批操作", Icon = "check-circle", Color = "success", IsRequireAudit = true, Status = YesOrNo.Yes, Sort = 10 },
            new() { OperationCode = "execute", OperationName = "执行", OperationTypeCode = OperationTypeCode.Execute, Category = OperationCategory.Business, HttpMethod = HttpMethodType.POST, Description = "执行操作", Icon = "play-circle", Color = "primary", IsRequireAudit = true, Status = YesOrNo.Yes, Sort = 11 },

            // 系统操作
            new() { OperationCode = "import", OperationName = "导入", OperationTypeCode = OperationTypeCode.Import, Category = OperationCategory.System, HttpMethod = HttpMethodType.POST, Description = "导入数据", Icon = "upload", Color = "info", IsRequireAudit = true, Status = YesOrNo.Yes, Sort = 20 },
            new() { OperationCode = "export", OperationName = "导出", OperationTypeCode = OperationTypeCode.Export, Category = OperationCategory.System, HttpMethod = HttpMethodType.GET, Description = "导出数据", Icon = "download", Color = "success", IsRequireAudit = true, Status = YesOrNo.Yes, Sort = 21 },
            new() { OperationCode = "upload", OperationName = "上传", OperationTypeCode = OperationTypeCode.Upload, Category = OperationCategory.System, HttpMethod = HttpMethodType.POST, Description = "上传文件", Icon = "upload", Color = "info", IsRequireAudit = true, Status = YesOrNo.Yes, Sort = 22 },
            new() { OperationCode = "download", OperationName = "下载", OperationTypeCode = OperationTypeCode.Download, Category = OperationCategory.System, HttpMethod = HttpMethodType.GET, Description = "下载文件", Icon = "download", Color = "success", IsRequireAudit = false, Status = YesOrNo.Yes, Sort = 23 },
            new() { OperationCode = "print", OperationName = "打印", OperationTypeCode = OperationTypeCode.Print, Category = OperationCategory.System, HttpMethod = HttpMethodType.GET, Description = "打印文件", Icon = "print", Color = "primary", IsRequireAudit = false, Status = YesOrNo.Yes, Sort = 25 },
            new() { OperationCode = "share", OperationName = "分享", OperationTypeCode = OperationTypeCode.Share, Category = OperationCategory.System, HttpMethod = HttpMethodType.GET, Description = "分享文件", Icon = "share", Color = "warning", IsRequireAudit = false, Status = YesOrNo.Yes, Sort = 26 },

            // 管理操作
            new() { OperationCode = "grant", OperationName = "授权", OperationTypeCode = OperationTypeCode.Grant, Category = OperationCategory.Admin, HttpMethod = HttpMethodType.POST, Description = "授予权限", Icon = "key", Color = "warning", IsRequireAudit = true, Status = YesOrNo.Yes, Sort = 30 },
            new() { OperationCode = "revoke", OperationName = "撤销", OperationTypeCode = OperationTypeCode.Revoke, Category = OperationCategory.Admin, HttpMethod = HttpMethodType.DELETE, Description = "撤销权限", Icon = "lock", Color = "danger", IsDangerous = true, IsRequireAudit = true, Status = YesOrNo.Yes, Sort = 31 },
            new() { OperationCode = "enable", OperationName = "启用", OperationTypeCode = OperationTypeCode.Enable, Category = OperationCategory.Admin, HttpMethod = HttpMethodType.PUT, Description = "启用功能", Icon = "check", Color = "success", IsRequireAudit = true, Status = YesOrNo.Yes, Sort = 32 },
            new() { OperationCode = "disable", OperationName = "禁用", OperationTypeCode = OperationTypeCode.Disable, Category = OperationCategory.Admin, HttpMethod = HttpMethodType.PUT, Description = "禁用功能", Icon = "close", Color = "danger", IsDangerous = true, IsRequireAudit = true, Status = YesOrNo.Yes, Sort = 33 },
        };

        await BulkInsertAsync(operations);
        Logger.LogInformation($"成功初始化 {operations.Count} 个系统操作");
    }
}
