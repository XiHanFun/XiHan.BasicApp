// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  access: {
    roles: {
      title: '角色',
      description: '当前作用侧的角色、权限与成员。',
      fields: {
        code: '角色码',
        name: '名称',
        description: '说明',
        status: '状态',
        built_in: '内建',
        created_at: '创建时间',
      },
      actions: {
        create: '创建角色',
        save_permissions: '保存授权',
        delete: '删除角色',
        add_member: '添加成员',
        remove_member: '移除',
      },
      sections: {
        permissions: '权限',
        members: '成员',
      },
      empty: '还没有角色。',
      total: '共 {total} 个角色',
      select_user: '选择用户',
      no_members: '还没有成员。',
      saved: '授权已保存。',
      built_in_badge: '内建',
    },
  },
}
