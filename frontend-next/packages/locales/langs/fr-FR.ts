// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  common: {
    app_name: 'XiHan.BasicApp',
    version: 'v{version}',
    locale: 'Langue',
    actions: {
      refresh: 'Actualiser',
      previous: 'Précédent',
      next: 'Suivant',
      home: 'Accueil',
    },
    session: {
      label: 'Session',
      signed_in: '{name} · {tenant}',
      platform: 'Plateforme',
      change_password: 'Changer le mot de passe',
      sign_out: 'Se déconnecter',
      must_change_password: 'Votre mot de passe a été réinitialisé par un administrateur ; changez-le avant de continuer.',
    },
    nav: {
      label: 'Navigation',
    },
    status: {
      forbidden: 'Accès refusé',
      forbidden_hint: 'Vous n\'avez pas la permission d\'accéder à cette page.',
      not_found: 'Page introuvable',
      not_found_hint: 'L\'adresse ne correspond à aucune page.',
      home: 'Aucune page disponible',
      home_hint: 'Demandez à un administrateur de vous attribuer un rôle.',
    },
  },
}
