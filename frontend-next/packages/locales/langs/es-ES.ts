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
      label: 'Sesión',
      signed_in: '{name} · {tenant}',
      platform: 'Plataforma',
      change_password: 'Cambiar contraseña',
      sign_out: 'Cerrar sesión',
      must_change_password: 'Un administrador restableció su contraseña; cámbiela antes de continuar.',
    },
  },
}
