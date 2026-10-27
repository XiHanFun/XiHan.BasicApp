// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  access: {
    roles: {
      title: 'Роли',
      description: 'Роли текущей стороны, их разрешения и участники.',
      fields: {
        code: 'Код',
        name: 'Имя',
        description: 'Описание',
        status: 'Состояние',
        built_in: 'Встроенная',
        created_at: 'Создана',
      },
      actions: {
        create: 'Создать роль',
        save_permissions: 'Сохранить разрешения',
        delete: 'Удалить роль',
        add_member: 'Добавить участника',
        remove_member: 'Убрать',
      },
      sections: {
        permissions: 'Разрешения',
        members: 'Участники',
      },
      empty: 'Ролей пока нет.',
      total: 'Ролей: {total}',
      select_user: 'Выберите пользователя',
      no_members: 'Участников пока нет.',
      saved: 'Разрешения сохранены.',
      built_in_badge: 'Встроенная',
    },
  },
}
