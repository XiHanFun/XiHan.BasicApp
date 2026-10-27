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
      },
      sections: {
        permissions: 'Permissions',
        members: 'Membres',
      },
      empty: 'Aucun rôle pour le moment.',
      total: '{total} rôles',
      select_user: 'Choisir un utilisateur',
      no_members: 'Aucun membre pour le moment.',
      saved: 'Permissions enregistrées.',
      built_in_badge: 'Intégré',
    },
  },
}
