// 文案文件：键集须与 en-US.ts 完全对称（scripts/check-i18n.mjs 守护）。
export default {
  tenancy: {
    tenants: {
      title: '테넌트',
      description: '플랫폼 측 테넌트 디렉터리: 개통, 관리자 초기화, 중지, 재개, 삭제.',
      fields: {
        tenant_id: '테넌트 ID',
        name: '이름',
        description: '설명',
        status: '상태',
        admin: '관리자',
        order: '순서',
        expires_at: '만료 시각',
        created_at: '생성 시각',
        user_count: '사용자 수',
      },
      actions: {
        create: '테넌트 개통',
        save: '저장',
        initialize_admin: '관리자 초기화',
        suspend: '중지',
        resume: '재개',
        delete: '테넌트 삭제',
      },
      sections: {
        detail: '테넌트',
        admin: '관리자',
      },
      admin_form: {
        user_name: '사용자 이름',
        password: '초기 비밀번호',
        display_name: '표시 이름',
      },
      status: {
        active: '활성',
        suspended: '중지됨',
      },
      empty: '아직 테넌트가 없습니다.',
      total: '테넌트 {total}개',
      no_admin: '아직 관리자가 없습니다.',
      saved: '테넌트를 저장했습니다.',
      admin_created: '관리자를 생성했습니다. 첫 로그인 시 비밀번호를 변경해야 합니다.',
    },
  },
}
