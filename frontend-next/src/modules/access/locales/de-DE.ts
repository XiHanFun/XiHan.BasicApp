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
        save_data_scope: 'Datenbereich speichern',
      },
      sections: {
        permissions: 'Berechtigungen',
        members: 'Mitglieder',
        data_scope: 'Datenbereich',
      },
      empty: 'Noch keine Rollen.',
      total: '{total} Rollen',
      select_user: 'Benutzer wählen',
      no_members: 'Noch keine Mitglieder.',
      saved: 'Berechtigungen gespeichert.',
      built_in_badge: 'Eingebaut',
      data_scope: {
        self_only: 'Nur selbst',
        department_only: 'Eigene Abteilungen',
        department_and_children: 'Eigene Abteilungen und Unterabteilungen',
        all: 'Alle',
        custom: 'Eigene Auswahl',
      },
      custom_departments: 'Ausgewählte Abteilungen',
      include_children: 'Mit Unterabteilungen',
      data_scope_saved: 'Datenbereich gespeichert.',
    },
  },
}
