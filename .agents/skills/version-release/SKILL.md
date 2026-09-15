---
name: xihan-basicapp-version-release
description: 为 XiHan.BasicApp 准备版本、同步后端前端与文档版本、整理日志、验证应用、打标签或发布版本时使用。只有用户明确要求实际发布或部署时才允许推送标签和改变外部环境。
---

# XiHan.BasicApp 版本发布

## 前置条件

- 工作区干净，无 merge/rebase、冲突或 detached HEAD；目标版本、发布分支和包含范围明确。
- 版本需要同步核对 `backend/props/version.props`、`frontend/package.json` 和 `docs/package.json`；前端 `lastBuildTime` 使用实际构建日期。
- `docs/changelog.md` 记录用户可感知变化，数据库变化还需对应 `UpdateScripts/<version>/<version>.sql`。
- 仓库当前没有应用包自动发布工作流；不得把 CI 或文档部署当成应用已经发布。

## 准备版本

1. 核对上一 tag 到当前提交的后端、前端、权限、迁移和依赖变化。
2. 运行交互脚本 `pwsh -File backend/scripts/nuget/VersionUpgrade.ps1`；不替用户预选版本级别或通道。
3. 显式同步前端和文档版本，检查 `lastBuildTime`、changelog 标题和升级脚本版本。
4. 运行后端 Release 构建与测试、前端 `pnpm check && pnpm test && pnpm build`、文档 `pnpm build`。
5. 版本提交使用 `build: vX.Y.Z`，并确认提交进入预期发布分支。

## 发布

只有用户明确要求发布，才可创建和推送 `vX.Y.Z[-tag.N]` 标签，或执行用户指定的部署流程。推标签前确认 tag、三个版本源、changelog 和构建产物一致。

发布后核对 tag、CI、文档站和实际部署版本。数据库升级和生产部署属于独立高风险动作，必须明确目标环境、备份和回滚方案；本技能不自动连接生产库、不猜部署平台，也不把文档成功当作应用发布成功。
