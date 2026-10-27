// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  access: {
    roles: {
      title: 'Papéis',
      description: 'Papéis do lado atual, suas permissões e membros.',
      fields: {
        code: 'Código',
        name: 'Nome',
        description: 'Descrição',
        status: 'Status',
        built_in: 'Interno',
        created_at: 'Criado em',
      },
      actions: {
        create: 'Criar papel',
        save_permissions: 'Salvar permissões',
        delete: 'Excluir papel',
        add_member: 'Adicionar membro',
        remove_member: 'Remover',
      },
      sections: {
        permissions: 'Permissões',
        members: 'Membros',
      },
      empty: 'Ainda não há papéis.',
      total: '{total} papéis',
      select_user: 'Selecione um usuário',
      no_members: 'Ainda não há membros.',
      saved: 'Permissões salvas.',
      built_in_badge: 'Interno',
    },
  },
}
