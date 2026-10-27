// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  access: {
    roles: {
      title: 'Roles',
      description: 'Roles of the current side, their permissions and members.',
      fields: {
        code: 'Code',
        name: 'Name',
        description: 'Description',
        status: 'Status',
        built_in: 'Built-in',
        created_at: 'Created at',
      },
      actions: {
        create: 'Create role',
        save_permissions: 'Save permissions',
        delete: 'Delete role',
        add_member: 'Add member',
        remove_member: 'Remove',
      },
      sections: {
        permissions: 'Permissions',
        members: 'Members',
      },
      empty: 'No roles yet.',
      total: '{total} roles',
      select_user: 'Select a user',
      no_members: 'No members yet.',
      saved: 'Permissions saved.',
      built_in_badge: 'Built-in',
    },
  },
}
