// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  access: {
    roles: {
      title: 'Rôles',
      description: 'Rôles du côté courant, leurs permissions et leurs membres.',
      fields: {
        code: 'Code',
        name: 'Nom',
        description: 'Description',
        status: 'État',
        built_in: 'Intégré',
        created_at: 'Créé le',
      },
      actions: {
        create: 'Créer un rôle',
        save_permissions: 'Enregistrer les permissions',
        delete: 'Supprimer le rôle',
        add_member: 'Ajouter un membre',
        remove_member: 'Retirer',
        save_data_scope: 'Enregistrer la portée',
      },
      sections: {
        permissions: 'Permissions',
        members: 'Membres',
        data_scope: 'Portée des données',
      },
      empty: 'Aucun rôle pour le moment.',
      total: '{total} rôles',
      select_user: 'Choisir un utilisateur',
      no_members: 'Aucun membre pour le moment.',
      saved: 'Permissions enregistrées.',
      built_in_badge: 'Intégré',
      data_scope: {
        self_only: 'Soi-même uniquement',
        department_only: 'Services propres',
        department_and_children: 'Services propres et sous-services',
        all: 'Tout',
        custom: 'Services personnalisés',
      },
      custom_departments: 'Services personnalisés',
      include_children: 'Inclure les sous-services',
      data_scope_saved: 'Portée des données enregistrée.',
    },
  },
}
