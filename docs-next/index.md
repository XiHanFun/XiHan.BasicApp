---
layout: home
title: 曦寒基础应用 6.0
titleTemplate: XiHan.BasicApp 6.0 文档

hero:
  name: 曦寒基础应用 6.0
  text: 基于 XiHan.Framework 5 的能力包式中后台应用
  tagline: 6.0 预览版文档。后端基于 XiHan.Framework 5，前端基于 XiHan.UI；一份操作契约同时发布为 HTTP、gRPC 与 MCP，内置多租户与多层授权。
  image:
    src: /images/logo.png
    alt: 曦寒基础应用
  actions:
    - theme: brand
      text: 快速开始
      link: /getting-started

    - theme: alt
      text: 应用简介
      link: /introduction

    - theme: alt
      text: 为什么是 6.0
      link: /why

features:
  - title: 能力包 + 操作契约
    details: 每个业务能力是一个 IFeature，每个公开方法是一个 [Operation]；路由、OpenAPI、gRPC 方法与 MCP 工具都从同一份操作登记推导，不使用 Controller。
    link: /backend/introduction
    linkText: "了解架构"

  - title: 多租户
    details: 平台库实体不实现 IMultiTenant，租户库实体由框架仓储严格过滤并可随租户分库；租户 ID 是字符串，切换租户换令牌、整页重建。
    link: /backend/multi-tenancy
    linkText: "了解多租户"

  - title: 多层授权
    details: 授予来自框架 Security.Database，Deny、角色继承、限时委托、套餐门控与 ABAC 条件逐层收窄；数据范围与字段安全在查询与投影处显式套用。
    link: /backend/permission
    linkText: "了解权限模型"

  - title: 前端契约由后端生成
    details: 后端导出一份 openapi.json，前端生成类型、操作客户端、权限码与枚举；CI 重新生成后比对，不一致即失败。
    link: /frontend/request
    linkText: "了解契约层"

  - title: RFC 9457 错误与无信封响应
    details: 成功响应就是 DTO，失败是 application/problem+json；客户端只按 code 分支，字段级 errors 直接挂回表单。
    link: /api-guide
    linkText: "查看接口对接指南"

  - title: 结构约束由测试保证
    details: 包间层级、Expose 显式、权限码文法、匿名白名单、十种语言键集、XML 文档与框架锁定提交都由架构测试检查。
    link: /backend/development
    linkText: "了解开发流程"
---

<div class="bap-status">
<span class="bap-eyebrow">预览阶段</span>
<h2 class="bap-title">开发进度</h2>
<p class="bap-desc">6.0 在长期分支 <code>next</code> 上从零重写，代码在 <code>backend-next/</code> 与 <code>frontend-next/</code>，尚未发布。<br />下表按总体方案的分期列出各阶段状态；规划中的模块在文档中单独标注。</p>
<div class="bap-grid">
<div class="bap-card bap-card--done"><b>N0 骨架与验证</b><span>已完成</span><p>Host、Contracts、Runtime、架构测试与前端契约生成；HTTP / gRPC / MCP 三协议冒烟。</p></div>
<div class="bap-card bap-card--done"><b>N1 身份与租户基座</b><span>已完成</span><p>Identity、Tenancy、Organization、Access 与前端壳层、登录、核心管理页。</p></div>
<div class="bap-card bap-card--doing"><b>N2 平台能力</b><span>八批已落地，收尾中</span><p>Settings、Auditing、Files、Realtime、Notification、OpenPlatform、Maintenance、Workbench；正在补齐对照 5.x 的收尾差距。</p></div>
<div class="bap-card bap-card--todo"><b>N3 可选模块</b><span>规划中</span><p>Workflow（含审批中心）、Printing、CodeGeneration、Chat。</p></div>
<div class="bap-card bap-card--todo"><b>N4 AI</b><span>规划中</span><p>AI 提供商、知识库与助手，架在框架 AI 子系统之上。</p></div>
<div class="bap-card bap-card--todo"><b>N5 / N6 对齐与切换</b><span>未开始</span><p>对齐矩阵核销、文档切到 6.0、目录 <code>git mv</code> 与发布 6.0.0。</p></div>
</div>
<div class="bap-actions">
<a class="bap-btn" href="/getting-started">快速开始</a>
<a class="bap-link" href="/changelog">看进度记录</a>
</div>
<p class="bap-cred">初始账号 <code>superadmin</code> · 密码 <code>SuperAdmin@123</code>（由 <code>seed</code> 写入，首次登录后请修改）</p>
</div>

<style>
.bap-status {
  max-width: 1080px;
  margin: 96px auto 0;
  padding: 0 24px;
  text-align: center;
}
/* 选择器均带 .bap-status 前缀以覆盖 VitePress 默认的 .vp-doc h2 / a / p 样式 */
.bap-status .bap-eyebrow {
  display: inline-block;
  padding: 5px 14px;
  border-radius: 999px;
  background: var(--vp-c-brand-soft);
  color: var(--vp-c-brand-1);
  font-size: 13px;
  font-weight: 600;
  letter-spacing: 0.08em;
}
.bap-status h2.bap-title {
  font-size: 32px;
  line-height: 1.2;
  font-weight: 800;
  border-top: none;
  margin: 18px 0 14px;
  padding-top: 0;
  letter-spacing: -0.02em;
}
.bap-status p.bap-desc {
  max-width: 680px;
  margin: 0 auto 32px;
  color: var(--vp-c-text-2);
  line-height: 1.75;
}
.bap-status .bap-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
  text-align: left;
}
.bap-status .bap-card {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 18px 20px;
  border-radius: 12px;
  border: 1px solid var(--vp-c-divider);
  background: var(--vp-c-bg-soft);
}
.bap-status .bap-card b {
  font-size: 15px;
}
.bap-status .bap-card span {
  align-self: flex-start;
  padding: 2px 10px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 600;
}
.bap-status .bap-card p {
  margin: 4px 0 0;
  color: var(--vp-c-text-2);
  font-size: 13.5px;
  line-height: 1.65;
}
.bap-status .bap-card--done span { background: var(--vp-c-green-soft); color: var(--vp-c-green-1); }
.bap-status .bap-card--doing span { background: var(--vp-c-yellow-soft); color: var(--vp-c-yellow-1); }
.bap-status .bap-card--todo span { background: var(--vp-c-default-soft); color: var(--vp-c-text-2); }
.bap-status .bap-actions {
  display: flex;
  gap: 14px;
  justify-content: center;
  align-items: center;
  flex-wrap: wrap;
  margin: 36px 0 14px;
}
.bap-status a.bap-btn {
  display: inline-flex;
  align-items: center;
  padding: 12px 30px;
  border-radius: 999px;
  background: var(--vp-c-brand-1);
  color: #fff;
  font-weight: 600;
  text-decoration: none;
  transition: background 0.25s ease, transform 0.25s ease;
}
.bap-status a.bap-btn:hover {
  background: var(--vp-c-brand-2);
  color: #fff;
  transform: translateY(-2px);
}
.bap-status a.bap-link {
  display: inline-flex;
  align-items: center;
  padding: 12px 24px;
  border-radius: 999px;
  border: 1px solid var(--vp-c-divider);
  color: var(--vp-c-text-1);
  font-weight: 600;
  text-decoration: none;
  transition: border-color 0.25s ease, color 0.25s ease;
}
.bap-status a.bap-link:hover {
  border-color: var(--vp-c-brand-1);
  color: var(--vp-c-brand-1);
}
.bap-status p.bap-cred {
  margin-top: 8px;
  color: var(--vp-c-text-3);
  font-size: 13.5px;
}
@media (max-width: 960px) {
  .bap-status .bap-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
@media (max-width: 640px) {
  .bap-status { margin-top: 64px; }
  .bap-status h2.bap-title { font-size: 26px; }
  .bap-status .bap-grid { grid-template-columns: minmax(0, 1fr); }
}
</style>
