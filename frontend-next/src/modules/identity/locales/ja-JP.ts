// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: 'ユーザー',
      description: '現在の主体が参照できるユーザー。',
      fields: {
        user_name: 'ユーザー名',
        display_name: '表示名',
        email: 'メール',
        status: '状態',
        created_at: '作成日時',
        phone: '電話',
        last_login_at: '最終ログイン',
      },
      actions: {
        create: 'ユーザーを作成',
        lock: 'ロック',
        unlock: 'ロック解除',
        reset_password: 'パスワードをリセット',
        delete: 'ユーザーを削除',
        save_roles: 'ロールを保存',
        add_department: '部門に追加',
        remove_department: '外す',
      },
      empty: 'ユーザーはまだいません。',
      total: '{total} 人のユーザー',
      sections: {
        account: 'アカウント',
        roles: 'ロール',
        departments: '部門',
      },
      reset_form: {
        new_password: '新しいパスワード',
        submit: 'リセット',
      },
      member_form: {
        department: '部門',
        position: '役職',
      },
      status: {
        active: '有効',
        disabled: '無効',
        locked: 'ロック中',
      },
      roles_saved: 'ロールを保存しました。',
      password_reset: 'パスワードをリセットしました。次回ログイン時に変更が必要です。',
      department_added: '部門に追加しました。',
      locked: 'ユーザーをロックしました。',
      no_roles: '利用できるロールがありません。',
      no_departments: 'どの部門にも所属していません。',
      primary_badge: '主',
    },
    login: {
      title: 'ログイン',
      description: 'ユーザー名とパスワードでログインします。',
      fields: {
        tenant: 'テナント',
        user_name: 'ユーザー名',
        password: 'パスワード',
      },
      tenant_placeholder: 'プラットフォームアカウントは空のまま',
      actions: {
        sign_in: 'ログイン',
      },
    },
    profile: {
      change_password: {
        title: 'パスワード変更',
        description: '現在のパスワードと新しいパスワード（8 文字以上、英字と数字を含む）を入力します。他のセッションはログアウトされます。',
        fields: {
          current_password: '現在のパスワード',
          new_password: '新しいパスワード',
          confirm_password: '新しいパスワード（確認）',
        },
        mismatch: '新しいパスワードが一致しません。',
        success: 'パスワードを変更しました。他のセッションはログアウトされました。',
        actions: {
          submit: 'パスワードを変更',
        },
      },
    },
  },
}
