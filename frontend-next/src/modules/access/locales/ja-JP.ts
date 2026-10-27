// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  access: {
    roles: {
      title: 'ロール',
      description: '現在の側のロール、権限、メンバー。',
      fields: {
        code: 'コード',
        name: '名前',
        description: '説明',
        status: '状態',
        built_in: '組み込み',
        created_at: '作成日時',
      },
      actions: {
        create: 'ロールを作成',
        save_permissions: '権限を保存',
        delete: 'ロールを削除',
        add_member: 'メンバーを追加',
        remove_member: '削除',
      },
      sections: {
        permissions: '権限',
        members: 'メンバー',
      },
      empty: 'ロールはまだありません。',
      total: '{total} 件のロール',
      select_user: 'ユーザーを選択',
      no_members: 'メンバーはまだいません。',
      saved: '権限を保存しました。',
      built_in_badge: '組み込み',
    },
  },
}
