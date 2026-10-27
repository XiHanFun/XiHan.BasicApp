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
      },
      actions: {
        create: 'Создать пользователя',
      },
      empty: 'Пользователей пока нет.',
      total: 'Пользователей: {total}',
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
