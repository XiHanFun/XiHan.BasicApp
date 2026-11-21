---
name: xihan-basicapp-version-release
description: 为 XiHan.BasicApp 准备版本、同步后端前端与文档版本、整理日志、验证应用、打标签、创建 GitHub Release 或发布版本时使用。只有用户明确要求实际发布或部署时才允许推送标签、创建 Release 和改变外部环境。
---

# XiHan.BasicApp 版本发布

## 前置条件

- 工作区干净，无 merge/rebase、冲突或 detached HEAD；目标版本、发布分支和包含范围明确。
- 版本源有四处，必须同一个值：`backend/Directory.Build.props` 的 `<Version>`、`frontend/package.json` 和 `docs/package.json` 的 `version`、`backend/src/XiHan.BasicApp.Host/xihan.app.json` 的镜像 `tag`。改号后重新导出契约（`openapi.json` 的 `info.version` 跟着变），与版本号同一提交。
- 发布分支：预览版（带预发布后缀）从 `preview` 发，正式版从 `main` 发，都由开发分支 `dev` 快进；标签打在对应分支的历史上。
- `docs/changelog.md` 记录用户可感知变化；ORM 做不了的表结构改动另有新的结构版本目录 `backend/src/XiHan.BasicApp.Host/Schema/<版本>/<库类型>/*.sql`（结构版本与应用版本号分开记，见 `docs/backend/upgrade.md`）。
- 推送版本标签触发 `release.yml`：guard 核对标签在对应发布分支的历史里、四处版本号与标签一致，通过后按 `backend/Dockerfile` 构建后端镜像，推到 `xihan.app.json` 登记的 GHCR 仓库（tag 是版本号，正式版另打 `latest`）。前端镜像与 NuGet 包不发布；不得把 CI 或文档部署当成应用已经发布。

## 准备版本

1. 核对上一 tag 到当前提交的后端、前端、权限、表结构和依赖变化。
2. 四处版本号手工改成同一个值；不替用户预选版本级别或通道。
3. 检查 changelog 标题与版本号一致；结构版本升了的，在升级须知里写明。
4. 运行后端 Release 构建与测试、前端 `pnpm check && pnpm test && pnpm build`、文档 `pnpm build`。
5. 版本提交使用 `build: vX.Y.Z`，并确认提交进入对应的发布分支：预览版 `preview`，正式版 `main`。

## 发布

只有用户明确要求发布，才可创建和推送 `vX.Y.Z[-tag.N]` 标签、创建 GitHub Release，或执行用户指定的部署流程。推标签前确认 tag、四个版本源、changelog 和构建产物一致；推标签即发布镜像。

1. 把发布分支快进到版本提交（预览版 `git fetch . dev:preview`，正式版 `git fetch . dev:main`，不是快进就停下查），打附注标签 `git tag -a vX.Y.Z -m "vX.Y.Z"`，推送开发分支、发布分支与标签。
2. 看「发布镜像」那次运行（`release.yml`）成功、镜像 `ghcr.io/xihanfun/xihan-basicapp:<版本>` 可拉取。失败修好后不重打标签，用 `gh workflow run release.yml --repo XiHanFun/XiHan.BasicApp -f tag=vX.Y.Z` 按原标签补发（工作流要已在缺省分支 `main` 上）。
3. 创建 GitHub Release：标题即标签名，正文取 `docs/changelog.md` 中该版本的条目（不含版本标题）。VitePress 的 `::: warning <标题>` 改写为 `> [!WARNING]` 引用块，首行 `**<标题>**`；站内相对链接改为 `https://basicapp.docs.xihanfun.com/` 下的绝对地址（去掉 `.md`）。文档站只从 `main` 部署，发预览版时 `main` 没跟着快进的，链接改指标签上的仓库文件（`https://github.com/XiHanFun/XiHan.BasicApp/blob/<标签>/docs/<路径>.md`，锚点按 GitHub 的规则）。不上传附件。
   - 稳定版：`gh release create vX.Y.Z --repo XiHanFun/XiHan.BasicApp --verify-tag --title vX.Y.Z --latest --notes-file <正文文件>`
   - 带 `-alpha` / `-beta` / `-preview` / `-rc` 的预发布版把 `--latest` 换成 `--prerelease`。

发布后核对 tag、镜像、GitHub Release、CI、文档站和实际部署版本。数据库升级和生产部署属于独立高风险动作，必须明确目标环境、备份和回滚方案；本技能不自动连接生产库、不猜部署平台，也不把文档成功当作应用发布成功。
