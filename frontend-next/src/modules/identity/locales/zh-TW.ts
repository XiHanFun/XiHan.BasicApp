// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: '使用者',
      description: '目前租戶的使用者（身分包的 N0 範例）。',
      fields: {
        user_name: '使用者名稱',
        display_name: '顯示名稱',
        email: '電子郵件',
        status: '狀態',
        created_at: '建立時間',
      },
      actions: {
        create: '建立使用者',
      },
      empty: '還沒有使用者。',
      total: '共 {total} 位使用者',
    },
  },
}
