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
      home: 'Inicio',
    },
    session: {
      label: 'Sesión',
      signed_in: '{name} · {tenant}',
      platform: 'Plataforma',
      change_password: 'Cambiar contraseña',
      sign_out: 'Cerrar sesión',
      must_change_password: 'Un administrador restableció su contraseña; cámbiela antes de continuar.',
    },
    nav: {
      label: 'Navegación',
    },
    status: {
      forbidden: 'Acceso denegado',
      forbidden_hint: 'No tiene permiso para esta página.',
      not_found: 'Página no encontrada',
      not_found_hint: 'La dirección no corresponde a ninguna página.',
      home: 'No hay páginas disponibles',
      home_hint: 'Pida a un administrador que le asigne un rol.',
    },
  },
}
