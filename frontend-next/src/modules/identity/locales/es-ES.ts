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
        phone: 'Teléfono',
        last_login_at: 'Último inicio de sesión',
      },
      actions: {
        create: 'Crear usuario',
        lock: 'Bloquear',
        unlock: 'Desbloquear',
        reset_password: 'Restablecer contraseña',
        delete: 'Eliminar usuario',
        save_roles: 'Guardar roles',
        add_department: 'Añadir a departamento',
        remove_department: 'Quitar',
      },
      empty: 'Todavía no hay usuarios.',
      total: '{total} usuarios',
      sections: {
        account: 'Cuenta',
        roles: 'Roles',
        departments: 'Departamentos',
      },
      reset_form: {
        new_password: 'Nueva contraseña',
        submit: 'Restablecer',
      },
      member_form: {
        department: 'Departamento',
        position: 'Puesto',
      },
      status: {
        active: 'Activo',
        disabled: 'Deshabilitado',
        locked: 'Bloqueado',
      },
      roles_saved: 'Roles guardados.',
      password_reset: 'Contraseña restablecida; el usuario debe cambiarla en el próximo inicio de sesión.',
      department_added: 'Añadido al departamento.',
      locked: 'Usuario bloqueado.',
      no_roles: 'No hay roles disponibles.',
      no_departments: 'No pertenece a ningún departamento.',
      primary_badge: 'Principal',
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
      subtitle: 'Introduzca su nombre de usuario y contraseña para iniciar sesión',
      user_name_placeholder: 'Nombre de usuario o correo',
      password_placeholder: 'Contraseña',
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
