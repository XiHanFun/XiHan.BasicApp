// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  common: {
    app_name: '曦寒基础应用',
    version: 'v{version}',
    locale: '语言',
    actions: {
      refresh: '刷新',
      previous: '上一页',
      next: '下一页',
    },
    session: {
      token: '令牌',
      token_placeholder: '粘贴 pnpm dev:token 签出的令牌',
      apply: '应用',
      signed_in: '已登录：{subject} · {tenant}',
    },
  },
}
