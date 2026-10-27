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
        save_data_scope: 'Guardar ámbito de datos',
      },
      sections: {
        permissions: 'Permisos',
        members: 'Miembros',
        data_scope: 'Ámbito de datos',
      },
      empty: 'Aún no hay roles.',
      total: '{total} roles',
      select_user: 'Seleccione un usuario',
      no_members: 'Aún no hay miembros.',
      saved: 'Permisos guardados.',
      built_in_badge: 'Integrado',
      data_scope: {
        self_only: 'Solo uno mismo',
        department_only: 'Departamentos propios',
        department_and_children: 'Departamentos propios y subdepartamentos',
        all: 'Todo',
        custom: 'Departamentos personalizados',
      },
      custom_departments: 'Departamentos personalizados',
      include_children: 'Incluir subdepartamentos',
      data_scope_saved: 'Ámbito de datos guardado.',
    },
  },
}
