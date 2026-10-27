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
      },
      actions: {
        create: 'ユーザーを作成',
      },
      empty: 'ユーザーはまだいません。',
      total: '{total} 人のユーザー',
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
