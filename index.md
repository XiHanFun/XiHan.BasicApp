来源：https://basicapp.docs.xihanfun.com/

# 曦寒基础应用

<div class="bap-preview">
<span class="bap-eyebrow">在线演示</span>
<h2 class="bap-title">不用本地搭建，直接看它长什么样</h2>
<p class="bap-desc">演示环境部署的就是本仓库的代码，多租户、权限、代码生成、实时通信都可以直接点开试。<br />点击下方窗口进入。</p>
<div class="bap-window">
<div class="bap-bar"><span class="bap-dots"><span class="bap-dot bap-dot--r"></span><span class="bap-dot bap-dot--y"></span><span class="bap-dot bap-dot--g"></span></span><span class="bap-url">basicapp.xihanfun.com</span></div>
<a class="bap-screen" href="https://basicapp.xihanfun.com" target="_blank" rel="noreferrer">
<img class="bap-img bap-img--light" src="/images/basicapp-preview.png" alt="曦寒基础应用在线预览" />
<img class="bap-img bap-img--dark" src="/images/basicapp-preview-dark.png" alt="曦寒基础应用在线预览（暗色）" />
</a>
</div>
<div class="bap-actions">
<a class="bap-btn" href="https://basicapp.xihanfun.com" target="_blank" rel="noreferrer">立即在线体验 →</a>
<a class="bap-link" href="/getting-started">在本地跑起来</a>
</div>
<p class="bap-cred">演示账号 <code>superadmin</code> · 密码 <code>SuperAdmin@123</code> · 演示环境，请勿录入真实数据</p>
</div>

<style>
.bap-preview {
  max-width: 1080px;
  margin: 112px auto 0;
  padding: 0 24px;
  text-align: center;
}
/* 选择器均带 .bap-preview 前缀以覆盖 VitePress 默认的 .vp-doc h2 / a / p / img 样式 */
.bap-preview .bap-eyebrow {
  display: inline-block;
  padding: 5px 14px;
  border-radius: 999px;
  background: var(--vp-c-brand-soft);
  color: var(--vp-c-brand-1);
  font-size: 13px;
  font-weight: 600;
  letter-spacing: 0.08em;
}
.bap-preview h2.bap-title {
  font-size: 32px;
  line-height: 1.2;
  font-weight: 800;
  border-top: none;
  margin: 18px 0 14px;
  padding-top: 0;
  letter-spacing: -0.02em;
}
.bap-preview p.bap-desc {
  max-width: 640px;
  margin: 0 auto 36px;
  color: var(--vp-c-text-2);
  line-height: 1.75;
}
.bap-preview .bap-window {
  max-width: 1040px;
  margin: 0 auto;
  border-radius: 16px;
  overflow: hidden;
  border: 1px solid var(--vp-c-divider);
  background: var(--vp-c-bg-soft);
  box-shadow: 0 24px 60px -18px rgba(0, 0, 0, 0.3);
  transition: transform 0.35s ease, box-shadow 0.35s ease;
}
.bap-preview .bap-window:hover {
  transform: translateY(-6px);
  box-shadow: 0 36px 72px -18px rgba(0, 0, 0, 0.4);
}
.bap-preview .bap-bar {
  position: relative;
  display: flex;
  align-items: center;
  height: 42px;
  padding: 0 16px;
  background: var(--vp-c-bg-alt);
  border-bottom: 1px solid var(--vp-c-divider);
}
.bap-preview .bap-dots {
  position: absolute;
  left: 16px;
  display: flex;
  gap: 8px;
}
.bap-preview .bap-dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
}
.bap-preview .bap-dot--r { background: #ff5f57; }
.bap-preview .bap-dot--y { background: #febc2e; }
.bap-preview .bap-dot--g { background: #28c840; }
.bap-preview .bap-url {
  margin: 0 auto;
  padding: 4px 18px;
  min-width: 240px;
  border-radius: 8px;
  background: var(--vp-c-bg);
  color: var(--vp-c-text-3);
  font-size: 13px;
}
.bap-preview .bap-screen {
  display: block;
}
.bap-preview .bap-img {
  display: block;
  width: 100%;
  height: auto;
}
.bap-preview .bap-img--dark {
  display: none;
}
.dark .bap-preview .bap-img--light {
  display: none;
}
.dark .bap-preview .bap-img--dark {
  display: block;
}
.bap-preview .bap-actions {
  display: flex;
  gap: 14px;
  justify-content: center;
  align-items: center;
  flex-wrap: wrap;
  margin: 36px 0 14px;
}
.bap-preview a.bap-btn {
  display: inline-flex;
  align-items: center;
  padding: 12px 30px;
  border-radius: 999px;
  background: var(--vp-c-brand-1);
  color: #fff;
  font-weight: 600;
  text-decoration: none;
  box-shadow: 0 10px 22px -8px rgba(0, 0, 0, 0.24);
  transition: background 0.25s ease, transform 0.25s ease, box-shadow 0.25s ease;
}
.bap-preview a.bap-btn:hover {
  background: var(--vp-c-brand-2);
  color: #fff;
  transform: translateY(-2px);
  box-shadow: 0 14px 28px -8px rgba(0, 0, 0, 0.3);
}
.bap-preview a.bap-link {
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
.bap-preview a.bap-link:hover {
  border-color: var(--vp-c-brand-1);
  color: var(--vp-c-brand-1);
}
.bap-preview p.bap-cred {
  margin-top: 8px;
  color: var(--vp-c-text-3);
  font-size: 13.5px;
}
@media (max-width: 640px) {
  .bap-preview { margin-top: 72px; }
  .bap-preview h2.bap-title { font-size: 26px; }
  .bap-preview .bap-url {
    min-width: 0;
    max-width: 160px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
}
</style>
