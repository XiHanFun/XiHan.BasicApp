// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  common: {
    app_name: 'XiHan.BasicApp',
    version: 'v{version}',
    locale: 'Idioma',
    actions: {
      refresh: 'Actualizar',
      previous: 'Anterior',
      next: 'Siguiente',
    },
    session: {
      token: 'Token',
      token_placeholder: 'Pega un token de pnpm dev:token',
      apply: 'Aplicar',
      signed_in: 'Sesión de {subject} · {tenant}',
    },
  },
}
