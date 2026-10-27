// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  tenancy: {
    tenants: {
      title: 'Tenants',
      description: 'Platform-side tenant directory: provision, initialize the administrator, suspend, resume and delete.',
      fields: {
        tenant_id: 'Tenant ID',
        name: 'Name',
        description: 'Description',
        status: 'Status',
        admin: 'Administrator',
        order: 'Order',
        expires_at: 'Expires at',
        created_at: 'Created at',
        user_count: 'Users',
      },
      actions: {
        create: 'Create tenant',
        save: 'Save',
        initialize_admin: 'Initialize administrator',
        suspend: 'Suspend',
        resume: 'Resume',
        delete: 'Delete tenant',
      },
      sections: {
        detail: 'Tenant',
        admin: 'Administrator',
      },
      admin_form: {
        user_name: 'User name',
        password: 'Initial password',
        display_name: 'Display name',
      },
      status: {
        active: 'Active',
        suspended: 'Suspended',
      },
      empty: 'No tenants yet.',
      total: '{total} tenants',
      no_admin: 'No administrator yet.',
      saved: 'Tenant saved.',
      admin_created: 'Administrator created; the password must be changed at first sign-in.',
    },
  },
}
