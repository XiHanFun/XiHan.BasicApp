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
        phone: '手機',
        last_login_at: '最近登入',
      },
      actions: {
        create: '建立使用者',
        lock: '鎖定',
        unlock: '解鎖',
        reset_password: '重設密碼',
        delete: '刪除使用者',
        save_roles: '儲存角色',
        add_department: '加入部門',
        remove_department: '移出',
      },
      empty: '還沒有使用者。',
      total: '共 {total} 位使用者',
      sections: {
        account: '帳號',
        roles: '角色',
        departments: '部門',
      },
      reset_form: {
        new_password: '新密碼',
        submit: '重設',
      },
      member_form: {
        department: '部門',
        position: '崗位',
      },
      status: {
        active: '啟用',
        disabled: '停用',
        locked: '鎖定',
      },
      roles_saved: '角色已儲存。',
      password_reset: '密碼已重設，使用者下次登入須變更。',
      department_added: '已加入部門。',
      locked: '使用者已鎖定。',
      no_roles: '沒有可用的角色。',
      no_departments: '不在任何部門。',
      primary_badge: '主',
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
      subtitle: '請輸入您的帳號密碼以登入系統',
      user_name_placeholder: '請輸入使用者名稱或電子郵件',
      password_placeholder: '請輸入密碼',
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
