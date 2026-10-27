// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  common: {
    app_name: 'XiHan.BasicApp',
    version: 'v{version}',
    locale: '言語',
    actions: {
      refresh: '更新',
      previous: '前へ',
      next: '次へ',
    },
    session: {
      token: 'トークン',
      token_placeholder: 'pnpm dev:token で発行したトークンを貼り付け',
      apply: '適用',
      signed_in: 'ログイン中：{subject} · {tenant}',
    },
  },
}
