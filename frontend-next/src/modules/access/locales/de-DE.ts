// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  access: {
    roles: {
      title: 'Rollen',
      description: 'Rollen der aktuellen Seite, ihre Berechtigungen und Mitglieder.',
      fields: {
        code: 'Code',
        name: 'Name',
        description: 'Beschreibung',
        status: 'Status',
        built_in: 'Eingebaut',
        created_at: 'Erstellt am',
      },
      actions: {
        create: 'Rolle anlegen',
        save_permissions: 'Berechtigungen speichern',
        delete: 'Rolle löschen',
        add_member: 'Mitglied hinzufügen',
        remove_member: 'Entfernen',
      },
      sections: {
        permissions: 'Berechtigungen',
        members: 'Mitglieder',
      },
      empty: 'Noch keine Rollen.',
      total: '{total} Rollen',
      select_user: 'Benutzer wählen',
      no_members: 'Noch keine Mitglieder.',
      saved: 'Berechtigungen gespeichert.',
      built_in_badge: 'Eingebaut',
    },
  },
}
