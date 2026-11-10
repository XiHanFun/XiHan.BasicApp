来源：https://basicapp.docs.xihanfun.com/

# 曦寒基础应用

<div class="bap-status">
<h2 class="bap-title">开始使用</h2>
<p class="bap-desc">按快速开始在本机建库，启动后端（启动时自动升级表结构）、写入种子，再启动前端。</p>
<div class="bap-actions">
<a class="bap-btn" href="/getting-started">快速开始</a>
<a class="bap-link" href="/changelog">更新日志</a>
</div>
<p class="bap-cred">开发环境初始账号 <code>superadmin</code> · 密码 <code>SuperAdmin@123</code>（<code>appsettings.Development.json</code> 配置，由 <code>seed</code> 写入）</p>
</div>

<style>
.bap-status {
  max-width: 1080px;
  margin: 96px auto 0;
  padding: 0 24px;
  text-align: center;
}
/* 选择器均带 .bap-status 前缀以覆盖 VitePress 默认的 .vp-doc h2 / a / p 样式 */
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
@media (max-width: 640px) {
  .bap-status { margin-top: 64px; }
  .bap-status h2.bap-title { font-size: 26px; }
}
</style>
