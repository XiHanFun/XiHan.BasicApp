// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  common: {
    app_name: 'XiHan.BasicApp',
    version: 'v{version}',
    locale: 'Idioma',
    actions: {
      refresh: 'Atualizar',
      previous: 'Anterior',
      next: 'Próxima',
    },
    session: {
      label: 'Sessão',
      signed_in: '{name} · {tenant}',
      platform: 'Plataforma',
      change_password: 'Alterar senha',
      sign_out: 'Sair',
      must_change_password: 'Sua senha foi redefinida por um administrador; altere-a antes de continuar.',
    },
  },
}
