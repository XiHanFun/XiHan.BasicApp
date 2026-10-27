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
      label: 'Session',
      signed_in: '{name} · {tenant}',
      platform: 'Platform',
      change_password: 'Change password',
      sign_out: 'Sign out',
      must_change_password: 'Your password was reset by an administrator; change it before continuing.',
    },
  },
}
