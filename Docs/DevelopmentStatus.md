# 마녀의 수확제 — 개발 현황 및 인수인계

**작성 기준: 2026-11-25 / VER 0.45.**

- 기준 소스: [510eff9 — 0.45 타이틀 화면 구현](https://github.com/arena5045/WitchHarvest/commit/510eff92c25e65e56fe0fba6c7dc9af362d4cbb4)
- 이전 기준: [1dd5040 — 작업 0.4 세이브 구현](https://github.com/arena5045/WitchHarvest/commit/1dd50401ec1e8dcf65c73de66833ef2a46575678)
- 0.45 기준 현황은 소스 검토와 사용자 테스트 보고를 바탕으로 작성했다. 이후 직접 실행한 항목은 아래에 검증 환경을 함께 기록한다.

## 1. 환경과 프로젝트 구조

Unity 6000.4.5f1, URP 2D, C#, 새 Input System 1.19.0, uGUI/TMP, DOTween, Odin Inspector. Git LFS로 관리되는 이미지가 있다.

```
Assets/
  01.Scripts/
    매니저/     DayManager, FarmManager, HarvestManager, EconomyManager,
               UpgradeManager, GameUIManager, SaveManager
    노드/       UpgradeNodeData, UpgradeNodeState, UpgradeStats
    세이브/     GameSaveData, MemoirSaveData
    Ui/         TitleUI, UpgradeTreeUI, UpgradeNodeUI
      대화창/   DialogueData, DialogueCharacterData, DialoguePlayer,
               DialoguePortraitSlot
    작물/       CropHealth, CropVisual, 작물SO/CropData
    FarmCell.cs, HarvestAttack.cs, HarvestCursor.cs, HarvestChargeVisual.cs
  02.Scenes/    TitleScene.unity, FarmPrototype.unity
  04.Data/SO/   작물, 강화, 대화, 캐릭터의 ScriptableObject
  07.Ui/        Title/, 폰트/ 등 UI 리소스
ProjectSettings/
Packages/
```

Build Scene List는 TitleScene(0), FarmPrototype(1) 순서. 에디터에서 FarmPrototype을 직접 열어 농장만 테스트할 수 있다.

## 2. 기능별 상태

| 기능 | 상태 |
| --- | --- |
| 기본 마법 커서, 범위 공격, 작물 수확 | 구현 / 사용자 기본 동작 확인 |
| 하루 진행, 결과창, 골드 경제 | 구현 / 사용자 기본 동작 확인 |
| 노드 구매 및 해금·농장 확장·수확 속도 | 구현 / 사용자 기본 동작 확인 |
| 강화 스탯 통합 집계 | 구현 / 사용자 기본 동작 확인 |
| 대화 재생, 초상·반전·타이핑·스킵 | 기본 재생 구현 / 일부 보완 사항 있음 |
| 자동 진행 저장 및 복원 | 구현 / Unity 실행·파일 검증 보류 |
| 대화 발견 즉시 회고록 저장 | 구현 / 실제 대화 이벤트 연동 및 검증 보류 |
| 타이틀 메뉴, 옵션/회고록 빈 패널 | 구현 / Windows 플레이어에서 메뉴 표시와 조작 확인. 이번 씬 전환 검증에서는 옵션 진입·Esc 복귀도 확인 |
| 세르의 별장 맞이 전환 | 구현 / Windows 플레이어에서 타이틀 → 오버레이 → 농장 진입, 입력 잠금, 세 화면 비율 확인 |
| 조건별 대화 이벤트, 회고록 목록·재생 | 미구현 |
| 실제 옵션 기능 | 미구현 |
| 특수 작물, 재성장, 사역마 등 추가 콘텐츠 | 미구현 |
| 완성된 엔딩, 업적, 출시 빌드 | 미구현 |

## 3. 현재 핵심 동작

### 하루·수확·경제

`DayManager`의 상태: Ready → Countdown → Harvesting → Result → Settlement → 다음 Countdown. 첫날은 Ready에서 시작한다. 기본 카운트다운 3초, 수확 10초. 수확 종료에서 보상 지급 플래그를 먼저 잠그고 판매금을 한 번만 더한 뒤 Result 상태 이벤트를 보낸다. Settlement가 밤(로비)이다.

`HarvestAttack`은 `DayManager.IsHarvesting`이 참일 때만 일정 간격으로 범위 안의 CropHealth를 공격한다. `CurrentAttackSpeed = attackSpeed * (1 + attackSpeedBonusPercent / 100)`. `HarvestChargeVisual`은 동일한 AttackProgress를 읽어 충전 연출을 표시한다. 기본 공격 속도 필드는 강화로 덮어쓰지 않는다.

`FarmManager`는 최초 Start에서 1×1 밭과 작물을 준비한다. ExpandTo는 기존 셀과 작물을 유지하면서 필요한 셀만 만들며 축소하지 않는다. 새 칸은 다음 날에 재배한다. FarmCell.ClearCrops / FarmManager.ClearAllCrops는 로드 때 초기 작물을 제거하며 수확으로 처리하지 않는다.

`EconomyManager.Gold`는 long이다. 결과 판매금은 수확된 작물별 수량 × 판매가격. 지급·차감은 경제 매니저에서 수행하고 UI는 읽어서 표시한다. HarvestManager는 일일 및 세션 누적 수확 기록을 가진다. 누적 작물별 데이터는 아직 디스크에 저장하지 않는다.

### 강화 노드

`UpgradeNodeData` SO는 NodeId, 표시명·설명, 아이콘, 선행 조건, 레벨별 비용과 효과를 가진다. 레벨별 효과는 해당 레벨의 전체 효과이며 이전 레벨 효과를 다시 더하지 않는다. 실제 구매 레벨은 UpgradeManager의 Dictionary에 기록한다.

- 현재 SO: `Assets/04.Data/SO/강화/UpgradeNode_Farm2x2.asset` (`farm_2x2`, 1레벨), `UpgradeNode_HarvestSpeed.asset` (`harvest_speed_01`, 3레벨, 2×2 선행).
- 강화 유형은 FarmSize 및 AttackSpeedPercent. UpgradeStats는 계산 결과만 담는다.
- CalculateStats는 농장 목표 크기의 축별 최댓값 및 노드 현재 레벨 속도 보너스의 합을 계산한다. ApplyStats는 실제 대상에 적용한다. 구매 및 로드에서 재사용.
- TryPurchase는 Settlement에서만 허용. 구매 중복 잠금, 선행 조건, 비용, 농장 준비를 확인하고 골드 차감·레벨 갱신·효과 적용 뒤 UI 이벤트를 전달한다.
- UpgradeTreeUI/UpgradeNodeUI는 미리 캐시한 UI와 상태 이벤트를 사용한다. 잠긴 노드는 숨기고 구매 가능/불충분/완료를 표시하는 구조. 연결선을 목적 노드 그룹 아래에 두어 함께 해금하는 방향. 노드 및 연결선은 수동 배치하고 Requirements가 해금의 유일한 기준이다. 별도 NextNodes 상태를 만들지 않는다. 실제 씬의 연결선 배치와 호버 설명 UI는 필요한 작업 시 확인한다.

### 대화

DialogueCharacterData: 캐릭터 표시명과 표정 ID/스프라이트. DialogueData: DialogueId, MemoirTitle, 대사 배열(캐릭터, 표정 ID, 좌우, 반전, 텍스트). DialoguePlayer.TryPlay는 전체 대본의 유효성을 확인하고 시작 시 DialogueStarted 이벤트를 보낸다. 좌우 DialoguePortraitSlot으로 초상을 표시하고 DOTween으로 타이핑 및 초상 효과를 처리한다.

현재 실제 테스트 대본은 `Assets/04.Data/SO/대화/Dialogue_.asset`이며 아직 DialogueId/MemoirTitle이 입력되지 않았다. 실제 이야기 콘텐츠와 이벤트 트리거는 없다. 정상 재생이 시작된 대화만 발견으로 취급하며 스킵해도 발견 기록을 유지하는 설계다.

### 저장과 복원

SaveManager는 `Application.persistentDataPath/SaveData/` 하위에 Progress.json과 Memoir.json을 관리한다. 두 저장의 버전은 현재 1이다. 파일 경로는 콘솔에 출력된다.

- Progress.json: SaveManager가 DayManager.StateChanged의 Result에서만 CaptureLevels, DayNumber, Gold를 기록한다. 구매/밤 종료/게임 종료에서는 저장하지 않는다.
- Memoir.json: DialogueStarted 이벤트에서 RecordDialogue가 새 ID를 HashSet으로 등록하고 즉시 저장한다. 같은 ID는 중복 기록하지 않는다. 진행 저장과 독립적이다.
- LoadOrNew는 원본 읽기·복원에 실패하면 .bak을 시도한다. 둘 다 복원하지 못한 경우 해당 파일 덮어쓰기를 중지하고 오류를 기록한다. WriteJson은 .tmp에 먼저 쓰고 기존 파일을 교체하며 백업을 유지한다. 실제 플랫폼의 파일 교체 지원과 복구 결과는 테스트 필요.
- SaveManager는 DefaultExecutionOrder(100). Awake에서 회고록을 읽고 Start에서 기존 활성 게임 매니저들의 준비 후 진행을 로드한다. 노드 ID/레벨·선행 조건을 검증한 뒤 TryRestoreLevels → ApplyStats → 골드 직접 복원 → RestoreCompletedDay → Settlement → UI 통지를 실행한다. RestoreCompletedDay는 최초 작물을 제거하고 이미 보상을 받았다는 플래그를 설정한다.
- 파생 스탯, 농장 크기, 노드 해금 여부, 작물 HP, 낮 타이머, 누적 수확 통계, 조건별 대화 실행 플래그는 현재 저장 대상이 아니다.
- 현재 로드는 새 게임 씬 최초 초기화 전용이다. 진행 중 로드나 세이브 슬롯 전환은 미지원이며 농장 축소 기능도 없다.

### 타이틀 (0.45)

TitleUI는 Inspector OnClick으로 StartGame/OpenOptions/OpenMemoir/ReturnToMenu/QuitGame을 연결하고, InputActionReference를 통해 취소 입력을 구독·해제한다. 방향키로 메뉴를 선택하며, 패널에서 돌아오면 이전 버튼으로 포커스를 복원한다.

게임 시작 시 TitleUI가 타이틀 Canvas의 `Assets/03.Prefabs/SerWelcome.prefab`을 열고 세르의 짧은 인사 네 가지 중 하나를 표시한다. 한 프레임을 그린 뒤 FarmPrototype을 Single 모드로 비동기 로드한다. 인사 표시 1.2초와 실제 씬 준비가 모두 끝나면 0.16초 동안 어두워진 뒤 농장 씬을 활성화한다. 전환 중에는 시작·취소·패널 입력을 막는다.

프리팹의 `SerWelcomeEffects`가 세르의 손 위 인장을 천천히 돌리고, 금빛 입자 다섯 개를 손에서 반투명 대사 카드의 가장자리로 보낸다. 입자의 출발점은 초상화에, 도착점은 카드에 붙어 있어 화면 비율이 바뀌어도 두 위치를 잇는다. ‘별장 준비 중’의 점은 0.3초마다 1~3개로 바뀐다. 이 연출과 페이드는 게임 시간 배율과 독립적이다.

Windows 플레이어 자동 입력 테스트에서 네 대사를 각각 표시하고 마우스·키보드로 농장에 진입했다. 이 PC에서 최종 네 차례의 시작 입력부터 농장 활성화까지 1.39~1.41초가 걸렸다. 1600×900, 1024×768, 1920×810, 960×540에서 대사와 카드 배치, 인장 회전, 입자 이동, 글자 위치를 유지하는 점 세 단계를 확인했다. 시간 배율 0에서도 연출이 진행되고 농장의 Ready 상태에 도달했다. 인사는 타이틀 UI에만 표시된다. 기존 세이브가 없는 테스트에서 전환으로 생성된 저장 파일은 없었다.

옵션/회고록은 아직 빈 패널이며 저장 파일을 타이틀에서 읽지 않는다. 회고록 구현 시 게임 매니저가 없는 상태에서도 Memoir.json을 읽을 수 있도록 기존 파일 처리 책임을 최소한으로 분리하는 방안을 검토한다. 현재 DialoguePlayer는 DayManager와 로비 CanvasGroup을 요구하므로 타이틀 다시보기용 재사용 전에 이 의존성을 정리해야 한다. 실제 대화 이벤트와 회고록 재생은 보상·진행 처리와 분리해야 한다.

## 4. 알려진 보완점과 검증 대기

지금 발견한 사항은 요청 범위 밖이면 임의 수정하지 않는다.

- [ ] 진행 세이브: 새 게임, 낮 종료 저장(0수확 포함), 밤 구매 후 롤백, 다음 낮 완료 후 강화 복원, 중복 판매금 없음, 다음 날 전체 칸 재배를 실제 Play Mode에서 확인.
- [ ] 회고록: 테스트 SO에 ID/제목 설정, 정상 시작 시 Memoir.json 생성, 중복 ID 미등록, 진행 롤백과 무관한 유지, 잘못된 대본 재생 실패 시 미등록, 손상 파일 및 백업 복원 검증.
- [ ] 대화 씬 참조: FarmPrototype에서 speakerNameText와 bodyText가 동일 TMP에 연결된 상태를 확인했다. 실제 콘텐츠 적용 시 분리 필요 여부 확인.
- [ ] DialoguePlayer.Next는 원격 소스 기준 타이핑 중에도 다음 줄로 넘어간다. 타이핑 중 첫 Next는 전체 문장을 표시하고 다음 Next에서 넘기는 동작은 미반영.
- [ ] DialoguePlayer.HandleDayStateChanged는 Settlement가 아닌 상태 전환에서 대화를 종료한다. 낮 시작/진행 조건 대화 전에 대화와 라운드의 진행·입력 차단 규칙을 확정한다.
- [ ] 대화 ID의 전체 목록/고유성 검증, ID → DialogueData 조회, 회고록 목록 및 다시보기 UI, 실제 이벤트의 재생 기록은 미구현.
- [ ] 누적 작물별 기록은 향후 도감/숙련 할인을 위해 저장할 필요가 있는지 결정한다.
- [ ] 타이틀 메뉴와 전환의 실제 게임패드 탐색·취소 입력을 확인한다. 마우스와 키보드 검증은 위 타이틀 항목에 기록했다.
- [ ] 타이틀 표시 버전 문자열은 v0.4로 되어 있다. 0.45 개발 버전과 일치하도록 표시할지 결정.
- [ ] 0.45에서 Unity Splash Screen과 Splash Logo 설정이 비활성화되었다. 사용자 요청에 없던 프로젝트 설정 변경으로, 유지 여부를 확인한다.

## 5. 다음 작업 후보

우선순위는 사용자와 합의한다. UI 구조가 준비되어 있으므로 실제 옵션 기능, 회고록 목록 및 재생, 조건부 대화 트리거를 작은 단위로 연결할 수 있다. 타이틀에서 농장 씬 없이 회고록을 읽을 수 있어야 하고, 저장/복원의 미검증 항목은 관련 구현 때 우선 검증한다. 이후 작물 종류와 노드 효과를 단계별로 확장한다. 엔딩 등 미확정 기획을 자동으로 구현하지 않는다.

문서를 갱신할 때 구현 완료와 실제 실행 검증 완료를 구분하고, 기준 커밋 및 발견 사항을 함께 적는다.
