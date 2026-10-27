// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: 'ユーザー',
      description: '現在のテナントのユーザー（Identity パッケージの N0 サンプル）。',
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
  },
}
