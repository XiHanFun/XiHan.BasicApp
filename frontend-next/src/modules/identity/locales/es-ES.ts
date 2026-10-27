// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: 'Usuarios',
      description: 'Usuarios del inquilino actual (muestra N0 del paquete Identity).',
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
  },
}
