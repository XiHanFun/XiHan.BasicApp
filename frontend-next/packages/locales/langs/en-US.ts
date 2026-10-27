// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  common: {
    app_name: 'XiHan.BasicApp',
    version: 'v{version}',
    locale: 'Locale',
    actions: {
      refresh: 'Refresh',
      previous: 'Previous',
      next: 'Next',
    },
    session: {
      token: 'Token',
      token_placeholder: 'Paste a token from pnpm dev:token',
      apply: 'Apply',
      signed_in: 'Signed in as {subject} · {tenant}',
    },
  },
}
