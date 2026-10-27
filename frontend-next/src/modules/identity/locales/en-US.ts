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
        phone: 'Phone',
        last_login_at: 'Last sign-in',
      },
      actions: {
        create: 'Create user',
        lock: 'Lock',
        unlock: 'Unlock',
        reset_password: 'Reset password',
        delete: 'Delete user',
        save_roles: 'Save roles',
        add_department: 'Add to department',
        remove_department: 'Remove',
      },
      empty: 'No users yet.',
      total: '{total} users',
      sections: {
        account: 'Account',
        roles: 'Roles',
        departments: 'Departments',
      },
      reset_form: {
        new_password: 'New password',
        submit: 'Reset',
      },
      member_form: {
        department: 'Department',
        position: 'Position',
      },
      status: {
        active: 'Active',
        disabled: 'Disabled',
        locked: 'Locked',
      },
      roles_saved: 'Roles saved.',
      password_reset: 'Password reset; the user must change it at next sign-in.',
      department_added: 'Added to the department.',
      locked: 'User locked.',
      no_roles: 'No roles available.',
      no_departments: 'Not in any department.',
      primary_badge: 'Primary',
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
