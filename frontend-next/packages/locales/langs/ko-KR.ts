// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  common: {
    app_name: 'XiHan.BasicApp',
    version: 'v{version}',
    locale: '언어',
    actions: {
      refresh: '새로 고침',
      previous: '이전',
      next: '다음',
      home: '홈',
    },
    session: {
      label: '세션',
      signed_in: '{name} · {tenant}',
      platform: '플랫폼',
      change_password: '비밀번호 변경',
      sign_out: '로그아웃',
      must_change_password: '관리자가 비밀번호를 재설정했습니다. 계속하기 전에 변경하세요.',
    },
    nav: {
      label: '탐색',
    },
    status: {
      forbidden: '접근 거부',
      forbidden_hint: '이 페이지에 대한 권한이 없습니다.',
      not_found: '페이지를 찾을 수 없습니다',
      not_found_hint: '이 주소에 해당하는 페이지가 없습니다.',
      home: '접근 가능한 페이지가 없습니다',
      home_hint: '관리자에게 역할 배정을 요청하세요.',
    },
  },
}
