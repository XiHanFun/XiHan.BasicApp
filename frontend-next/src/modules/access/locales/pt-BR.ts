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
        save_data_scope: 'Salvar escopo de dados',
      },
      sections: {
        permissions: 'Permissões',
        members: 'Membros',
        data_scope: 'Escopo de dados',
      },
      empty: 'Ainda não há papéis.',
      total: '{total} papéis',
      select_user: 'Selecione um usuário',
      no_members: 'Ainda não há membros.',
      saved: 'Permissões salvas.',
      built_in_badge: 'Interno',
      data_scope: {
        self_only: 'Somente si mesmo',
        department_only: 'Departamentos próprios',
        department_and_children: 'Departamentos próprios e subdepartamentos',
        all: 'Tudo',
        custom: 'Departamentos personalizados',
      },
      custom_departments: 'Departamentos personalizados',
      include_children: 'Incluir subdepartamentos',
      data_scope_saved: 'Escopo de dados salvo.',
    },
  },
}
