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
        phone: 'Téléphone',
        last_login_at: 'Dernière connexion',
      },
      actions: {
        create: 'Créer un utilisateur',
        lock: 'Verrouiller',
        unlock: 'Déverrouiller',
        reset_password: 'Réinitialiser le mot de passe',
        delete: 'Supprimer l\'utilisateur',
        save_roles: 'Enregistrer les rôles',
        add_department: 'Ajouter au service',
        remove_department: 'Retirer',
      },
      empty: 'Aucun utilisateur pour l’instant.',
      total: '{total} utilisateurs',
      sections: {
        account: 'Compte',
        roles: 'Rôles',
        departments: 'Services',
      },
      reset_form: {
        new_password: 'Nouveau mot de passe',
        submit: 'Réinitialiser',
      },
      member_form: {
        department: 'Service',
        position: 'Poste',
      },
      status: {
        active: 'Actif',
        disabled: 'Désactivé',
        locked: 'Verrouillé',
      },
      roles_saved: 'Rôles enregistrés.',
      password_reset: 'Mot de passe réinitialisé ; l\'utilisateur devra le changer à la prochaine connexion.',
      department_added: 'Ajouté au service.',
      locked: 'Utilisateur verrouillé.',
      no_roles: 'Aucun rôle disponible.',
      no_departments: 'Dans aucun service.',
      primary_badge: 'Principal',
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
      subtitle: 'Saisissez votre identifiant et votre mot de passe pour vous connecter',
      user_name_placeholder: 'Identifiant ou e-mail',
      password_placeholder: 'Mot de passe',
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
