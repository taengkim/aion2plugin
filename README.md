# Aion2 Plugin - 파티 DPS & 아툴 점수 오버레이

아이온2 게임 패킷을 감청해 **파티원 딜량**과 **아툴 점수**([aion2tools.com](https://aion2tools.com))를 게임 화면 위에 실시간으로 표시합니다.

> 패킷 분석 기반: [AION2Meter4J](https://github.com/ion2me/Aion2Meter_public)

## 기능

| 기능 | 설명 |
|------|------|
| **실시간 DPS** | 파티원별 초당 데미지, 누적 딜, 딜 비율 (%) |
| **아툴 점수** | aion2tools.com 연동, 파티원 입장 시 자동 조회 + 10분 캐시 |
| **반투명 오버레이** | 게임 위에 항상 표시, 드래그 이동, 투명도 조절 |
| **클릭 투과** | 오버레이 켜진 상태에서도 게임 클릭 정상 작동 |
| **시스템 트레이** | 백그라운드 실행, 트레이 아이콘으로 언제든 제어 |
| **전투 자동 감지** | 데미지 패킷 수신 시 자동 세션 시작, 12초 비활성 시 자동 종료 |

## 오버레이 미리보기

```
┌─────────────────────────────────────────────────┐
│ ⚔ AION2 PLUGIN - DPS & ATUUL          🔓  ×  │
├─────────────────────────────────────────────────┤
│ ● 전투 중                              02:34   │
├─ # ─ 이름 ─────── 총 딜 ─── DPS ── % ── 아툴 ─┤
│  1  ⚔ Taengkim   1.23B   45,231  38.2%  A 9821│
│  2  🔮 Mageuser   980M   36,102  30.5%  B 7634│
│  3  🗡 Assassin   750M   27,891  23.3%  S12043│
│  4  ✚ Healer      256M    9,481   8.0%  C 4521│
├─────────────────────────────────────────────────┤
│ [초기화] [설정]     파티 총 딜: 3.22B          │
└─────────────────────────────────────────────────┘
```

---

## 요구 사항

| 항목 | 버전 |
|------|------|
| OS | Windows 10 / 11 (x64) |
| 런타임 | [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| 드라이버 | [Npcap 1.79+](https://npcap.com/) |
| 권한 | 관리자 (패킷 캡처 필수) |

---

## 빌드

### 사전 준비

```
- Visual Studio 2022 (17.8+)  또는  .NET SDK 8.0+
- Windows 빌드 환경 필수 (WPF 프로젝트)
```

### 1. 소스 클론

```bash
git clone https://github.com/taengkim/aion2plugin.git
cd aion2plugin
```

### 2. NuGet 패키지 복원 & 빌드

```bash
dotnet restore AionPlugin.sln
dotnet build AionPlugin.sln -c Release -r win-x64
```

빌드 결과물:

```
src/AionPlugin/bin/Release/net8.0-windows/win-x64/
├── AionPlugin.exe        ← 실행 파일
├── AionPlugin.dll
└── ...
```

### 3. 단일 파일 배포본 생성 (선택)

```bash
dotnet publish src/AionPlugin/AionPlugin.csproj \
  -c Release \
  -r win-x64 \
  --self-contained false \
  -p:PublishSingleFile=true \
  -o publish/
```

결과: `publish/AionPlugin.exe` (단일 실행 파일)

---

## 설치 & 실행

### 1단계: Npcap 설치

1. [https://npcap.com/](https://npcap.com/) 에서 최신 설치 파일 다운로드
2. 설치 시 **"Install Npcap in WinPcap API-compatible Mode"** 체크 필수
3. 설치 완료 후 **재부팅 권장**

### 2단계: 플러그인 실행

1. `AionPlugin.exe` 를 **우클릭 → 관리자 권한으로 실행**
2. 설정 창이 열립니다

> 관리자 권한 없이 실행하면 Npcap 이 패킷을 캡처하지 못합니다.

### 3단계: 설정

**네트워크 탭:**

| 항목 | 기본값 | 설명 |
|------|--------|------|
| 네트워크 어댑터 | 자동 선택 | 실제 인터넷 연결에 사용하는 어댑터 선택 |
| 서버 CIDR | `206.127.156.0/24` | 변경 불필요 (게임 서버 IP 대역) |
| 서버명 (아툴용) | 비어있음 | aion2tools.com 에서 사용하는 서버명 입력 |

**오버레이 탭:**

| 항목 | 설명 |
|------|------|
| 항상 위 | 오버레이가 게임 창 위에 유지 |
| 아툴 점수 표시 | 점수 컬럼 표시/숨김 |
| 클릭 투과 | 체크 시 오버레이 클릭이 게임으로 통과 |
| 투명도 | 슬라이더로 0.3 ~ 1.0 조절 |

### 4단계: 시작

1. **시작** 버튼 클릭
2. 오버레이 창이 자동으로 나타납니다
3. 아이온2 실행 후 파티 전투 시작 → 데이터 자동 수집

---

## 오버레이 조작

| 조작 | 방법 |
|------|------|
| 이동 | 오버레이 상단 타이틀 바를 드래그 |
| 이동 잠금 | 🔓 버튼 클릭 (🔒 로 변경됨) |
| 데이터 초기화 | **[초기화]** 버튼 |
| 설정 창 열기 | **[설정]** 버튼 또는 트레이 아이콘 우클릭 |
| 숨기기 | **×** 버튼 (프로그램 종료 아님, 트레이에서 복원 가능) |
| 완전 종료 | 트레이 아이콘 우클릭 → **종료** |

---

## 패킷 구조 참고

본 프로젝트는 [AION2Meter4J](https://github.com/ion2me/Aion2Meter_public) 의 역공학 결과를 기반으로 합니다.

| 항목 | 값 |
|------|-----|
| 서버 포트 | `13328` |
| 서버 IP 대역 | `206.127.156.0/24` |
| 패킷 구분자 | 트레일러 `06 00 36` |
| 숫자 인코딩 | VarInt (7-bit) |
| 일반 데미지 OpCode | `04 38` |
| DoT 데미지 OpCode | `05 38` |
| 닉네임 OpCode | `04 8D` |
| 크리티컬 판정 | `type == 3` |

게임 패치로 패킷 구조가 변경된 경우 `src/AionPlugin/Network/PacketParser.cs` 를 수정하세요.

---

## 아툴 점수 API 연동

`src/AionPlugin/Services/AionToolsService.cs` 에서 엔드포인트와 파싱 로직을 aion2tools.com 실제 응답에 맞게 수정하세요.

```csharp
// URL 예시 (실제 API 경로로 교체 필요)
string url = $"{BaseUrl}/api/character/{serverName}/{characterName}";
```

---

## 트러블슈팅

**"네트워크 어댑터를 찾을 수 없습니다"**
- Npcap 이 설치되어 있는지 확인
- WinPcap API 호환 모드로 재설치

**오버레이가 게임 화면에 안 보임**
- 오버레이 탭 → **항상 위** 체크 확인
- 풀스크린 게임은 **창 모드** 또는 **창 모드 (전체화면)** 으로 실행

**데이터가 수집되지 않음**
- `AionPlugin.exe` 를 **관리자 권한으로 실행**했는지 확인
- 설정 창 **로그 탭**에서 오류 메시지 확인
- 올바른 네트워크 어댑터가 선택됐는지 확인 (VPN 사용 시 VPN 어댑터 선택)

**VPN 사용 시**
- VPN 어댑터를 선택하거나, 서버 CIDR 을 직접 입력

---

## 면책 조항

이 도구는 **읽기 전용** 패킷 스니핑만 사용합니다. 게임 서버로 어떠한 데이터도 전송하지 않습니다.
사용 전 게임사의 이용약관을 확인하세요.

## 라이선스

MIT
