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
      label: 'セッション',
      signed_in: '{name} · {tenant}',
      platform: 'プラットフォーム',
      change_password: 'パスワード変更',
      sign_out: 'ログアウト',
      must_change_password: '管理者によりパスワードがリセットされました。続行する前に変更してください。',
    },
  },
}
