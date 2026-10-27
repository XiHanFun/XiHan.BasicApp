// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  common: {
    app_name: 'XiHan.BasicApp',
    version: 'v{version}',
    locale: 'Sprache',
    actions: {
      refresh: 'Aktualisieren',
      previous: 'Zurück',
      next: 'Weiter',
    },
    session: {
      label: 'Sitzung',
      signed_in: '{name} · {tenant}',
      platform: 'Plattform',
      change_password: 'Passwort ändern',
      sign_out: 'Abmelden',
      must_change_password: 'Ihr Passwort wurde von einem Administrator zurückgesetzt; ändern Sie es, bevor Sie fortfahren.',
    },
  },
}
