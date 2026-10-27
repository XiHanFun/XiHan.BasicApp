// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  access: {
    roles: {
      title: 'Roles',
      description: 'Roles del lado actual, sus permisos y miembros.',
      fields: {
        code: 'Código',
        name: 'Nombre',
        description: 'Descripción',
        status: 'Estado',
        built_in: 'Integrado',
        created_at: 'Creado el',
      },
      actions: {
        create: 'Crear rol',
        save_permissions: 'Guardar permisos',
        delete: 'Eliminar rol',
        add_member: 'Añadir miembro',
        remove_member: 'Quitar',
      },
      sections: {
        permissions: 'Permisos',
        members: 'Miembros',
      },
      empty: 'Aún no hay roles.',
      total: '{total} roles',
      select_user: 'Seleccione un usuario',
      no_members: 'Aún no hay miembros.',
      saved: 'Permisos guardados.',
      built_in_badge: 'Integrado',
    },
  },
}
