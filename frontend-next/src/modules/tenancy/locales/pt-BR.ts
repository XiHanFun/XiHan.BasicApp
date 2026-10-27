// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  tenancy: {
    tenants: {
      title: 'Locatários',
      description: 'Diretório de locatários do lado da plataforma: provisionar, inicializar o administrador, suspender, retomar e excluir.',
      fields: {
        tenant_id: 'ID do locatário',
        name: 'Nome',
        description: 'Descrição',
        status: 'Status',
        admin: 'Administrador',
        order: 'Ordem',
        expires_at: 'Expira em',
        created_at: 'Criado em',
        user_count: 'Usuários',
      },
      actions: {
        create: 'Criar locatário',
        save: 'Salvar',
        initialize_admin: 'Inicializar administrador',
        suspend: 'Suspender',
        resume: 'Retomar',
        delete: 'Excluir locatário',
      },
      sections: {
        detail: 'Locatário',
        admin: 'Administrador',
      },
      admin_form: {
        user_name: 'Nome de usuário',
        password: 'Senha inicial',
        display_name: 'Nome de exibição',
      },
      status: {
        active: 'Ativo',
        suspended: 'Suspenso',
      },
      empty: 'Ainda não há locatários.',
      total: '{total} locatários',
      no_admin: 'Ainda não há administrador.',
      saved: 'Locatário salvo.',
      admin_created: 'Administrador criado; a senha deve ser alterada no primeiro login.',
    },
  },
}
