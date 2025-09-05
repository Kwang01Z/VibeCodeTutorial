# ROADMAP – Endless Runner (Mèo) – Kế hoạch triển khai

Phiên bản tài liệu: 0.1 (khởi tạo)
Giả định: Unity 2022 LTS, URP, New Input System, Scripting Backend IL2CPP, .NET Standard 2.1, UniTask cho async, Addressables cho tài nguyên.
Có thể thay đổi: thiết bị mục tiêu, FPS, pipeline build, phạm vi theme/nhân vật.

---

## 1) Tóm tắt yêu cầu (tổng hợp từ 2 file mô tả)
- Thể loại: Endless Runner 3 làn (Trái – Giữa – Phải).
- Mục tiêu: né chướng ngại, nhặt vật phẩm và tiền tệ (Xương Cá, Bẫy Chuột), đạt quãng đường/điểm cao.
- Core loop: Chạy → né/thu thập → tăng tốc theo quãng đường → hết mạng → Endgame/Revive.
- Hệ thống chính: Điều khiển (PC/Mobile), Lane & Speed, Va chạm & mạng + I-frames, Item (Magnet/x2/Invisible/Life), Spawn chunk khó/dễ + seed, UI (Main Menu, Store, Mission, Leaderboard, In-game HUD), Theme, Save/Load (binary), Audio/Mixer, Analytics.

### 1.1) Các điểm cần làm rõ (blocking specs)
- Input & cảm giác điều khiển:
  - Coyote time (ms) và input buffer (ms) mặc định cho Jump/Slide? Đề xuất: 100ms/100ms.
  - Thời lượng Jump/Slide (ms) và LaneChange blend time (ms)? Đề xuất: Jump 500–600ms, Slide 600–700ms, LaneChange 180–220ms.
  - Cooldown tối thiểu giữa 2 hành động (ms) theo loại? Đề xuất: LaneChange 150ms, Jump 250ms, Slide 300ms.
  - Mobile swipe: ngưỡng px và thời lượng tối đa? Đề xuất: 80–120px trong 120–200ms. Left-hand mode: đảo trục UI hay mapping?
- Vật phẩm & stacking:
  - Thời lượng mặc định: Magnet 10s (bán kính 1.2 lane), x2 10–12s (stack multiplicative), Invisible 4s, Life: +1 nếu < cap.
  - Quy tắc chồng: x2 có thể xN; Invisible không huỷ Magnet/x2; manual item slot = 1, cooldown bao nhiêu giây?
- Kinh tế & revive:
  - Tỷ lệ rơi coin/item; hệ số điểm theo tốc độ; bảng giá revive theo lần trong cùng lượt (vd: 1→2→3 bẫy chuột?).
  - Phần thưởng endgame: công thức quy đổi xương cá/bonus theo distance.
- Spawn & độ khó:
  - Định nghĩa difficulty tag cho chunk và barrier “không thể né” ở tốc độ cao; khoảng cách tối thiểu giữa bẫy khó (m).
  - Có sử dụng seed cố định theo run để tái lập/ghi replay? Cross-platform determinism cần chốt RNG.
- Theme & camera:
  - Mỗi theme có lighting preset, FOV/camera offset, bảng spawn riêng? Điều kiện mở khoá.
- Kỹ thuật:
  - Controller dùng Rigidbody physics-based (không CharacterController). Kinematic = false để tận dụng Unity physics.
  - Update xử lý input & state transitions; FixedUpdate xử lý movement/collision/physics forces.
  - Constraints: freeze rotation XYZ, freeze position Z. Drag/Angular Drag tune theo feel.
  - Target devices & FPS: Android (60/120Hz), iOS (60/120Hz). Safe area & tỉ lệ 886x1920.
- Lưu trữ & bảo toàn dữ liệu:
  - Định dạng save binary: versioning, checksum/obfuscation tối thiểu; chiến lược reset/migration.
- Analytics:
  - Schema event (tham số bắt buộc) cho start_run, end_run, item_pick/use, mission_progress, revive_used.

### 1.2) Đề xuất mở rộng (non-blocking)
- Character/Accessory có chỉ số: +% thời lượng item, 1 lần miễn nhiễm hit đầu, bonus coin.
- UX: Wheel chọn item (slow time) khi kích hoạt manual; hiệu ứng camera shake vi mô khi va.
- Anti-cheat: checksum score theo seed, validate tốc độ tối đa, phát hiện pause abuse.
- Tooling Editor: Menu tạo ChunkDefinition từ scene, Validator spawn rule, Visualizer speed curve.

---

## 2) Phân rã theo Phase

### P0 – Khởi tạo & Phân tích yêu cầu
Mục tiêu: Có tài liệu, cấu hình dự án, khung repo.
- Khởi tạo tài liệu (file này) và Architecture overview.
- Xác nhận giả định kỹ thuật (Unity 2022 LTS, URP, Input System, UniTask, Addressables).
- Chuẩn bị repo Git: nhánh main/dev/feature, .gitattributes (LFS nếu cần), .editorconfig.
- Cấu hình project: IL2CPP, .NET Std 2.1, SRP Batcher, Quality (mobile), VSync off, target 120 FPS.
- Thiết lập package: Input System, Addressables, NUnit, (tùy) CI template.

Kết quả chấp nhận: Project mở được, build Development thành công Windows/Android; tài liệu ROADMAP và Architecture có sẵn.

---

### P1 – MVP Core Gameplay
Mục tiêu: Chơi được vòng lặp cơ bản trên 1 theme mặc định.
- LaneSystem: cấu hình 3 lane (tọa độ X), chuyển làn bằng AddForce/velocity lerp và giới hạn biên.
- RunnerController: Rigidbody + State machine Run/Jump/Slide/LaneChange/Hit/IFrame; physics-based movement với constraints.
- InputHandler: PC (Arrow ↑↓←→), Mobile (swipe U/D/L/R, ngưỡng px và thời lượng), chế độ tay trái. Update polling.
- SpeedManager: điều chỉnh Rigidbody.velocity.z theo curve, vmax, gia tốc mượt trong FixedUpdate.
- ObstacleSpawner: spawn theo ChunkDefinition với Colliders, tag độ khó, quy tắc tránh 2 chunk khó liên tiếp.
- Collision/Life: OnTriggerEnter với layer filtering; 3 mạng; -1 mạng khi va; I-frames 1.0–1.5s.
- HUD tối thiểu: distance, score, heart, pause.

Kết quả chấp nhận: Chạy ≥500m không crash; input mượt; va chạm/trừ mạng/I-frames đúng; pause/resume ổn.

---

### P2 – Item & Currency
Mục tiêu: Thu thập và kích hoạt item, quản lý tiền tệ.
- ItemPickup + ItemEffectSystem: auto-activate (Magnet/x2/Invisible/Life), slot manual 1 item + cooldown; stacking rule (x2 có thể chồng xN; Invisible không đè lên Magnet/x2).
- CurrencyManager: Xương Cá (on-run), Bẫy Chuột (mission/đường chạy/nạp stub); economy config.
- UI HUD: hiển thị coin run, item đang hoạt động + timer, nút dùng item thủ công.

Kết quả chấp nhận: Nhặt/áp dụng hiệu ứng đúng thời lượng; x2 stack đúng; Life không vượt cap; số dư cập nhật đúng.

---

### P3 – Theme & Procedural Map
Mục tiêu: Hỗ trợ nhiều theme, chunk theo độ khó.
- ThemeDefinition: lighting, obstacles set, speed modifier, spawn table riêng.
- ChunkSystem: độ dài, sockets, difficulty tag; seed để tái lập run.
- Addressables: load/pool chunk & obstacle; rule chống spawn rác (khoảng cách tối thiểu giữa bẫy khó, không đặt item ngay trước đoạn không thể né).

Kết quả chấp nhận: Đổi theme từ menu; run tái lập bằng seed; không có “combo không thể né”.

---

### P4 – Meta-game & UI Framework
Mục tiêu: Meta đầy đủ và khung UI điều hướng.
- Scene: Boot → MainMenu → Game; panel stack bằng CanvasGroup.
- Store: xem và mua (stub IAP), tab Item/Character/Accessory/Theme.
- MissionSystem: 3 nhiệm vụ song song, reroll 1 lần/ngày, phần thưởng và tiến độ.
- Leaderboard: local high score; tie-breaker theo distance/time/coins.
- Save/Load: binary; reset data; Setting (âm lượng, left-hand mode), Onboarding/Tutorial 30–60s.
- Revive flow: popup khi hết mạng lần đầu, giá Bẫy Chuột tăng dần; hồi về tốc độ thấp hơn, cấp I-frames 2–3s.

Kết quả chấp nhận: Điều hướng UI mượt, dữ liệu meta lưu/đọc ổn, revive đúng quy tắc.

---

### P5 – Audio, VFX, Feedback, Accessibility
- AudioManager: Mixer tách Music/SFX, ducking khi pause/popup; Addressables clip.
- Feedback: haptic, vignette khi Invisible, cảnh báo hết hiệu lực item, âm báo tăng tốc.
- Accessibility: color-blind palette, toggle chơi một tay.

---

### P6 – Optimization & Polish
- Pooling toàn bộ spawnable; giảm GC spike; pre-allocate list/arrays.
- Batching: SRP Batcher on, material atlas; giảm overdraw UI.
- Jobs/Burst: cân nhắc cho tính toán nặng (vd: đường hút magnet nhiều coin).
- Memory budget <200MB, APK/IPA size tối ưu bằng Addressables.

---

### P7 – Testing, CI/CD & Release
- Unit test (logic item/economy/mission) + Play Mode test (điều khiển/va chạm/flow) chạy trong CI.
- Build pipeline Android/iOS/Windows IL2CPP; auto-versioning; symbol upload.
- Analytics events: start_run, end_run, distance, coins, hits, item_pick/use, mission_progress, revive_used, speed_peaks.
- Beta Test (phân phối nội bộ), checklist phát hành.

---

## 3) Kiến trúc & Thành phần

Note: Tuân thủ chuẩn dự án – cache component, tránh alloc hot path, ưu tiên event-driven/UniTask thay Coroutine.

### 3.1. Components (MonoBehaviour)
- RunnerController, InputHandler, SpeedManager, LaneController, Obstacle, Pickup, ChunkAnchor, ItemButton, LifeUI, ThemeLightRig.

### 3.2. Systems/Managers (dịch vụ đơn nhẹ)
- GameStateMachine, ObjectPool, CurrencyManager, ItemEffectSystem, MissionSystem, SaveService, AudioManager, AnalyticsService.

### 3.3. ScriptableObjects
- LaneConfig, SpeedCurve, ItemDefinition, ThemeDefinition, ChunkDefinition, ObstacleConfig, MissionDefinition, EconomyConfig, CharacterDefinition, AccessoryDefinition.

### 3.4. Dòng đời
- Awake: khởi tạo nội bộ/pool. OnEnable: subscribe sự kiện. OnDisable/OnDestroy: unsubscribe/Dispose.
- Update: input polling, state machine transitions, UI updates, coyote/buffer timers.
- FixedUpdate: physics movement (AddForce/velocity), collision detection, lane positioning.

---

## 4) User Stories & Acceptance Criteria (rút gọn)
- Là người chơi, tôi có thể đổi làn trong 200ms với animation mượt → không bị kẹt biên; test bằng 50 lần đổi liên tục.
- Là người chơi, tôi có thể nhảy/trượt với coyote time 100ms, input buffer 100ms → không “nuốt” nút ở mép chướng ngại.
- Khi va chạm, trừ 1 tim và có I-frames 1.2s → không trừ liên tiếp trong I-frames; có hiệu ứng ghost.
- Nhặt x2 thì điểm từ coin nhân đôi; stack 2 lần thành x4 trong thời lượng chồng lấn.
- Invisible xuyên chướng ngại trong 4s nhưng vẫn hút coin nếu có Magnet.
- Revive 1 lần/lượt (giá tăng dần), hồi tốc độ xuống mức an toàn và cấp I-frames 2.5s.

---

## 5) Quy tắc Spawn & Độ khó
- Chunk có difficulty tag (Easy/Medium/Hard). Không nối Hard→Hard.
- Khoảng cách tối thiểu giữa bẫy khó: cấu hình theo mét/ticks.
- Không spawn item ngay trước combo “không thể né”.
- Seed cho phép tái hiện run/replay.

---

## 6) Cân bằng tốc độ & điểm
- v(t) = v0 + a·t hoặc theo mốc quãng đường; clamp vmax.
- Score = f(distance, speed factor) + coins (đã nhân xN).
- Dùng curve để tăng tốc mượt, tránh gắt khi qua mốc.

---

## 7) Dữ liệu & Lưu trữ
- Save binary local: profile, currency, unlocks, settings, missions, high score.
- Reset/Clear Data từ Setting.
- Định dạng gợi ý: Header(version, device), Body(serialized), CRC. Obfuscation nhẹ để chống sửa tay.
- Migration: map version → upgrader theo bước, unit test cho mỗi upgrader.

---

## 8) UI/UX
- Panel stack dùng CanvasGroup; tránh Instantiate/Destroy mỗi lần mở.
- HUD: distance, score, hearts, coins, active item + timer, pause.
- Onboarding: hướng dẫn cử chỉ 30–60s đầu, có thể bỏ qua.
- Countdown 3→1 khi resume từ pause.

---

## 9) Âm thanh
- Mixer: Music/SFX; ducking khi pause/popup.
- SFX: đổi làn, nhảy, trượt, nhặt coin, bật/tắt item, va chạm, revive, tăng tốc.
- Routing: Snapshot khi pause (ducking -10dB), bus riêng cho UI.

---

## 10) Performance & Memory Checklist
- Pooling toàn bộ prefab spawn/despawn.
- Tránh alloc trong Update/FixedUpdate; không dùng LINQ hot path; cache component.
- SRP Batcher, culling; giảm overdraw UI; batching sprite/material.
- Mobile: cap FPS, dynamic resolution nếu cần; nén texture/mesh/anim hợp lý.

---

## 11) Rủi ro & Phương án
- Điều khiển swipe không ổn định giữa thiết bị → chuẩn hóa ngưỡng px & thời lượng, thêm test A/B.
- Combo chướng ngại “không thể né” ở tốc độ cao → rule validate offline + guard khi spawn runtime.
- GC spike do timer/effect → dùng struct timer, pre-allocate pools, UniTask không tạo GC.
- IL2CPP khác biệt reflection → tránh reflection động; bật Nullability, Analyzers.
- RNG không quyết định giữa nền tảng → dùng PRNG riêng (vd XorShift) với seed cố định; unit test determinism.

---

## 12) Kiểm thử
- Unit: Item stacking/x2, Life cap, Economy, Mission progress.
- Play Mode: điều khiển (buffer/coyote/cooldown), collision & I-frames, speed curve, revive flow, spawn rules.
- Contract test: seed → run determinism (distance/score) giống nhau qua platform.
- Đo: Profiler/Frame Debugger/Memory Profiler; FPS, batches, GC.Alloc/frame, memory footprint.

---

## 13) Mốc & Ước lượng (tham khảo)
- P1 (MVP): 1.5–2 tuần
- P2–P3: 1.5–2 tuần
- P4: 1–1.5 tuần
- P5–P6: 1–1.5 tuần
- P7: 0.5–1 tuần

---

## 14) Backlog (rút gọn)
- Character/Accessory chỉ số đặc biệt (ví dụ +5% thời lượng item, miễn nhiễm hit đầu).
- Theme mở rộng: đêm/mưa/sương mù thay đổi FOV/camera, spawn table riêng.
- Analytics mở rộng: funnel tutorial, retention events.
- Netcode leaderboard/cloud save (tương lai).

---

Chủ trương tối ưu & coding standard: theo checklist hiệu năng/memory; không LINQ hot path; dùng UniTask thay Coroutine; Addressables + Pooling; phân tách gameplay/presentation; S.O.L.I.D và DI có kiểm soát.

