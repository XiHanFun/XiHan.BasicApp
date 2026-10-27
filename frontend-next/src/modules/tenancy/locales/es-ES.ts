// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  tenancy: {
    tenants: {
      title: 'Inquilinos',
      description: 'Directorio de inquilinos del lado de plataforma: aprovisionar, inicializar el administrador, suspender, reanudar y eliminar.',
      fields: {
        tenant_id: 'ID de inquilino',
        name: 'Nombre',
        description: 'Descripción',
        status: 'Estado',
        admin: 'Administrador',
        order: 'Orden',
        expires_at: 'Caduca el',
        created_at: 'Creado el',
        user_count: 'Usuarios',
      },
      actions: {
        create: 'Crear inquilino',
        save: 'Guardar',
        initialize_admin: 'Inicializar administrador',
        suspend: 'Suspender',
        resume: 'Reanudar',
        delete: 'Eliminar inquilino',
      },
      sections: {
        detail: 'Inquilino',
        admin: 'Administrador',
      },
      admin_form: {
        user_name: 'Nombre de usuario',
        password: 'Contraseña inicial',
        display_name: 'Nombre para mostrar',
      },
      status: {
        active: 'Activo',
        suspended: 'Suspendido',
      },
      empty: 'Aún no hay inquilinos.',
      total: '{total} inquilinos',
      no_admin: 'Aún no hay administrador.',
      saved: 'Inquilino guardado.',
      admin_created: 'Administrador creado; la contraseña debe cambiarse en el primer inicio de sesión.',
    },
  },
}
