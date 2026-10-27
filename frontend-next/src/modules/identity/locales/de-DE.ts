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
      },
      actions: {
        create: 'Benutzer anlegen',
      },
      empty: 'Noch keine Benutzer.',
      total: '{total} Benutzer',
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
