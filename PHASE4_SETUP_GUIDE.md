# Phase 4 - Setup Guide: Scene & Prefab Testing

**Tài liệu**: Hướng dẫn setup scene test cho ItemEffectSystem Phase 4  
**Phiên bản**: 1.0  
**Ngày tạo**: January 4, 2025  
**Mục tiêu**: Tạo môi trường test hoàn chỉnh cho Enhanced Visual Effects, Audio System, và Save/Load

---

## 🎯 **Tổng quan Setup**

Phase 4 cần môi trường test đầy đủ để verify:
- ✅ Enhanced Particle Systems (Magnet/Speed/Invisible/Health effects)
- ✅ Shader Effects Integration (URP compatible)
- ✅ Advanced Audio System với spatial audio
- ✅ Save/Load System với effect persistence
- ✅ Performance monitoring và optimization

---

## 🏗️ **PHẦN 1: CẤU HÌNH PROJECT**

### **1.1 Unity Project Settings**
```
1. Edit → Project Settings → Graphics
   ├── Scriptable Render Pipeline Settings: URP-HighFidelity
   ├── SRP Batcher: ✓ Enable
   ├── Dynamic Batching: ✓ Enable
   └── GPU Instancing: ✓ Enable

2. Edit → Project Settings → Quality
   ├── Rendering:
   │   ├── V-Sync Count: Don't Sync (testing)
   │   ├── Target Frame Rate: 120 (mobile testing)
   │   └── Real-time Reflection Probes: ✓ Enable
   └── Shadows:
       ├── Shadow Quality: Hard Shadows Only
       └── Shadow Resolution: Medium

3. Edit → Project Settings → Audio
   ├── DSP Buffer Size: Best Performance
   ├── Virtual Voice Count: 512
   └── Real Voice Count: 32
```

### **1.2 Package Manager Requirements**
```
Window → Package Manager → Unity Registry:
├── ✅ Universal RP: 12.1.7+
├── ✅ Cinemachine: 2.8.9+
├── ✅ Post Processing: 3.2.2+
├── ✅ Test Framework: 1.1.31+
├── ✅ Input System: 1.4.4+
└── ✅ UniTask: 2.3.3+ (from Git URL)
```

---

## 🎬 **PHẦN 2: TẠO SCENE TEST**

### **2.1 Tạo Scene Mới**
```
1. File → New Scene
   ├── Template: URP → Basic (Indoor/Outdoor)
   ├── Save as: Assets/Scenes/Test_Phase4_ItemEffect.unity
   └── File → Build Settings → Add Open Scenes

2. Hierarchy Setup:
   Test_Phase4_ItemEffect
   ├── 📷 Main Camera (URP + Post Process)
   ├── ☀️ Directional Light
   ├── 🎵 EventSystem
   └── 📱 Canvas (Screen Space - Camera)
```

### **2.2 Scene Lighting Setup**
```
1. Window → Rendering → Lighting:
   ├── Environment:
   │   ├── Skybox Material: Default-Skybox
   │   ├── Sun Source: Directional Light
   │   └── Environment Lighting: Gradient
   └── Mixed Lighting:
       ├── Baked Global Illumination: ✓
       └── Realtime Global Illumination: ✗ (performance)

2. Directional Light Settings:
   ├── Mode: Mixed
   ├── Color: White (255,255,255)
   ├── Intensity: 1.2
   ├── Rotation: (35, 45, 0)
   └── Shadows: Soft Shadows
```

---

## 👤 **PHẦN 3: PLAYER SETUP**

### **3.1 RunnerPlayer Prefab**

Kiểm tra xem prefab đã tồn tại:
```
Assets/Prefabs/Character/RunnerPlayer.prefab
```

Nếu chưa có, tạo từ đầu:

```
1. Tạo Empty GameObject → Rename: "RunnerPlayer"

2. Add Components:
   ├── 📦 Rigidbody
   │   ├── Mass: 1
   │   ├── Drag: 2
   │   ├── Angular Drag: 5
   │   └── Constraints: Freeze Rotation X,Y,Z & Position Z
   ├── 📐 CapsuleCollider
   │   ├── Is Trigger: ✗
   │   ├── Center: (0, 1, 0)
   │   ├── Radius: 0.5
   │   └── Height: 2
   ├── 🎮 RunnerController (Script)
   ├── 💊 HealthComponent (Script)
   ├── ⚡ SpeedManager (Script)
   └── 🎭 Animator

3. Child Objects:
   RunnerPlayer/
   ├── 📦 Model (3D cat model hoặc primitive)
   ├── 🔥 ParticleEffects (Empty, sẽ add sau)
   └── 🔊 AudioSources (Empty, sẽ add sau)
```

### **3.2 Character Visual Model**

Nếu chưa có model 3D:
```
1. Tạo Primitive:
   ├── GameObject → 3D Object → Capsule
   ├── Rename: "CatModel"
   ├── Scale: (0.8, 1, 0.8)
   ├── Material: Tạo Material mới "Mat_CatBody"
   └── Color: Orange (255, 165, 0)

2. Add Simple Animation:
   ├── Window → Animation → Animation
   ├── Create New Animation Clip: "Cat_Run"
   ├── Record simple bob animation (Y position ±0.1)
   └── Loop Time: ✓
```

### **3.3 Player Layer & Tags**
```
1. RunnerPlayer GameObject:
   ├── Layer: Player (8)
   ├── Tag: Player
   └── Position: (0, 0, 0)

2. Kiểm tra Layer Physics:
   ├── Edit → Project Settings → Physics
   ├── Layer Collision Matrix:
   │   ├── Player ↔ Obstacle: ✓
   │   ├── Player ↔ Pickup: ✓
   │   └── Player ↔ Ground: ✓
```

---

## 🏃 **PHẦN 4: TRACK & ENVIRONMENT**

### **4.1 Basic Track Setup**

Tạo đường chạy đơn giản:
```
1. Tạo Ground:
   ├── GameObject → 3D Object → Plane
   ├── Rename: "Ground"
   ├── Scale: (20, 1, 200) - dài cho test
   ├── Material: Tạo "Mat_Ground" (xám nhạt)
   └── Layer: Ground

2. Tạo 3 Lane Markers:
   ├── GameObject → 3D Object → Cube
   ├── Scale: (0.1, 0.5, 200)
   ├── Position Lane Left: (-2, 0.25, 100)
   ├── Position Lane Right: (2, 0.25, 100)
   └── Material: "Mat_LaneMarker" (vàng)
```

### **4.2 Camera Setup**

```
1. Main Camera Settings:
   ├── Position: (0, 3, -5)
   ├── Rotation: (15, 0, 0)
   ├── Camera Component:
   │   ├── Rendering Path: Forward
   │   ├── Allow HDR: ✓
   │   ├── Allow MSAA: ✓
   │   └── Field of View: 60

2. Add Post Process Volume:
   ├── Add Component: Volume (URP)
   ├── Is Global: ✓
   ├── Create Profile: "PostProcess_GameTest"
   └── Add Overrides: Bloom (Intensity: 0.3)
```

### **4.3 Cinemachine Virtual Camera**
```
1. GameObject → Cinemachine → Virtual Camera
   ├── Rename: "vcam_FollowRunner"
   ├── Follow: RunnerPlayer Transform
   ├── Look At: RunnerPlayer Transform
   ├── Body: 3rd Person Follow
   │   ├── Shoulder Offset: (0, 1.5, -4)
   │   ├── Vertical Arm Length: 0.4
   │   └── Camera Side: 0.3
   └── Aim: Same As Follow Target
```

---

## ⚡ **PHẦN 5: ITEMEFFECT SYSTEM SETUP**

### **5.1 ItemEffectSystem Manager**

```
1. Tạo Empty GameObject: "_Systems"
   ├── Add Component: ItemEffectSystem
   ├── Settings trong Inspector:
   │   ├── Enable Effect Logging: ✓
   │   ├── Update Frequency: 0.1
   │   ├── Max Active Effects: 20
   │   └── Show Debug Info: ✓

2. DontDestroyOnLoad Setup:
   ├── _Systems GameObject
   ├── Inspector: DontDestroyOnLoad ✓ (nếu có option)
   └── Hoặc sẽ auto-handle trong ItemEffectSystem script
```

### **5.2 ItemPickup Prefabs**

Tạo prefab cho từng loại item:

**Magnet Item:**
```
1. GameObject → 3D Object → Sphere
   ├── Rename: "ItemPickup_Magnet"
   ├── Scale: (0.5, 0.5, 0.5)
   ├── Layer: Pickup (10)

2. Components:
   ├── SphereCollider: Is Trigger ✓
   ├── Add Script: ItemPickup
   ├── ItemDefinition: Tạo ScriptableObject "Item_Magnet"
   │   ├── Display Name: "Magnet"
   │   ├── Type: Magnet
   │   ├── Duration: 10f
   │   ├── Effect Value: 3f (radius)
   │   ├── Can Stack: ✓
   │   └── Stacking Rule: Add

3. Visual:
   ├── Material: "Mat_Magnet" (màu xanh dương)
   ├── Add Simple Rotate Animation
   └── Add Glow Effect (Emission: Blue)
```

**Speed Item:**
```
1. Tương tự như Magnet nhưng:
   ├── Rename: "ItemPickup_Speed"
   ├── ItemDefinition: "Item_Speed"
   │   ├── Type: Multiplier
   │   ├── Effect Value: 2f (2x speed)
   │   └── Duration: 8f
   ├── Material: "Mat_Speed" (màu đỏ)
   └── Particle System: Speed trails
```

**Invisibility Item:**
```
1. Tương tự:
   ├── Rename: "ItemPickup_Invisible"
   ├── ItemDefinition: "Item_Invisible"
   │   ├── Type: Invisible
   │   ├── Duration: 5f
   │   └── Effect Value: 1f
   ├── Material: "Mat_Invisible" (màu tím, semi-transparent)
   └── Flickering effect
```

**Health Item:**
```
1. Tương tự:
   ├── Rename: "ItemPickup_Health"
   ├── ItemDefinition: "Item_Health"
   │   ├── Type: Life
   │   ├── Effect Value: 1f (1 heart)
   │   └── Duration: 0f (instant)
   ├── Material: "Mat_Health" (màu hồng)
   └── Heart-beat pulse animation
```

### **5.3 Convert to Prefabs**
```
1. Drag từng ItemPickup vào Assets/Prefabs/Items/
2. Delete khỏi scene (sẽ spawn bằng code)
3. Tạo folder structure:
   Assets/Prefabs/Items/
   ├── ItemPickup_Magnet.prefab
   ├── ItemPickup_Speed.prefab
   ├── ItemPickup_Invisible.prefab
   └── ItemPickup_Health.prefab
```

---

## 🖥️ **PHẦN 6: UI SETUP**

### **6.1 Canvas Configuration**
```
1. Canvas Settings:
   ├── Render Mode: Screen Space - Camera
   ├── Render Camera: Main Camera
   ├── Plane Distance: 1
   ├── Canvas Scaler:
   │   ├── UI Scale Mode: Scale With Screen Size
   │   ├── Reference Resolution: (1920, 1080)
   │   ├── Screen Match Mode: Match Width Or Height
   │   └── Match: 0.5
```

### **6.2 ItemEffect UI Panel**
```
1. Canvas → Create Empty: "Panel_ItemEffects"
   ├── Anchor: Top-Right
   ├── Position: (-200, -50, 0)
   ├── Size: (300, 400)

2. Add Components:
   ├── Image: Background (semi-transparent black)
   ├── Vertical Layout Group:
   │   ├── Spacing: 5
   │   ├── Padding: (10, 10, 10, 10)
   │   └── Child Force Expand: Width ✓
   └── Content Size Fitter: Vertical Fit: Preferred Size

3. Add Script: ItemEffectUI
```

### **6.3 Health UI**
```
1. Canvas → Create Empty: "Panel_Health"
   ├── Anchor: Top-Left
   ├── Position: (50, -50, 0)

2. Add HealthUI Component
3. Heart Sprite: Tạo simple heart texture hoặc dùng default UI sprite
```

### **6.4 Debug UI Panel**
```
1. Canvas → Create Empty: "Panel_Debug"
   ├── Anchor: Bottom-Left
   ├── Add Text components cho:
   │   ├── FPS Counter
   │   ├── Active Effects Count
   │   ├── Performance Metrics
   │   └── Current Effect Values
```

---

## 🎮 **PHẦN 7: INPUT SYSTEM**

### **7.1 Input Action Asset**
```
1. Assets → Create → Input Actions
   ├── Rename: "GameInput"
   ├── Add Action Map: "Gameplay"
   ├── Add Actions:
   │   ├── Movement (Vector2): WASD, Arrow Keys
   │   ├── Jump (Button): Space, W, Up Arrow
   │   ├── Slide (Button): S, Down Arrow
   │   ├── UseManualItem (Button): E, Enter
   │   └── Debug Actions:
   │       ├── SpawnMagnet: 1
   │       ├── SpawnSpeed: 2  
   │       ├── SpawnInvisible: 3
   │       └── SpawnHealth: 4
```

### **7.2 Input Handler Component**
```
1. RunnerPlayer → Add Component: PlayerInput
   ├── Actions: GameInput Asset
   ├── Default Map: Gameplay
   └── Behavior: Send Messages

2. Add Script: InputHandler
   ├── Link các Input Actions
   └── Forward tới RunnerController
```

---

## 🧪 **PHẦN 8: DEBUG & TESTING TOOLS**

### **8.1 Debug Item Spawner**
```csharp
// Tạo Script: DebugItemSpawner.cs
public class DebugItemSpawner : MonoBehaviour
{
    [Header("Item Prefabs")]
    public GameObject magnetPrefab;
    public GameObject speedPrefab;
    public GameObject invisiblePrefab;
    public GameObject healthPrefab;
    
    [Header("Spawn Settings")]
    public Transform player;
    public Vector3 spawnOffset = Vector3.forward * 2f;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SpawnItem(magnetPrefab);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SpawnItem(speedPrefab);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SpawnItem(invisiblePrefab);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SpawnItem(healthPrefab);
    }
    
    void SpawnItem(GameObject prefab)
    {
        if (prefab && player)
        {
            Vector3 spawnPos = player.position + spawnOffset;
            Instantiate(prefab, spawnPos, Quaternion.identity);
        }
    }
}
```

### **8.2 Performance Monitor**
```csharp
// Tạo Script: PerformanceMonitor.cs  
public class PerformanceMonitor : MonoBehaviour
{
    [Header("Display")]
    public Text fpsText;
    public Text effectCountText;
    public Text performanceText;
    
    private float frameCount = 0;
    private float dt = 0.0f;
    private float fps = 0.0f;
    
    void Update()
    {
        frameCount++;
        dt += Time.unscaledDeltaTime;
        
        if (dt > 1.0f)
        {
            fps = frameCount / dt;
            frameCount = 0;
            dt -= 1.0f;
            UpdateDisplay();
        }
    }
    
    void UpdateDisplay()
    {
        if (fpsText) fpsText.text = $"FPS: {fps:F1}";
        
        var effects = ItemEffectSystem.Instance;
        if (effectCountText && effects)
            effectCountText.text = $"Effects: {effects.ActiveEffectCount}";
    }
}
```

### **8.3 Auto-Test Sequence**
```csharp
// Tạo Script: AutoTestSequence.cs
public class AutoTestSequence : MonoBehaviour
{
    public float testInterval = 2f;
    public bool enableAutoTest = true;
    
    private DebugItemSpawner spawner;
    
    void Start()
    {
        spawner = FindObjectOfType<DebugItemSpawner>();
        if (enableAutoTest && spawner)
        {
            StartCoroutine(RunAutoTest());
        }
    }
    
    IEnumerator RunAutoTest()
    {
        yield return new WaitForSeconds(1f);
        
        // Test sequence: Magnet → Speed → Invisible → Health
        var items = new[] { 
            spawner.magnetPrefab, 
            spawner.speedPrefab, 
            spawner.invisiblePrefab, 
            spawner.healthPrefab 
        };
        
        foreach (var item in items)
        {
            spawner.SpawnItem(item);
            yield return new WaitForSeconds(testInterval);
        }
        
        Debug.Log("Auto-test sequence completed!");
    }
}
```

---

## 📊 **PHẦN 9: VALIDATION CHECKLIST**

### **9.1 Scene Validation**
```
✅ Checklist Setup:
├── 📷 Camera follows player smoothly
├── 🏃 Player moves with WASD/Arrow keys
├── ⚡ ItemEffectSystem singleton initializes
├── 🎮 Input system responds correctly
├── 🖥️ UI panels visible và positioned correctly
├── 🔊 Audio system initialized (no errors)
├── 💡 Lighting looks good
└── 📱 No console errors on Play

✅ Performance Targets:
├── FPS: >60 trong empty scene
├── FPS: >30 với 10+ active effects
├── Memory: <100MB allocated
├── Draw Calls: <50 per frame
└── Batching: >80% efficiency
```

### **9.2 ItemEffect Testing**
```
✅ Manual Test Cases:
1. Press '1' → Magnet item spawns → Player picks up → Blue icon appears → Expires after 10s
2. Press '2' → Speed item spawns → Player faster → Red icon + timer → Stack test
3. Press '3' → Invisible item spawns → Player transparent → Purple icon → Collision disabled
4. Press '4' → Health item spawns → Instant pickup → Health +1 → Pink flash effect

✅ Automated Test Results:
├── All effects apply correctly: PASS/FAIL
├── Stacking rules work: PASS/FAIL
├── UI updates real-time: PASS/FAIL
├── Performance <0.1ms overhead: PASS/FAIL
└── No memory leaks after 100+ pickups: PASS/FAIL
```

---

## 🚀 **PHẦN 10: BUILD & DEPLOY TEST**

### **10.1 Build Configuration**
```
1. File → Build Settings:
   ├── Platform: PC, Mac & Linux Standalone
   ├── Architecture: x86_64
   ├── Scenes to Build:
   │   ├── 0: Boot (nếu có)
   │   └── 1: Test_Phase4_ItemEffect
   └── Player Settings:
       ├── Resolution: Windowed (1920x1080)
       ├── Splash Screen: Unity Logo (fast)
       └── Script Backend: IL2CPP

2. Development Build Options:
   ├── ✅ Development Build
   ├── ✅ Script Debugging
   ├── ✅ Deep Profiling Support
   └── ✅ Wait for Managed Debugger (if needed)
```

### **10.2 Final Verification**
```
1. Build thành công ✅
2. Executable runs without crashes ✅
3. All ItemEffect features working ✅
4. Performance meets targets ✅
5. UI responsive và readable ✅
6. Audio plays correctly ✅
7. Save/Load system functional ✅
8. Ready for Phase 4 development ✅
```

---

## 📝 **TROUBLESHOOTING COMMON ISSUES**

### **Issues & Solutions:**

**"ItemEffectSystem not found"**
```
→ Kiểm tra _Systems GameObject có ItemEffectSystem component
→ Đảm bảo script không có compile errors
→ Check singleton pattern đã implement đúng
```

**"UI không hiển thị effects"**
```
→ Verify Canvas Render Camera = Main Camera
→ Check ItemEffectUI component subscribed events
→ Ensure UI Panel active và visible
```

**"Performance giảm khi nhiều effects"**
```
→ Enable SRP Batcher trong Graphics Settings
→ Check Object Pooling cho particles/UI elements
→ Profile bằng Unity Profiler Window
```

**"Audio không play"**
```
→ Kiểm tra AudioSource components setup
→ Verify Audio Listener trên Main Camera
→ Check volume levels và AudioMixer setup
```

---

## 🎯 **NEXT STEPS**

Sau khi hoàn thành setup:

1. **Phase 4.1**: Implement Enhanced Particle Systems
2. **Phase 4.2**: Add Advanced Audio System  
3. **Phase 4.3**: Create Save/Load System
4. **Phase 4.4**: Performance optimization và testing
5. **Phase 4.5**: Final polish và documentation

---

**🎊 Setup hoàn tất! Scene test sẵn sàng cho Phase 4 development.**

**📧 Contact**: Báo cáo issues hoặc questions qua project documentation.

---

*Tài liệu được tạo bởi AI Assistant cho VibeCodeTutorial Phase 4*
