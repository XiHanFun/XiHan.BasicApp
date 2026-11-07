---
name: xihan-basicapp-commit-msg
description: 根据 XiHan.BasicApp 当前暂存区生成一行提交信息时使用。适用于“提交信息”“commit msg”“msg”或提交前概括 staged changes；只生成消息，不负责暂存、解决冲突或执行提交。
---

# XiHan.BasicApp 提交信息

1. 检查 `git status --short --branch`；存在冲突、merge 或 rebase 时先报告。
2. 只读取 `git diff --cached --stat`、完整 staged diff 和最近 20 条提交。暂存区为空时不从未暂存内容猜测。
3. 将全部暂存变化归纳为一个意图，输出一行 Conventional Commit。

格式：`<type>(<scope>): <中文说明>`，第一人称，64 字以内，不写正文。type 使用 `feat`、`fix`、`refactor`、`perf`、`docs`、`style`、`test`、`build`、`ci`、`chore`、`revert`。

scope 取改动所在的位置：`backend`、`frontend`、`docs`、`plans`、`ci`，或能力包短名（`identity`、`tenancy`、`organization`、`access`、`settings`、`files`、`notification`、`auditing`、`realtime`、`maintenance`、`openplatform`、`workbench`、`printing`、`chat`、`workflow`、`codegeneration`、`ai`、`host`、`runtime`、`contracts`）；跨前后端的同一功能用能力包 scope，不逐文件罗列。

改变操作 ID 或路由、DTO、权限码、页面路由、表结构或公开配置时使用 `!`，除非仓库当前兼容策略明确证明非破坏（首个正式版发布前直接改，不算破坏）。只要求消息时最终只输出一行标题。
