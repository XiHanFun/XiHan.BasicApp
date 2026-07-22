// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Options;
using SqlSugar;
using XiHan.Framework.DistributedIds.SnowflakeIds;
using XiHan.Framework.Utils.Extensions;
using XiHan.Framework.Utils.Logging;
using XiHan.Framework.Utils.Reflections;

namespace XiHan.BasicApp.Web.Core.Extensions;

/// <summary>
/// 自动版本更新扩展：应用启动时按语义化版本顺序执行数据库升级脚本
/// </summary>
/// <remarks>
/// 在调用处（模块初始化阶段）同步执行一次即返回，之后不参与任何请求处理。
/// <para>
/// 机制：以程序目录下的 <c>version.txt</c> 记录「上次运行的版本」，与当前程序集版本比对，
/// 执行 <c>UpdateScripts/*.sql</c> 中「高于历史版本、且不高于当前程序版本」的脚本。
/// </para>
/// <para>
/// 本项目无需额外配置：
/// <list type="number">
/// <item>调用点：<c>XiHanBasicAppWebHostModule.OnApplicationInitialization</c>。该模块是依赖图的根，初始化晚于框架 <c>XiHanDataModule</c> 的建表与播种，故脚本执行时表结构与基线数据均已就绪。</item>
/// <item>脚本目录：入口项目 <c>XiHan.BasicApp.WebHost/UpdateScripts/</c>，已在其 csproj 配置 <c>CopyToOutputDirectory</c> 随输出/发布拷贝。</item>
/// <item>仅主节点执行：<c>appsettings.*.json</c> 的 <c>XiHan:DistributedIds:SnowflakeId:WorkerId</c> 已为 1。</item>
/// </list>
/// </para>
/// <para>
/// 发布新版本时：在 <c>UpdateScripts/</c> 下新增以新版本号命名的脚本（如 <c>3.6.0.sql</c>），
/// 并同步抬升 <c>props/version.props</c> 的 <c>Version</c>。二者版本号需匹配，否则脚本会因「高于当前程序版本」被跳过。
/// </para>
/// </remarks>
public static class AutoVersionUpdate
{
    /// <summary>
    /// 执行自动版本更新：按语义化版本升序执行待应用的数据库升级脚本
    /// </summary>
    /// <remarks>
    /// 执行规则：
    /// <list type="bullet">
    /// <item>节点门控：仅主节点（<c>WorkerId == 1</c>）执行，多节点部署下不会重复跑脚本。</item>
    /// <item>脚本来源：<c>AppContext.BaseDirectory/UpdateScripts/*.sql</c>，文件名即版本号（如 <c>3.6.0.sql</c>）。
    /// 若该目录未随输出拷贝而不存在，则静默不执行任何脚本。</item>
    /// <item>全新部署：无 <c>version.txt</c> 时视为已是最新版本，只落版本号、不跑任何历史脚本，
    /// 因此不会在空库上误执行陈旧脚本。</item>
    /// <item>跳过条件：低于历史版本、等于历史版本且已执行过、高于当前程序版本。</item>
    /// <item>事务与失败：每个脚本在独立事务中执行；失败则回滚该脚本并记录错误日志，
    /// <b>但不中断后续脚本</b>，且不写入版本号——下次启动会重试该脚本。</item>
    /// </list>
    /// <para>
    /// 部署提醒：<c>version.txt</c> 写在 <c>AppContext.BaseDirectory</c>。容器化部署若未持久化该目录，
    /// 每次启动都会被判定为「全新部署」，从而永远跳过升级脚本；此时需将其落到持久卷。
    /// </para>
    /// </remarks>
    /// <param name="app">应用构建器</param>
    /// <returns>原样返回 <paramref name="app"/>，以支持链式调用</returns>
    public static IApplicationBuilder UseAutoVersionUpdate(this IApplicationBuilder app)
    {
        LogHelper.Info("AutoVersionUpdate 自动版本更新扩展运行");

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

        // 全新部署（无 version.txt）：视为已是最新版本，只落版本号、不跑任何历史脚本，
        // 避免在空库上误执行陈旧脚本（建表与基线数据由框架的数据库初始化负责）
        if (historyVersion == string.Empty)
        {
            LogHelper.Handle("历史版本为空，默认为最新版本，不执行脚本");

            // 保存当前版本信息
            SetEntryAssemblyCurrentVersion(currentVersion, true);

            return app;
        }
        // 未升级（当前版本不高于历史版本）且历史脚本已执行完：无事可做
        else if (CompareVersion(currentVersion, historyVersion) <= 0 && historyIsRunScript)
        {
            LogHelper.Handle("当前版本号与历史版本号相同，且已执行过脚本，不再执行");

            // 保存当前版本信息
            SetEntryAssemblyCurrentVersion(currentVersion, false);

            return app;
        }
        // 已升级（当前版本高于历史版本），或版本持平但历史脚本未成功执行（上次失败）：尝试执行脚本
        else
        {
            LogHelper.Handle("当前版本号与历史版本号不同，或版本号相同但未执行过脚本，开始执行脚本");

            var scriptSqlVersions = GetScriptSqlVersions();

            // 所有脚本版本都低于当前程序版本 ⇒ 本次升级没有配套脚本，只记录版本号
            if (scriptSqlVersions.All(s => CompareVersion(s.Version, currentVersion) < 0))
            {
                LogHelper.Handle("不存在当前版本的脚本，只保存当前版本信息，不执行脚本");

                // 保存当前版本信息
                SetEntryAssemblyCurrentVersion(currentVersion, false);

                return app;
            }

            // 按语义化版本升序逐个执行落在 (历史版本, 当前程序版本] 区间内的脚本
            foreach (var sqlFileInfo in scriptSqlVersions)
            {
                var sqlVersion = sqlFileInfo.Version;

                // 低于历史版本：早已应用过
                if (CompareVersion(sqlVersion, historyVersion) < 0)
                {
                    LogHelper.Handle($"版本{sqlVersion}低于历史版本，跳过");
                    continue;
                }
                // 等于历史版本：仅当上次已成功执行才跳过；否则重试（覆盖上次失败的场景）
                if (sqlVersion == historyVersion && historyIsRunScript)
                {
                    LogHelper.Handle($"版本{sqlVersion}等于历史版本，且已执行过脚本，跳过");
                    continue;
                }

                // 高于当前程序版本：属于「未来版本」的脚本，本次不应用
                if (CompareVersion(sqlVersion, currentVersion) > 0)
                {
                    LogHelper.Handle($"版本{sqlVersion}高于当前版本{currentVersion}，跳过");
                    continue;
                }

                // 执行脚本（单事务；失败只回滚本脚本并记录错误，不中断后续脚本）
                var sql = File.ReadAllText(sqlFileInfo.FilePath);
                if (sql != null)
                {
                    LogHelper.Handle($"执行版本{sqlVersion}脚本");

                    HandleSqlScript(app, sql, sqlVersion);
                }
            }
        }

        LogHelper.Success("AutoVersionUpdate 自动版本更新扩展结束");

        return app;
    }

    #region 辅助方法

    /// <summary>
    /// 获取当前程序版本号（入口程序集版本，取 x.y.z 三段）
    /// </summary>
    /// <returns>形如 <c>3.5.0</c> 的版本号；无法取得程序集版本时回退为 <c>0.0.0</c>（该值低于任何脚本版本，等价于不执行脚本）</returns>
    private static string GetEntryAssemblyCurrentVersion()
    {
        var entryAssemblyVersion = ReflectionHelper.GetEntryAssemblyVersion();
        return entryAssemblyVersion?.ToString(3) ?? "0.0.0";
    }

    /// <summary>
    /// 写入版本记录文件 <c>version.txt</c>（覆盖写），供下次启动比对
    /// </summary>
    /// <remarks>
    /// 位于 <c>AppContext.BaseDirectory</c>，格式为 <c>版本^写入时间^是否已执行脚本</c>，
    /// 例如 <c>3.5.0^2026-07-14 16:40:00^True</c>。
    /// </remarks>
    /// <param name="version">要记录的版本号（脚本执行成功时记的是该脚本版本，而非程序版本）</param>
    /// <param name="isRunScript">该版本的脚本是否已成功执行；为 false 时下次启动会再次尝试执行</param>
    private static void SetEntryAssemblyCurrentVersion(string version, bool isRunScript)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "version.txt");
        var now = DateTime.Now;
        File.WriteAllText(path, $"{version}^{now:yyyy-MM-dd HH:mm:ss}^{isRunScript}");
    }

    /// <summary>
    /// 解析语义化版本号 x.y.z，用于正确比较 10.2.0 与 2.0.0 等（字符串直接比较会误判 "10" &lt; "2"）
    /// </summary>
    /// <param name="version">待解析的版本号字符串，缺省段按 0 处理（如 "3.5" ⇒ 3.5.0）</param>
    /// <param name="major">主版本号</param>
    /// <param name="minor">次版本号</param>
    /// <param name="patch">修订号</param>
    /// <returns>解析是否成功；失败时三个输出参数均为 0</returns>
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
    /// 语义化版本比较：依次比较主/次/修订号
    /// </summary>
    /// <remarks>无法解析的版本号按 0.0.0 参与比较（<see cref="TryParseVersion"/> 失败时输出全 0），不会抛异常。</remarks>
    /// <param name="a">左操作数</param>
    /// <param name="b">右操作数</param>
    /// <returns><paramref name="a"/> 小于 <paramref name="b"/> 返回负数，等于返回 0，大于返回正数</returns>
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
    /// 读取上次运行的版本记录（<c>version.txt</c>）
    /// </summary>
    /// <returns>
    /// 解析出的历史版本信息；文件不存在或格式不符（缺少分隔符 <c>^</c>）时返回空版本
    /// （<c>Version = string.Empty</c>），调用方据此判定为「全新部署」。
    /// </returns>
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
    /// 扫描程序目录下的升级脚本，按语义化版本升序返回
    /// </summary>
    /// <remarks>以文件名（不含扩展名）作为版本号，故 <c>UpdateScripts/</c> 下的 .sql 必须以版本号命名（如 <c>3.6.0.sql</c>）。</remarks>
    /// <returns>按版本升序排列的脚本文件列表；<c>UpdateScripts</c> 目录不存在时返回空列表（即静默不执行任何脚本）</returns>
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
    /// 在单个事务中执行一个版本的 SQL 脚本，成功后才记录该版本为「已执行」
    /// </summary>
    /// <remarks>
    /// 失败处理：回滚该脚本并记录错误日志，<b>不抛异常、不中断后续脚本</b>；由于未写入版本号，
    /// 下次启动会重试该脚本。因此脚本应尽量写成可重复执行（幂等）的形式。
    /// <para>脚本在独立 DI 作用域中取 <see cref="ISqlSugarClient"/> 执行，与请求作用域无关。</para>
    /// </remarks>
    /// <param name="app">应用构建器，用于创建 DI 作用域解析数据库客户端</param>
    /// <param name="sql">脚本内容</param>
    /// <param name="sqlVersion">该脚本对应的版本号，执行成功后写入 version.txt</param>
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
/// 升级脚本文件信息
/// </summary>
/// <param name="Version">版本号，取自文件名（不含扩展名），如 <c>3.6.0</c></param>
/// <param name="FilePath">脚本文件的完整路径</param>
public record SqlFileInfo(string Version, string FilePath);

/// <summary>
/// 版本记录文件（<c>version.txt</c>）的解析结果
/// </summary>
/// <param name="Version">上次运行的版本号；为空字符串表示无记录（全新部署）</param>
/// <param name="Date">该记录的写入时间，仅用于日志排查</param>
/// <param name="IsRunScript">该版本的脚本是否已成功执行；false 表示下次启动会重试</param>
public record HistoryVersionInfo(string Version, string Date, bool IsRunScript);
