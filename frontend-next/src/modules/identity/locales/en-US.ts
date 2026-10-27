// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: 'Users',
      description: 'Users visible to the current principal.',
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
    login: {
      title: 'Sign in',
      description: 'Sign in with your user name and password.',
      fields: {
        tenant: 'Tenant',
        user_name: 'User name',
        password: 'Password',
      },
      tenant_placeholder: 'Leave empty for a platform account',
      actions: {
        sign_in: 'Sign in',
      },
    },
    profile: {
      change_password: {
        title: 'Change password',
        description: 'Enter the current password and a new one (at least 8 characters with letters and digits). Your other sessions will be signed out.',
        fields: {
          current_password: 'Current password',
          new_password: 'New password',
          confirm_password: 'Confirm new password',
        },
        mismatch: 'The two new passwords do not match.',
        success: 'Password changed; your other sessions were signed out.',
        actions: {
          submit: 'Change password',
        },
      },
    },
  },
}
