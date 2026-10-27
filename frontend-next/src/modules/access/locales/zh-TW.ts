// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  access: {
    roles: {
      title: '角色',
      description: '目前作用側的角色、權限與成員。',
      fields: {
        code: '角色碼',
        name: '名稱',
        description: '說明',
        status: '狀態',
        built_in: '內建',
        created_at: '建立時間',
      },
      actions: {
        create: '建立角色',
        save_permissions: '儲存授權',
        delete: '刪除角色',
        add_member: '新增成員',
        remove_member: '移除',
        save_data_scope: '儲存資料範圍',
      },
      sections: {
        permissions: '權限',
        members: '成員',
        data_scope: '資料範圍',
      },
      empty: '還沒有角色。',
      total: '共 {total} 個角色',
      select_user: '選擇使用者',
      no_members: '還沒有成員。',
      saved: '授權已儲存。',
      built_in_badge: '內建',
      data_scope: {
        self_only: '只看自己',
        department_only: '本部門',
        department_and_children: '本部門及子部門',
        all: '全部',
        custom: '自訂部門',
      },
      custom_departments: '自訂部門',
      include_children: '含子部門',
      data_scope_saved: '資料範圍已儲存。',
    },
  },
}
