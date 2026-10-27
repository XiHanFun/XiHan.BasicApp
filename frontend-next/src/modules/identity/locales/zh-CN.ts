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
        phone: '手机',
        last_login_at: '最近登录',
      },
      actions: {
        create: '创建用户',
        lock: '锁定',
        unlock: '解锁',
        reset_password: '重置密码',
        delete: '删除用户',
        save_roles: '保存角色',
        add_department: '加入部门',
        remove_department: '移出',
      },
      empty: '还没有用户。',
      total: '共 {total} 个用户',
      sections: {
        account: '账号',
        roles: '角色',
        departments: '部门',
      },
      reset_form: {
        new_password: '新密码',
        submit: '重置',
      },
      member_form: {
        department: '部门',
        position: '岗位',
      },
      status: {
        active: '启用',
        disabled: '停用',
        locked: '锁定',
      },
      roles_saved: '角色已保存。',
      password_reset: '密码已重置，用户下次登录须修改。',
      department_added: '已加入部门。',
      locked: '用户已锁定。',
      no_roles: '没有可用的角色。',
      no_departments: '不在任何部门。',
      primary_badge: '主',
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
      subtitle: '请输入您的账号密码以登录系统',
      user_name_placeholder: '请输入用户名或邮箱',
      password_placeholder: '请输入密码',
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
