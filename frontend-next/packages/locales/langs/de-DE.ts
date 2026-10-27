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
      home: 'Startseite',
    },
    session: {
      label: 'Sitzung',
      signed_in: '{name} · {tenant}',
      platform: 'Plattform',
      change_password: 'Passwort ändern',
      sign_out: 'Abmelden',
      must_change_password: 'Ihr Passwort wurde von einem Administrator zurückgesetzt; ändern Sie es, bevor Sie fortfahren.',
    },
    nav: {
      label: 'Navigation',
    },
    status: {
      forbidden: 'Zugriff verweigert',
      forbidden_hint: 'Sie haben keine Berechtigung für diese Seite.',
      not_found: 'Seite nicht gefunden',
      not_found_hint: 'Unter dieser Adresse gibt es keine Seite.',
      home: 'Keine Seiten verfügbar',
      home_hint: 'Bitten Sie einen Administrator, Ihnen eine Rolle zuzuweisen.',
    },
  },
}
