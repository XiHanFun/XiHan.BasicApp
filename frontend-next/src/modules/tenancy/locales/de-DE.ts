// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  tenancy: {
    tenants: {
      title: 'Mandanten',
      description: 'Mandantenverzeichnis der Plattformseite: einrichten, Administrator initialisieren, aussetzen, fortsetzen und löschen.',
      fields: {
        tenant_id: 'Mandanten-ID',
        name: 'Name',
        description: 'Beschreibung',
        status: 'Status',
        admin: 'Administrator',
        order: 'Reihenfolge',
        expires_at: 'Läuft ab am',
        created_at: 'Erstellt am',
        user_count: 'Benutzer',
      },
      actions: {
        create: 'Mandant einrichten',
        save: 'Speichern',
        initialize_admin: 'Administrator initialisieren',
        suspend: 'Aussetzen',
        resume: 'Fortsetzen',
        delete: 'Mandant löschen',
      },
      sections: {
        detail: 'Mandant',
        admin: 'Administrator',
      },
      admin_form: {
        user_name: 'Benutzername',
        password: 'Initiales Passwort',
        display_name: 'Anzeigename',
      },
      status: {
        active: 'Aktiv',
        suspended: 'Ausgesetzt',
      },
      empty: 'Noch keine Mandanten.',
      total: '{total} Mandanten',
      no_admin: 'Noch kein Administrator.',
      saved: 'Mandant gespeichert.',
      admin_created: 'Administrator angelegt; das Passwort muss bei der ersten Anmeldung geändert werden.',
    },
  },
}
