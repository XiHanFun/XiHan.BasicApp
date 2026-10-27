// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: 'Utilisateurs',
      description: 'Utilisateurs visibles par le principal courant.',
      fields: {
        user_name: 'Nom d’utilisateur',
        display_name: 'Nom d’affichage',
        email: 'E-mail',
        status: 'Statut',
        created_at: 'Créé le',
      },
      actions: {
        create: 'Créer un utilisateur',
      },
      empty: 'Aucun utilisateur pour l’instant.',
      total: '{total} utilisateurs',
    },
    login: {
      title: 'Connexion',
      description: 'Connectez-vous avec votre nom d\'utilisateur et votre mot de passe.',
      fields: {
        tenant: 'Locataire',
        user_name: 'Nom d\'utilisateur',
        password: 'Mot de passe',
      },
      tenant_placeholder: 'Laissez vide pour un compte de plateforme',
      actions: {
        sign_in: 'Se connecter',
      },
    },
    profile: {
      change_password: {
        title: 'Changer le mot de passe',
        description: 'Saisissez le mot de passe actuel et un nouveau (au moins 8 caractères avec lettres et chiffres). Vos autres sessions seront déconnectées.',
        fields: {
          current_password: 'Mot de passe actuel',
          new_password: 'Nouveau mot de passe',
          confirm_password: 'Confirmer le nouveau mot de passe',
        },
        mismatch: 'Les deux nouveaux mots de passe ne correspondent pas.',
        success: 'Mot de passe changé ; vos autres sessions ont été déconnectées.',
        actions: {
          submit: 'Changer le mot de passe',
        },
      },
    },
  },
}
