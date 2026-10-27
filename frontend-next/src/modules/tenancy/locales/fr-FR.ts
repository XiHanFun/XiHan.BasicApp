// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  tenancy: {
    tenants: {
      title: 'Locataires',
      description: 'Annuaire des locataires côté plateforme : provisionner, initialiser l\'administrateur, suspendre, réactiver et supprimer.',
      fields: {
        tenant_id: 'Identifiant',
        name: 'Nom',
        description: 'Description',
        status: 'Statut',
        admin: 'Administrateur',
        order: 'Ordre',
        expires_at: 'Expire le',
        created_at: 'Créé le',
        user_count: 'Utilisateurs',
      },
      actions: {
        create: 'Créer un locataire',
        save: 'Enregistrer',
        initialize_admin: 'Initialiser l\'administrateur',
        suspend: 'Suspendre',
        resume: 'Réactiver',
        delete: 'Supprimer le locataire',
      },
      sections: {
        detail: 'Locataire',
        admin: 'Administrateur',
      },
      admin_form: {
        user_name: 'Nom d\'utilisateur',
        password: 'Mot de passe initial',
        display_name: 'Nom affiché',
      },
      status: {
        active: 'Actif',
        suspended: 'Suspendu',
      },
      empty: 'Aucun locataire pour l\'instant.',
      total: '{total} locataires',
      no_admin: 'Aucun administrateur pour l\'instant.',
      saved: 'Locataire enregistré.',
      admin_created: 'Administrateur créé ; le mot de passe doit être changé à la première connexion.',
    },
  },
}
