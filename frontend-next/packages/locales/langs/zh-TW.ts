// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  common: {
    app_name: '曦寒基礎應用',
    version: 'v{version}',
    locale: '語言',
    actions: {
      refresh: '重新整理',
      previous: '上一頁',
      next: '下一頁',
    },
    session: {
      token: '權杖',
      token_placeholder: '貼上 pnpm dev:token 簽出的權杖',
      apply: '套用',
      signed_in: '已登入：{subject} · {tenant}',
    },
  },
}
