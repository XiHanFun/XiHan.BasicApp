// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: 'Benutzer',
      description: 'Benutzer des aktuellen Mandanten (N0-Beispiel des Identity-Pakets).',
      fields: {
        user_name: 'Benutzername',
        display_name: 'Anzeigename',
        email: 'E-Mail',
        status: 'Status',
        created_at: 'Erstellt am',
      },
      actions: {
        create: 'Benutzer anlegen',
      },
      empty: 'Noch keine Benutzer.',
      total: '{total} Benutzer',
    },
  },
}
