// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: 'Usuários',
      description: 'Usuários visíveis para o principal atual.',
      fields: {
        user_name: 'Nome de usuário',
        display_name: 'Nome de exibição',
        email: 'E-mail',
        status: 'Status',
        created_at: 'Criado em',
        phone: 'Telefone',
        last_login_at: 'Último login',
      },
      actions: {
        create: 'Criar usuário',
        lock: 'Bloquear',
        unlock: 'Desbloquear',
        reset_password: 'Redefinir senha',
        delete: 'Excluir usuário',
        save_roles: 'Salvar papéis',
        add_department: 'Adicionar ao departamento',
        remove_department: 'Remover',
      },
      empty: 'Ainda não há usuários.',
      total: '{total} usuários',
      sections: {
        account: 'Conta',
        roles: 'Papéis',
        departments: 'Departamentos',
      },
      reset_form: {
        new_password: 'Nova senha',
        submit: 'Redefinir',
      },
      member_form: {
        department: 'Departamento',
        position: 'Cargo',
      },
      status: {
        active: 'Ativo',
        disabled: 'Desabilitado',
        locked: 'Bloqueado',
      },
      roles_saved: 'Papéis salvos.',
      password_reset: 'Senha redefinida; o usuário deve alterá-la no próximo login.',
      department_added: 'Adicionado ao departamento.',
      locked: 'Usuário bloqueado.',
      no_roles: 'Nenhum papel disponível.',
      no_departments: 'Em nenhum departamento.',
      primary_badge: 'Principal',
    },
    login: {
      title: 'Entrar',
      description: 'Entre com seu nome de usuário e senha.',
      fields: {
        tenant: 'Locatário',
        user_name: 'Nome de usuário',
        password: 'Senha',
      },
      tenant_placeholder: 'Deixe vazio para uma conta de plataforma',
      actions: {
        sign_in: 'Entrar',
      },
    },
    profile: {
      change_password: {
        title: 'Alterar senha',
        description: 'Informe a senha atual e uma nova (pelo menos 8 caracteres com letras e dígitos). Suas outras sessões serão encerradas.',
        fields: {
          current_password: 'Senha atual',
          new_password: 'Nova senha',
          confirm_password: 'Confirmar nova senha',
        },
        mismatch: 'As duas novas senhas não coincidem.',
        success: 'Senha alterada; suas outras sessões foram encerradas.',
        actions: {
          submit: 'Alterar senha',
        },
      },
    },
  },
}
