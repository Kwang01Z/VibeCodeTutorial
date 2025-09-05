# Scene Setup Guide - Testing Input + Lane Integration

## 🎮 Quick Test Scene Setup

### 1. Create Basic Scene Structure

```
TestScene
├── Runner (GameObject)
│   ├── Model (3D object hoặc primitive Capsule)
│   ├── Rigidbody
│   ├── LaneController
│   ├── InputHandler
│   └── SimpleRunnerController
├── Ground (Plane scaled to be long runway)
└── Main Camera (positioned behind runner)
```

### 2. Component Configuration

#### Runner GameObject:
1. **Rigidbody Component:**
   - Mass: 1
   - Drag: 1
   - Angular Drag: 5
   - Use Gravity: ✓
   - Is Kinematic: ❌

2. **LaneController:**
   - Config: Create `LaneConfig` asset (right-click → Create → EndlessRunner → Lane Config)
   - Lane Positions: [-2, 0, 2] (hoặc tùy chỉnh)
   - Lane Change Speed: 8
   - Physics Mode: AddForce (hoặc Velocity)

3. **InputHandler:**
   - Input Config đã có defaults hợp lý
   - Platform: Auto (sẽ detect PC/Mobile automatically)
   - PC Keyboard Keys: Space (Jump), S (Slide), A/D (Lane)
   - Enable Debug: ✓ (để xem logs)

4. **SimpleRunnerController:**
   - Input Handler: Drag reference từ InputHandler component
   - Lane Controller: Drag reference từ LaneController component

### 3. Keyboard Controls (PC)

**Default Controls:**
- **Jump**: `Space`, `W`, `Up Arrow`
- **Slide**: `S`, `Down Arrow`  
- **Lane Left**: `A`, `Left Arrow`
- **Lane Right**: `D`, `Right Arrow`
- **Manual Item**: `E`
- **Pause**: `Escape`

**Customizable Keys:**
Bạn có thể thay đổi keys trong Inspector của InputHandler:
- Jump Key: Configurable KeyCode
- Slide Key: Configurable KeyCode
- Lane Left Key: Configurable KeyCode
- Lane Right Key: Configurable KeyCode
- Manual Item Key: Configurable KeyCode
- Pause Key: Configurable KeyCode

### 4. Mobile Testing

InputHandler tự động detect mobile và switch sang touch controls:

**Swipe Controls:**
- Swipe Up: Jump
- Swipe Down: Slide  
- Swipe Left: Move left lane
- Swipe Right: Move right lane

**Swipe Settings (tunable in inspector):**
- Swipe Threshold: 100 pixels
- Swipe Time Limit: 0.2s
- Buffer Time: 0.15s

### 5. Debug & Testing

#### Console Logs (khi Enable Debug = true):
```
[InputHandler] Jump input detected
[LaneController] Lane change requested: 0 → 1
[SimpleRunnerController] Executing lane change to lane 1
[LaneController] Lane change completed: 1
```

#### Visual Gizmos:
- LaneController sẽ draw lane positions trong Scene view
- Current lane được highlight

#### Testing Checklist:
- [ ] PC keyboard input works (A/D for lane change, Space for jump)
- [ ] Mobile swipe detection (test on device hoặc Unity Remote)
- [ ] Input buffering (input while lane changing still gets executed)
- [ ] Input cooldowns (prevent spam clicking)
- [ ] Lane bounds validation (can't go beyond min/max lanes)
- [ ] Physics movement (smooth lane transitions với animation curve)

### 6. Common Issues & Solutions

#### Issue: Input not working
**Solution:** Check InputHandler enabled, platform detection correct, và Input Actions properly assigned

#### Issue: Lane change too fast/slow  
**Solution:** Adjust `_laneChangeSpeed` trong LaneConfig asset

#### Issue: Mobile swipes not detected
**Solution:** Check swipe threshold values, test on actual device instead of editor

#### Issue: Physics jittery
**Solution:** Ensure consistent FixedUpdate usage, check Rigidbody settings

### 7. Next Steps After Testing

Once basic integration works:
1. **Add obstacle prefabs** để test collision
2. **Implement jumping/sliding physics** trong RunnerController
3. **Add visual effects** cho lane changes và actions
4. **Setup camera follow** system
5. **Add audio feedback** cho inputs

### 8. Performance Monitoring

Watch for:
- **GC Allocation:** Should be 0 during runtime (check Profiler)
- **Frame Rate:** Stable 60 FPS
- **Input Latency:** Response should feel immediate
- **Physics Stability:** No jitter during lane changes

### 9. Running Unit Tests

**Timer Tests:**
1. Tạo empty GameObject trong scene
2. Add component `TimerTests` 
3. Press Play - tests sẽ tự động chạy
4. Hoặc right-click component → "Run All Timer Tests"
5. Check Console log cho kết quả:
   ```
   === TIMER TESTS COMPLETED ===
   Total: 13 | Passed: 13 | Failed: 0
   ALL TESTS PASSED! ✓
   ```

**Test Settings:**
- Run Tests On Start: ✓ (chạy khi Start())
- Enable Detailed Logs: ✓ (hiển thị từng test case)
- Context Menu: "Run All Timer Tests" để chạy manual

---

**Ready to test!** 🚀 Theo hướng dẫn này để setup scene và test integration giữa InputHandler và LaneController.
