// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  identity: {
    users: {
      title: 'Benutzer',
      description: 'Für das aktuelle Prinzipal sichtbare Benutzer.',
      fields: {
        user_name: 'Benutzername',
        display_name: 'Anzeigename',
        email: 'E-Mail',
        status: 'Status',
        created_at: 'Erstellt am',
        phone: 'Telefon',
        last_login_at: 'Letzte Anmeldung',
      },
      actions: {
        create: 'Benutzer anlegen',
        lock: 'Sperren',
        unlock: 'Entsperren',
        reset_password: 'Passwort zurücksetzen',
        delete: 'Benutzer löschen',
        save_roles: 'Rollen speichern',
        add_department: 'Zur Abteilung hinzufügen',
        remove_department: 'Entfernen',
      },
      empty: 'Noch keine Benutzer.',
      total: '{total} Benutzer',
      sections: {
        account: 'Konto',
        roles: 'Rollen',
        departments: 'Abteilungen',
      },
      reset_form: {
        new_password: 'Neues Passwort',
        submit: 'Zurücksetzen',
      },
      member_form: {
        department: 'Abteilung',
        position: 'Stelle',
      },
      status: {
        active: 'Aktiv',
        disabled: 'Deaktiviert',
        locked: 'Gesperrt',
      },
      roles_saved: 'Rollen gespeichert.',
      password_reset: 'Passwort zurückgesetzt; der Benutzer muss es bei der nächsten Anmeldung ändern.',
      department_added: 'Zur Abteilung hinzugefügt.',
      locked: 'Benutzer gesperrt.',
      no_roles: 'Keine Rollen verfügbar.',
      no_departments: 'In keiner Abteilung.',
      primary_badge: 'Haupt',
    },
    login: {
      title: 'Anmelden',
      description: 'Melden Sie sich mit Benutzername und Passwort an.',
      fields: {
        tenant: 'Mandant',
        user_name: 'Benutzername',
        password: 'Passwort',
      },
      tenant_placeholder: 'Für ein Plattformkonto leer lassen',
      actions: {
        sign_in: 'Anmelden',
      },
    },
    profile: {
      change_password: {
        title: 'Passwort ändern',
        description: 'Geben Sie das aktuelle und ein neues Passwort ein (mindestens 8 Zeichen mit Buchstaben und Ziffern). Ihre übrigen Sitzungen werden abgemeldet.',
        fields: {
          current_password: 'Aktuelles Passwort',
          new_password: 'Neues Passwort',
          confirm_password: 'Neues Passwort bestätigen',
        },
        mismatch: 'Die beiden neuen Passwörter stimmen nicht überein.',
        success: 'Passwort geändert; Ihre übrigen Sitzungen wurden abgemeldet.',
        actions: {
          submit: 'Passwort ändern',
        },
      },
    },
  },
}
