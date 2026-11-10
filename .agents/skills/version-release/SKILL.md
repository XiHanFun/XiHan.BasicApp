---
name: xihan-basicapp-version-release
description: 为 XiHan.BasicApp 准备版本、同步后端前端与文档版本、整理日志、验证应用、打标签、创建 GitHub Release 或发布版本时使用。只有用户明确要求实际发布或部署时才允许推送标签、创建 Release 和改变外部环境。
---

# XiHan.BasicApp 版本发布

## 前置条件

- 工作区干净，无 merge/rebase、冲突或 detached HEAD；目标版本、发布分支和包含范围明确。
- 版本源有三处，必须同一个值：`backend/Directory.Build.props` 的 `<Version>`、`frontend/package.json` 和 `docs/package.json` 的 `version`。
- 发布分支：预览版（带预发布后缀）从 `preview` 发，正式版从 `main` 发，都由开发分支 `dev` 快进；标签打在对应分支的历史上。
- `docs/changelog.md` 记录用户可感知变化；表结构变化还需对应版本目录 `backend/src/XiHan.BasicApp.Host/Schema/<版本>/*.sql`。
- 仓库当前没有应用包自动发布工作流；不得把 CI 或文档部署当成应用已经发布。

## 准备版本

1. 核对上一 tag 到当前提交的后端、前端、权限、表结构和依赖变化。
2. 三处版本号手工改成同一个值；不替用户预选版本级别或通道。
3. 检查 changelog 标题、表结构脚本的版本目录与版本号一致。
4. 运行后端 Release 构建与测试、前端 `pnpm check && pnpm test && pnpm build`、文档 `pnpm build`。
5. 版本提交使用 `build: vX.Y.Z`，并确认提交进入对应的发布分支：预览版 `preview`，正式版 `main`。

## 发布

只有用户明确要求发布，才可创建和推送 `vX.Y.Z[-tag.N]` 标签、创建 GitHub Release，或执行用户指定的部署流程。推标签前确认 tag、三个版本源、changelog 和构建产物一致。

1. 把发布分支快进到版本提交（预览版 `git fetch . dev:preview`，正式版 `git fetch . dev:main`，不是快进就停下查），打附注标签 `git tag -a vX.Y.Z -m "vX.Y.Z"`，推送开发分支、发布分支与标签。
2. 标签推送后创建 GitHub Release：标题即标签名，正文取 `docs/changelog.md` 中该版本的条目（不含版本标题）。VitePress 的 `::: warning` 升级须知改写为 `> [!WARNING]` 引用块，首行 `**升级须知**`；站内相对链接改为 `https://basicapp.docs.xihanfun.com/` 下的绝对地址（去掉 `.md`）。不上传附件。
   - 稳定版：`gh release create vX.Y.Z --repo XiHanFun/XiHan.BasicApp --verify-tag --title vX.Y.Z --latest --notes-file <正文文件>`
   - 带 `-alpha` / `-beta` / `-preview` / `-rc` 的预发布版把 `--latest` 换成 `--prerelease`。

发布后核对 tag、GitHub Release、CI、文档站和实际部署版本。数据库升级和生产部署属于独立高风险动作，必须明确目标环境、备份和回滚方案；本技能不自动连接生产库、不猜部署平台，也不把文档成功当作应用发布成功。
