// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: 'Пользователи',
      description: 'Пользователи текущего арендатора (пример N0 пакета Identity).',
      fields: {
        user_name: 'Имя пользователя',
        display_name: 'Отображаемое имя',
        email: 'Эл. почта',
        status: 'Статус',
        created_at: 'Создан',
      },
      actions: {
        create: 'Создать пользователя',
      },
      empty: 'Пользователей пока нет.',
      total: 'Пользователей: {total}',
    },
  },
}
