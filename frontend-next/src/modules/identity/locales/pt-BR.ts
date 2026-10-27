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
      },
      actions: {
        create: 'Criar usuário',
      },
      empty: 'Ainda não há usuários.',
      total: '{total} usuários',
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
