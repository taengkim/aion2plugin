# Aion2 Plugin - 파티 DPS & 아툴 점수 오버레이

아이온2 게임 패킷을 감청해 **파티원 딜량**과 **아툴 점수**([aion2tools.com](https://aion2tools.com))를 게임 화면 위에 실시간으로 표시합니다.

## 기능

| 기능 | 설명 |
|------|------|
| **실시간 DPS** | 파티원별 초당 데미지, 누적 딜, 딜 비율 |
| **아툴 점수** | aion2tools.com API 연동, 파티원 입장 시 자동 조회 |
| **반투명 오버레이** | 게임 위에 항상 표시, 드래그 이동, 투명도 조절 |
| **클릭 투과** | 오버레이 활성화 시에도 게임 클릭 가능 |
| **시스템 트레이** | 백그라운드 실행, 트레이 아이콘으로 제어 |

## 스크린샷

```
┌─────────────────────────────────────────────────┐
│ ⚔ AION2 PLUGIN - DPS & ATUUL          🔓  ×  │
├─────────────────────────────────────────────────┤
│ ● 전투 중                              02:34   │
├─ # ─ 이름 ────── 총 딜 ── DPS ─ % ── 아툴 ──┤
│  1  ⚔ Taengkim  1.23B   45,231  38.2%  A 9821 │
│  2  🔮 Mageuser  980M   36,102  30.5%  B 7634 │
│  3  🗡 Assassin  750M   27,891  23.3%  S 12043│
│  4  ✚ Healer    256M    9,481   8.0%  C 4521  │
├─────────────────────────────────────────────────┤
│ [초기화] [설정]   파티 총 딜: 3.22B            │
└─────────────────────────────────────────────────┘
```

## 요구 사항

- Windows 10/11 (x64)
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Npcap](https://npcap.com/) (패킷 캡처 드라이버)
- 관리자 권한 (패킷 캡처를 위해 필요)

## 빌드

```bash
# Visual Studio 2022 또는 .NET SDK 8.0+
dotnet build AionPlugin.sln -c Release

# 출력: src/AionPlugin/bin/Release/net8.0-windows/AionPlugin.exe
```

## 설치 & 실행

1. [Npcap](https://npcap.com/) 설치 (WinPcap API 호환 모드 활성화)
2. `AionPlugin.exe` 를 **관리자 권한**으로 실행
3. 설정 창에서 네트워크 어댑터 선택
4. 게임 서버 IP 입력 (선택, 비워두면 포트 기반 필터링)
5. 서버명 입력 (아툴 점수 조회용, 예: `Israphel`)
6. **시작** 버튼 클릭 → 오버레이 자동 표시

## 패킷 파서 업데이트

아이온2의 패킷 구조는 게임 업데이트에 따라 변경될 수 있습니다.

`src/AionPlugin/Network/PacketParser.cs` 에서 다음을 수정하세요:

```csharp
// OpCode 값 업데이트
private const ushort OP_ATTACK_RESULT  = 0x0075;  // 실제 값으로 수정
private const ushort OP_SKILL_RESULT   = 0x0076;  // 실제 값으로 수정
private const ushort OP_PARTY_INFO     = 0x0097;  // 실제 값으로 수정

// ParseDamagePacket() 의 offset 값도 실제 패킷 구조에 맞게 수정
```

패킷 분석 도구: [Wireshark](https://www.wireshark.org/), [x64dbg](https://x64dbg.com/)

## 아툴 점수 API

`src/AionPlugin/Services/AionToolsService.cs` 의 URL 및 파싱 로직을 aion2tools.com 실제 API에 맞게 업데이트하세요.

## 면책 조항

이 도구는 **읽기 전용** 패킷 스니핑을 사용합니다. 게임 서버에 아무 데이터도 전송하지 않습니다. 사용 전 게임사의 이용약관을 확인하세요.

## 라이선스

MIT
