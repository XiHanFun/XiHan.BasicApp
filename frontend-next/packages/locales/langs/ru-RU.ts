// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  common: {
    app_name: 'XiHan.BasicApp',
    version: 'v{version}',
    locale: 'Язык',
    actions: {
      refresh: 'Обновить',
      previous: 'Назад',
      next: 'Далее',
    },
    session: {
      label: 'Сеанс',
      signed_in: '{name} · {tenant}',
      platform: 'Платформа',
      change_password: 'Сменить пароль',
      sign_out: 'Выйти',
      must_change_password: 'Администратор сбросил ваш пароль; смените его, прежде чем продолжить.',
    },
  },
}
