#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AutoVersionUpdate
// Guid:9b069c7c-c521-452d-b00c-a04d6ee09070
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/01 15:29:44
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Options;
using SqlSugar;
using XiHan.Framework.DistributedIds.SnowflakeIds;
using XiHan.Framework.Utils.Extensions;
using XiHan.Framework.Utils.Logging;
using XiHan.Framework.Utils.Reflections;

namespace XiHan.BasicApp.Web.Core.Extensions;

/// <summary>
/// 自动版本更新中间件拓展
/// </summary>
public static class AutoVersionUpdate
{
    /// <summary>
    /// 使用自动版本更新中间件
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseAutoVersionUpdate(this IApplicationBuilder app)
    {
        LogHelper.Info("AutoVersionUpdate 中间件运行");

        var config = app.ApplicationServices.GetRequiredService<IOptions<SnowflakeIdOptions>>().Value;
        if (config.WorkerId != 1)
        {
            LogHelper.Handle("非主节点，不执行脚本");
            return app;
        }

        var currentVersion = GetEntryAssemblyCurrentVersion();
        LogHelper.Handle($"当前版本：{currentVersion}");

        var historyVersionInfo = GetEntryAssemblyHistoryVersionInfo();
        var historyVersion = historyVersionInfo.Version;
        var historyDate = historyVersionInfo.Date;
        var historyIsRunScript = historyVersionInfo.IsRunScript;

        LogHelper.Handle($"历史版本：{historyVersion},更新时间：{historyDate}，是否已执行{historyIsRunScript}");

        // 历史版本为空、版本号相同，不执行脚本
        if (historyVersion == string.Empty)
        {
            LogHelper.Handle("历史版本为空，默认为最新版本，不执行脚本");

            // 保存当前版本信息
            SetEntryAssemblyCurrentVersion(currentVersion, true);

            return app;
        }
        else if (CompareVersion(currentVersion, historyVersion) <= 0 && historyIsRunScript)
        {
            LogHelper.Handle("当前版本号与历史版本号相同，且已执行过脚本，不再执行");

            // 保存当前版本信息
            SetEntryAssemblyCurrentVersion(currentVersion, false);

            return app;
        }
        else
        {
            LogHelper.Handle("当前版本号与历史版本号不同，或版本号相同但未执行过脚本，开始执行脚本");

            var scriptSqlVersions = GetScriptSqlVersions();

            // 若不存在当前版本的脚本，则只保存当前版本信息，不执行脚本
            if (scriptSqlVersions.All(s => CompareVersion(s.Version, currentVersion) < 0))
            {
                LogHelper.Handle("不存在当前版本的脚本，只保存当前版本信息，不执行脚本");

                // 保存当前版本信息
                SetEntryAssemblyCurrentVersion(currentVersion, false);

                return app;
            }

            // 执行脚本
            foreach (var sqlFileInfo in scriptSqlVersions)
            {
                var sqlVersion = sqlFileInfo.Version;

                // 只执行大于历史版本的脚本，或者当前版本但未执行过（使用语义化版本比较）
                if (CompareVersion(sqlVersion, historyVersion) < 0)
                {
                    LogHelper.Handle($"版本{sqlVersion}低于历史版本，跳过");
                    continue;
                }
                if (sqlVersion == historyVersion && historyIsRunScript)
                {
                    LogHelper.Handle($"版本{sqlVersion}等于历史版本，且已执行过脚本，跳过");
                    continue;
                }

                // 不执行超过当前程序版本的脚本
                if (CompareVersion(sqlVersion, currentVersion) > 0)
                {
                    LogHelper.Handle($"版本{sqlVersion}高于当前版本{currentVersion}，跳过");
                    continue;
                }

                // 执行脚本
                var sql = File.ReadAllText(sqlFileInfo.FilePath);
                if (sql != null)
                {
                    LogHelper.Handle($"执行版本{sqlVersion}脚本");

                    HandleSqlScript(app, sql, sqlVersion);
                }
            }
        }

        LogHelper.Success("AutoVersionUpdate 中间件结束");

        return app;
    }

    #region 辅助方法

    /// <summary>
    /// 获取入口程序集当前版本信息
    /// </summary>
    /// <returns></returns>
    private static string GetEntryAssemblyCurrentVersion()
    {
        var entryAssemblyVersion = ReflectionHelper.GetEntryAssemblyVersion();
        return entryAssemblyVersion?.ToString(3) ?? "0.0.0";
    }

    /// <summary>
    /// 设置入口程序集当前版本信息
    /// </summary>
    /// <param name="version"></param>
    /// <param name="isRunScript"></param>
    private static void SetEntryAssemblyCurrentVersion(string version, bool isRunScript)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "version.txt");
        var now = DateTime.Now;
        File.WriteAllText(path, $"{version}^{now:yyyy-MM-dd HH:mm:ss}^{isRunScript}");
    }

    /// <summary>
    /// 解析语义化版本号 x.y.z，用于正确比较 10.2.0 与 2.0.0 等
    /// </summary>
    private static bool TryParseVersion(string version, out int major, out int minor, out int patch)
    {
        major = minor = patch = 0;
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        var parts = version.Trim().Split('.');
        if (parts.Length < 1 || !int.TryParse(parts[0], out major))
        {
            return false;
        }

        if (parts.Length >= 2 && !int.TryParse(parts[1], out minor))
        {
            return false;
        }

        if (parts.Length >= 3 && !int.TryParse(parts[2], out patch))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 语义化版本比较：a 小于 b 返回负数，等于返回 0，大于返回正数
    /// </summary>
    private static int CompareVersion(string a, string b)
    {
        TryParseVersion(a, out var ma, out var mia, out var pa);
        TryParseVersion(b, out var mb, out var mib, out var pb);
        if (ma != mb)
        {
            return ma.CompareTo(mb);
        }

        if (mia != mib)
        {
            return mia.CompareTo(mib);
        }

        return pa.CompareTo(pb);
    }

    /// <summary>
    /// 获取入口程序集上一次运行版本信息
    /// </summary>
    /// <returns></returns>
    private static HistoryVersionInfo GetEntryAssemblyHistoryVersionInfo()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "version.txt");

        // 检查文件是否存在
        if (File.Exists(path))
        {
            // 文件存在时读取内容
            var info = File.ReadAllText(path);

            if (info.Contains('^'))
            {
                var parts = info.Split('^');
                var version = parts.Length > 0 ? parts[0].ToString() : string.Empty;
                var date = parts.Length > 1 ? parts[1] : string.Empty;
                var isRunScript = parts.Length > 2 && parts[2].ConvertToBool();

                return new HistoryVersionInfo(version, date, isRunScript);
            }
        }

        // 文件不存在或内容格式不正确时返回默认值
        return new HistoryVersionInfo(string.Empty, string.Empty, false);
    }

    /// <summary>
    /// 获取程序目录下的脚本 SQL 文件版本
    /// </summary>
    /// <returns></returns>
    private static List<SqlFileInfo> GetScriptSqlVersions()
    {
        // 获取所有脚本文件
        var path = Path.Combine(AppContext.BaseDirectory, "UpdateScripts");

        if (Directory.Exists(path))
        {
            var scriptFiles = Directory.GetFiles(path, "*.sql").ToList();

            var sqlVersions = scriptFiles
                .Select(s => new SqlFileInfo(Path.GetFileNameWithoutExtension(s), s))
                .OrderBy(s => s.Version, Comparer<string>.Create(CompareVersion))
                .ToList();
            return sqlVersions;
        }
        return [];
    }

    /// <summary>
    /// 保存当前版本信息
    /// </summary>
    /// <param name="app"></param>
    /// <param name="sql"></param>
    /// <param name="sqlVersion"></param>
    private static void HandleSqlScript(IApplicationBuilder app, string sql, string sqlVersion)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var isSuccess = false;

        try
        {
            // 开启事务
            dbContext.Ado.BeginTran();
            dbContext.Ado.ExecuteCommand(sql);
            dbContext.Ado.CommitTran();
            isSuccess = true;
        }
        catch (Exception ex)
        {
            dbContext.Ado.RollbackTran();
            LogHelper.Error($"AutoVersionUpdate 执行 SQL 脚本出错，版本：{sqlVersion}，错误：{ex.Message}");
        }
        finally
        {
            if (isSuccess)
            {
                // 保存当前版本信息
                SetEntryAssemblyCurrentVersion(sqlVersion, true);
            }
        }
    }

    #endregion 辅助方法
}

/// <summary>
/// 脚本 SQL 文件信息
/// </summary>
/// <param name="Version"></param>
/// <param name="FilePath"></param>
public record SqlFileInfo(string Version, string FilePath);

/// <summary>
/// 历史版本信息
/// </summary>
/// <param name="Version"></param>
/// <param name="Date"></param>
/// <param name="IsRunScript"></param>
public record HistoryVersionInfo(string Version, string Date, bool IsRunScript);
