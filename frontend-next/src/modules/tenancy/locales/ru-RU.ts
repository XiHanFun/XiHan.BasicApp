// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  tenancy: {
    tenants: {
      title: 'Арендаторы',
      description: 'Каталог арендаторов на стороне платформы: создание, инициализация администратора, приостановка, возобновление и удаление.',
      fields: {
        tenant_id: 'Идентификатор',
        name: 'Имя',
        description: 'Описание',
        status: 'Статус',
        admin: 'Администратор',
        order: 'Порядок',
        expires_at: 'Истекает',
        created_at: 'Создан',
        user_count: 'Пользователи',
      },
      actions: {
        create: 'Создать арендатора',
        save: 'Сохранить',
        initialize_admin: 'Инициализировать администратора',
        suspend: 'Приостановить',
        resume: 'Возобновить',
        delete: 'Удалить арендатора',
      },
      sections: {
        detail: 'Арендатор',
        admin: 'Администратор',
      },
      admin_form: {
        user_name: 'Имя пользователя',
        password: 'Начальный пароль',
        display_name: 'Отображаемое имя',
      },
      status: {
        active: 'Активен',
        suspended: 'Приостановлен',
      },
      empty: 'Арендаторов пока нет.',
      total: 'Арендаторов: {total}',
      no_admin: 'Администратора пока нет.',
      saved: 'Арендатор сохранён.',
      admin_created: 'Администратор создан; пароль нужно сменить при первом входе.',
    },
  },
}
