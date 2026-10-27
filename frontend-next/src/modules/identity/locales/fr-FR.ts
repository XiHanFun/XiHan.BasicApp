// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: 'Utilisateurs',
      description: 'Utilisateurs du locataire courant (exemple N0 du paquet Identity).',
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
  },
}
