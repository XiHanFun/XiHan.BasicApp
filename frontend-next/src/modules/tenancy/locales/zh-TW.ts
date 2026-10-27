// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  tenancy: {
    tenants: {
      title: '租戶',
      description: '平台側的租戶目錄：開通、初始化管理員、停用、恢復與刪除。',
      fields: {
        tenant_id: '租戶 ID',
        name: '名稱',
        description: '說明',
        status: '狀態',
        admin: '管理員',
        order: '排序',
        expires_at: '到期時間',
        created_at: '建立時間',
        user_count: '使用者數',
      },
      actions: {
        create: '開通租戶',
        save: '儲存',
        initialize_admin: '初始化管理員',
        suspend: '停用',
        resume: '恢復',
        delete: '刪除租戶',
      },
      sections: {
        detail: '租戶',
        admin: '管理員',
      },
      admin_form: {
        user_name: '登入名',
        password: '初始密碼',
        display_name: '顯示名',
      },
      status: {
        active: '啟用',
        suspended: '已停用',
      },
      empty: '還沒有租戶。',
      total: '共 {total} 個租戶',
      no_admin: '還沒有管理員。',
      saved: '租戶已儲存。',
      admin_created: '管理員已建立；首次登入須變更密碼。',
    },
  },
}
