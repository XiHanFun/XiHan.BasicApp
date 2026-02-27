#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConfigSeeder
// Guid:be6ad33d-b678-4682-ab57-f5e246f24b76
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 12:12:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Rbac.Seeders;

/// <summary>
/// 系统配置种子数据
/// </summary>
public class SysConfigSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysConfigSeeder(ISqlSugarDbContext dbContext, ILogger<SysConfigSeeder> logger, IServiceProvider serviceProvider)
        : base(dbContext, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 18;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Rbac]系统配置种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        if (await HasDataAsync<SysConfig>(c => true))
        {
            Logger.LogInformation("系统配置数据已存在，跳过种子数据");
            return;
        }

        var configs = new List<SysConfig>
        {
            new()
            {
                ConfigName = "系统名称",
                ConfigGroup = "system.base",
                ConfigKey = "system.name",
                ConfigValue = "曦寒基础应用",
                DefaultValue = "曦寒基础应用",
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.String,
                ConfigDescription = "系统展示名称",
                IsBuiltIn = true,
                IsEncrypted = false,
                Status = YesOrNo.Yes,
                Sort = 1
            },
            new()
            {
                ConfigName = "系统版本",
                ConfigGroup = "system.base",
                ConfigKey = "system.version",
                ConfigValue = "v1.0.0",
                DefaultValue = "v1.0.0",
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.String,
                ConfigDescription = "系统版本号",
                IsBuiltIn = true,
                IsEncrypted = false,
                Status = YesOrNo.Yes,
                Sort = 2
            },
            new()
            {
                ConfigName = "默认语言",
                ConfigGroup = "system.base",
                ConfigKey = "system.defaultLanguage",
                ConfigValue = "zh-CN",
                DefaultValue = "zh-CN",
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.String,
                ConfigDescription = "系统默认语言",
                IsBuiltIn = true,
                IsEncrypted = false,
                Status = YesOrNo.Yes,
                Sort = 3
            },
            new()
            {
                ConfigName = "允许用户注册",
                ConfigGroup = "system.security",
                ConfigKey = "system.allowRegister",
                ConfigValue = "false",
                DefaultValue = "false",
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.Boolean,
                ConfigDescription = "是否允许用户自行注册账号",
                IsBuiltIn = true,
                IsEncrypted = false,
                Status = YesOrNo.Yes,
                Sort = 10
            },
            new()
            {
                ConfigName = "密码最小长度",
                ConfigGroup = "system.security",
                ConfigKey = "system.passwordMinLength",
                ConfigValue = "8",
                DefaultValue = "8",
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.Number,
                ConfigDescription = "密码策略-最小长度",
                IsBuiltIn = true,
                IsEncrypted = false,
                Status = YesOrNo.Yes,
                Sort = 11
            },
            new()
            {
                ConfigName = "上传大小限制(MB)",
                ConfigGroup = "system.file",
                ConfigKey = "system.uploadMaxSizeMb",
                ConfigValue = "20",
                DefaultValue = "20",
                ConfigType = ConfigType.System,
                DataType = ConfigDataType.Number,
                ConfigDescription = "系统文件上传大小限制",
                IsBuiltIn = true,
                IsEncrypted = false,
                Status = YesOrNo.Yes,
                Sort = 20
            },
            new()
            {
                ConfigName = "代码生成默认作者",
                ConfigGroup = "system.codegen",
                ConfigKey = "codegen.defaultAuthor",
                ConfigValue = "xihan",
                DefaultValue = "xihan",
                ConfigType = ConfigType.Application,
                DataType = ConfigDataType.String,
                ConfigDescription = "代码生成默认作者",
                IsBuiltIn = true,
                IsEncrypted = false,
                Status = YesOrNo.Yes,
                Sort = 30
            }
        };

        await BulkInsertAsync(configs);
        Logger.LogInformation("成功初始化 {Count} 个系统配置", configs.Count);
    }
}
