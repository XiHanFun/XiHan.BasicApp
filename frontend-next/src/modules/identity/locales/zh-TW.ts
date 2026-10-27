// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: '使用者',
      description: '目前主體可見的使用者。',
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
    login: {
      title: '登入',
      description: '以使用者名稱與密碼登入。',
      fields: {
        tenant: '租戶',
        user_name: '使用者名稱',
        password: '密碼',
      },
      tenant_placeholder: '平台帳號留空',
      actions: {
        sign_in: '登入',
      },
    },
    profile: {
      change_password: {
        title: '變更密碼',
        description: '輸入目前密碼與新密碼（至少 8 位，含字母與數字）；本人的其他工作階段將被登出。',
        fields: {
          current_password: '目前密碼',
          new_password: '新密碼',
          confirm_password: '確認新密碼',
        },
        mismatch: '兩次輸入的新密碼不一致。',
        success: '密碼已變更，其他工作階段已登出。',
        actions: {
          submit: '變更密碼',
        },
      },
    },
  },
}
