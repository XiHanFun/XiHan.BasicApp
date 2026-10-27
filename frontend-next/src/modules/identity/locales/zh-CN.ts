// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: '用户',
      description: '当前租户的用户（身份包的 N0 示例）。',
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
  },
}
