// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: 'Usuarios',
      description: 'Usuarios visibles para el principal actual.',
      fields: {
        user_name: 'Nombre de usuario',
        display_name: 'Nombre para mostrar',
        email: 'Correo',
        status: 'Estado',
        created_at: 'Creado el',
      },
      actions: {
        create: 'Crear usuario',
      },
      empty: 'Todavía no hay usuarios.',
      total: '{total} usuarios',
    },
    login: {
      title: 'Iniciar sesión',
      description: 'Inicie sesión con su nombre de usuario y contraseña.',
      fields: {
        tenant: 'Inquilino',
        user_name: 'Nombre de usuario',
        password: 'Contraseña',
      },
      tenant_placeholder: 'Déjelo vacío para una cuenta de plataforma',
      actions: {
        sign_in: 'Iniciar sesión',
      },
    },
    profile: {
      change_password: {
        title: 'Cambiar contraseña',
        description: 'Introduzca la contraseña actual y una nueva (al menos 8 caracteres con letras y dígitos). Sus demás sesiones se cerrarán.',
        fields: {
          current_password: 'Contraseña actual',
          new_password: 'Nueva contraseña',
          confirm_password: 'Confirmar nueva contraseña',
        },
        mismatch: 'Las dos contraseñas nuevas no coinciden.',
        success: 'Contraseña cambiada; sus demás sesiones se han cerrado.',
        actions: {
          submit: 'Cambiar contraseña',
        },
      },
    },
  },
}
