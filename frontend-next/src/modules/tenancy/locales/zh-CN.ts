// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  tenancy: {
    tenants: {
      title: '租户',
      description: '平台侧的租户目录：开通、初始化管理员、停用、恢复与删除。',
      fields: {
        tenant_id: '租户 ID',
        name: '名称',
        description: '说明',
        status: '状态',
        admin: '管理员',
        order: '排序',
        expires_at: '到期时间',
        created_at: '创建时间',
        user_count: '用户数',
      },
      actions: {
        create: '开通租户',
        save: '保存',
        initialize_admin: '初始化管理员',
        suspend: '停用',
        resume: '恢复',
        delete: '删除租户',
      },
      sections: {
        detail: '租户',
        admin: '管理员',
      },
      admin_form: {
        user_name: '登录名',
        password: '初始密码',
        display_name: '显示名',
      },
      status: {
        active: '启用',
        suspended: '已停用',
      },
      empty: '还没有租户。',
      total: '共 {total} 个租户',
      no_admin: '还没有管理员。',
      saved: '租户已保存。',
      admin_created: '管理员已创建；首次登录须修改密码。',
    },
  },
}
