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
      token: 'Токен',
      token_placeholder: 'Вставьте токен из pnpm dev:token',
      apply: 'Применить',
      signed_in: 'Вы вошли как {subject} · {tenant}',
    },
  },
}
