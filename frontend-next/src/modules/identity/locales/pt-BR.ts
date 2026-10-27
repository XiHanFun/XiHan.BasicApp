// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: 'Usuários',
      description: 'Usuários do locatário atual (amostra N0 do pacote Identity).',
      fields: {
        user_name: 'Nome de usuário',
        display_name: 'Nome de exibição',
        email: 'E-mail',
        status: 'Status',
        created_at: 'Criado em',
      },
      actions: {
        create: 'Criar usuário',
      },
      empty: 'Ainda não há usuários.',
      total: '{total} usuários',
    },
  },
}
