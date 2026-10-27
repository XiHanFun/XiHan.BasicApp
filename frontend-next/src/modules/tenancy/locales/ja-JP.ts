// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  tenancy: {
    tenants: {
      title: 'テナント',
      description: 'プラットフォーム側のテナントディレクトリ：開通、管理者の初期化、停止、再開、削除。',
      fields: {
        tenant_id: 'テナント ID',
        name: '名前',
        description: '説明',
        status: '状態',
        admin: '管理者',
        order: '並び順',
        expires_at: '有効期限',
        created_at: '作成日時',
        user_count: 'ユーザー数',
      },
      actions: {
        create: 'テナントを開通',
        save: '保存',
        initialize_admin: '管理者を初期化',
        suspend: '停止',
        resume: '再開',
        delete: 'テナントを削除',
      },
      sections: {
        detail: 'テナント',
        admin: '管理者',
      },
      admin_form: {
        user_name: 'ユーザー名',
        password: '初期パスワード',
        display_name: '表示名',
      },
      status: {
        active: '有効',
        suspended: '停止中',
      },
      empty: 'テナントはまだありません。',
      total: '{total} 件のテナント',
      no_admin: '管理者はまだいません。',
      saved: 'テナントを保存しました。',
      admin_created: '管理者を作成しました。初回ログイン時にパスワードの変更が必要です。',
    },
  },
}
