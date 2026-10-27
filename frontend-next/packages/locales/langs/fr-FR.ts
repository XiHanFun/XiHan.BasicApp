// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  common: {
    app_name: 'XiHan.BasicApp',
    version: 'v{version}',
    locale: 'Langue',
    actions: {
      refresh: 'Actualiser',
      previous: 'Précédent',
      next: 'Suivant',
    },
    session: {
      token: 'Jeton',
      token_placeholder: 'Collez un jeton issu de pnpm dev:token',
      apply: 'Appliquer',
      signed_in: 'Connecté : {subject} · {tenant}',
    },
  },
}
