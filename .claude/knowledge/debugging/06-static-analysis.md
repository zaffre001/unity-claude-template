# 6. 정적 분석·경고를 최대한 살려라

> Optimisation implies a lot of code flow analysis, which ends up in rearranging code statements. It means that once optimised, the code could be different from what was originally written, making debugging virtually impossible. Therefore, optimisation flags should be turned on only when the code appears to be reasonably bug free.
> — P. Adragna, §2.1

## Do
- 컴파일러·IDE 경고는 최대 수준 유지. Unity 콘솔의 `CS`·`UNT` 경고를 "노이즈" 로 무시하지 않는다.
- LSP (`hover` / `goToDefinition` / `findReferences`) 는 경고 이전의 사전 방어선 — 타입·nullable·셰도잉이 의심되면 즉시 확인.
- 디버깅 중에는 **Debug 빌드 + Mono + Editor 스크립트 어셈블리** 에서 재현. 최적화 빌드는 버그 없음을 확인한 뒤.

## Don't
- 경고를 `#pragma warning disable` 로 덮지 않는다 — 원인 파악 뒤 의도적 억제만 허용, 이유를 주석 한 줄로 남긴다.
- 최적화 빌드에서만 나는 버그를 디버그 빌드에서 고치지 않는다. 증상이 안 나오므로 추측 수정이 된다 → Heisenbug 양산.
- IL2CPP 전용 증상을 Mono 에서 재현 시도하지 않는다. 재현 환경을 맞춘다.

## Unity 맥락
- CLAUDE.md §7: 디버깅 완료 후 반드시 Code Optimization 을 Release 로 복구.
- RULE-01 Domain Reload 비활성 환경: static 관련 경고·셰도잉 분석을 더 엄격히 살려야 함.
- Editor.log / Player.log 의 경고 라인은 실제 크래시 직전 단서가 많다 — 무시 금지.
- Rider / Unity IDE / dotnet analyzer 셋 중 하나는 CI 에서 돌려 놓는 게 기본값.
