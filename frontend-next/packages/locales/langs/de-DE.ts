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
      token: 'Token',
      token_placeholder: 'Token aus pnpm dev:token einfügen',
      apply: 'Übernehmen',
      signed_in: 'Angemeldet als {subject} · {tenant}',
    },
  },
}
