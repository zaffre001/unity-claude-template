# 10. Localization — 초기부터. 후기 = 디자인 붕괴

> too many companies treat localization as an afterthought, tacking it on to the end of the process when the product is almost ready. This is a mistake. (...) Suddenly, the text doesn't fit in buttons or boxes, and everything looks out of whack. Your whole design might suffer if you're switching to a language that reads right-to-left, such as Arabic, Hebrew or Urdu.
> — Global App Testing, Ch 5

## 인디 톤 — 조건부 적용
이 규칙은 **해외 출시 의향이 있을 때만** 적용한다. 국내(한국어) 전용 릴리즈면 본 섹션 스킵. 단 "나중에 해외 낼지도" 싶으면 초기 3개 항목만이라도 지키는 게 낫다 (비용 차이가 10배 이상).

## Do
- 모든 UI 문자열을 초기부터 **리소스(Localization Package / CSV / JSON)** 로 분리. 하드코딩 금지 — `text.text = "시작";` 같은 코드 리뷰에서 즉시 리젝.
- 개발 초기부터 **"가장 긴 언어 + RTL + 큰 폰트"** 조합 스모크 1회 이상.
- 다음 케이스를 테스트 매트릭스에 고정:
  - **긴 텍스트** (독일어·한국어) 가 버튼·팝업 안에 들어가는가 (clipping·overflow·ellipsis)
  - **RTL** (ar·he) 에서 UI 앵커·아이콘·프로그레스 방향이 올바르게 뒤집히는가
  - **숫자·통화·날짜** 포맷이 locale 따라 바뀌는가 (천 단위 구분자·소수점·통화 기호)
  - **특수문자** (한자·키릴·이모지·ligature) 폰트 fallback 이 안 깨지는가
  - **IME** (ja·ko·zh) 조합 중 입력이 Backspace·Enter 에서 올바르게 동작하는가

## Don't (해외 출시 의향이 있을 때)
- 릴리즈 직전에 번역본 붙여서 UI 깨짐 발견 금지 — 수정 범위가 디자인·코드 전면으로 커진다 (Ch 5 "editing code to make sure everything fits").
- 번역 품질을 기계 번역만으로 끝내지 않는다 — Ch 5 토닉 워터 일화("이탈리아에서 '변기 물' 로 번역") 같은 브랜드 리스크.

## 강제하지 않는 것 (기본 동작 ❌)
- 국내 전용 릴리즈에 i18n 강제 — 시간 낭비. 해외 낼 때 이 문서 돌아오면 됨.
- 모든 언어 UI 전수 QA — 핵심 플로우 (결제·저장·튜토리얼) + 가장 긴 언어 1개만으로도 80% 커버.

## Unity / Agent — 게임 맥락
- **TextMeshPro** 동적 아틀라스 + fallback 체인 (한자·이모지·Noto) 설정부터 검증.
- `ContentSizeFitter` / `HorizontalLayoutGroup` 의 텍스트 길이 변화 대응 검증 — Bridge `Component.SetField` 로 긴 언어 문자열 치환 후 `Component.GetField` 로 `sizeDelta` · `preferredWidth` 확인.
- 숫자 포맷은 `CultureInfo` 강제 (`value.ToString("N0", CultureInfo.CurrentCulture)`) — 하드코딩된 `","` 구분자 금지.
- 키보드 입력 locale: IME on/off · 조합 중 종료 · Backspace 동작 3종은 Editor 재현 어려우니 실 디바이스 필수.

## 게임 특히 중요한 이유
- 모바일 게임 글로벌 출시 시 지역별 리텐션은 번역·현지화 품질에 강하게 연동. UI 깨짐은 별점·리뷰에 직결 (Ch 5 사례 Indonesia "last name" 일화 — 40% 가입 불가).
- 인앱 결제 화면은 특히 — 통화·세제 포맷 틀리면 법적·정책적 이슈까지.
- 문화적 이미지 (카드 슈트·손 제스처·색 의미) 는 지역마다 다르다 — Ch 5 디아퍼 스토크/복숭아 일화.
