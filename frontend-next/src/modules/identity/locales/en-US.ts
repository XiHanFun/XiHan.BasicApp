// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: 'Users',
      description: 'Users of the current tenant (N0 sample of the Identity package).',
      fields: {
        user_name: 'User name',
        display_name: 'Display name',
        email: 'Email',
        status: 'Status',
        created_at: 'Created at',
      },
      actions: {
        create: 'Create user',
      },
      empty: 'No users yet.',
      total: '{total} users',
    },
  },
}
