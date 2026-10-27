// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: 'Пользователи',
      description: 'Пользователи, видимые текущему субъекту.',
      fields: {
        user_name: 'Имя пользователя',
        display_name: 'Отображаемое имя',
        email: 'Эл. почта',
        status: 'Статус',
        created_at: 'Создан',
        phone: 'Телефон',
        last_login_at: 'Последний вход',
      },
      actions: {
        create: 'Создать пользователя',
        lock: 'Заблокировать',
        unlock: 'Разблокировать',
        reset_password: 'Сбросить пароль',
        delete: 'Удалить пользователя',
        save_roles: 'Сохранить роли',
        add_department: 'Добавить в подразделение',
        remove_department: 'Убрать',
      },
      empty: 'Пользователей пока нет.',
      total: 'Пользователей: {total}',
      sections: {
        account: 'Учётная запись',
        roles: 'Роли',
        departments: 'Подразделения',
      },
      reset_form: {
        new_password: 'Новый пароль',
        submit: 'Сбросить',
      },
      member_form: {
        department: 'Подразделение',
        position: 'Должность',
      },
      status: {
        active: 'Активен',
        disabled: 'Отключён',
        locked: 'Заблокирован',
      },
      roles_saved: 'Роли сохранены.',
      password_reset: 'Пароль сброшен; пользователь должен сменить его при следующем входе.',
      department_added: 'Добавлен в подразделение.',
      locked: 'Пользователь заблокирован.',
      no_roles: 'Нет доступных ролей.',
      no_departments: 'Не состоит ни в одном подразделении.',
      primary_badge: 'Основное',
    },
    login: {
      title: 'Вход',
      description: 'Войдите с именем пользователя и паролем.',
      fields: {
        tenant: 'Арендатор',
        user_name: 'Имя пользователя',
        password: 'Пароль',
      },
      tenant_placeholder: 'Для платформенной учётной записи оставьте пустым',
      actions: {
        sign_in: 'Войти',
      },
    },
    profile: {
      change_password: {
        title: 'Смена пароля',
        description: 'Введите текущий и новый пароль (не менее 8 символов, с буквами и цифрами). Остальные ваши сеансы будут завершены.',
        fields: {
          current_password: 'Текущий пароль',
          new_password: 'Новый пароль',
          confirm_password: 'Подтвердите новый пароль',
        },
        mismatch: 'Новые пароли не совпадают.',
        success: 'Пароль изменён; остальные сеансы завершены.',
        actions: {
          submit: 'Сменить пароль',
        },
      },
    },
  },
}
