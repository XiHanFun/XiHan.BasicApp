// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: '用户',
      description: '当前主体可见的用户。',
      fields: {
        user_name: '用户名',
        display_name: '显示名',
        email: '邮箱',
        status: '状态',
        created_at: '创建时间',
      },
      actions: {
        create: '创建用户',
      },
      empty: '还没有用户。',
      total: '共 {total} 个用户',
    },
    login: {
      title: '登录',
      description: '用用户名与密码登录。',
      fields: {
        tenant: '租户',
        user_name: '用户名',
        password: '密码',
      },
      tenant_placeholder: '平台账号留空',
      actions: {
        sign_in: '登录',
      },
    },
    profile: {
      change_password: {
        title: '修改密码',
        description: '输入当前密码与新密码（至少 8 位，含字母与数字）；本人的其他会话将被注销。',
        fields: {
          current_password: '当前密码',
          new_password: '新密码',
          confirm_password: '确认新密码',
        },
        mismatch: '两次输入的新密码不一致。',
        success: '密码已修改，其他会话已注销。',
        actions: {
          submit: '修改密码',
        },
      },
    },
  },
}
