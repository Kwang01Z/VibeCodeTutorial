# Phase 4 - Quick Setup Guide: Sử dụng Assets có sẵn

**Dự án**: VibeCodeTutorial  
**Mục tiêu**: Tạo scene test nhanh chóng sử dụng Cat character và prefabs có sẵn  
**Thời gian**: ~15-20 phút  

---

## 🚀 **SETUP NHANH - BƯỚC 1: TẠO SCENE TEST**

### **1.1 Tạo Scene Mới**
```
1. File → New Scene → URP → Basic (Indoor)
2. File → Save As → "Test_ItemEffectSystem.unity"
3. Location: Assets/Scenes/Test_ItemEffectSystem.unity
4. File → Build Settings → Add Open Scenes
```

### **1.2 Import Character Model**
```
1. Kéo prefab từ:
   📁 Assets/Bundles/Characters/Cat/character.prefab
   
2. Kéo vào Hierarchy → Rename: "Player"

3. Kiểm tra components có sẵn:
   ✅ Animator (có Trash Cat controller)
   ✅ AudioSource (có jump/hit/death sounds)  
   ✅ Character script với accessories
   ✅ SkinnedMeshRenderer với materials
```

### **1.3 Thêm Physics cho Player**
```
Player GameObject:
├── Add Component → Rigidbody
│   ├── Mass: 1
│   ├── Drag: 2
│   ├── Angular Drag: 5  
│   └── Constraints: Freeze Rotation XYZ, Freeze Position Z
├── Add Component → CapsuleCollider
│   ├── Is Trigger: ✗
│   ├── Center: (0, 0.6, 0)
│   ├── Radius: 0.4
│   └── Height: 1.2
├── Layer: Player (8)
├── Tag: Player
└── Position: (0, 0.5, 0)
```

### **1.4 Thêm Game Scripts**
```
Player GameObject thêm scripts:
├── Add Component → RunnerController
├── Add Component → HealthComponent  
├── Add Component → SpeedManager
├── Add Component → CollisionDetector
└── Add Component → ItemEffectIntegrator (nếu có)
```

---

## 🏃 **BƯỚC 2: SETUP ENVIRONMENT**

### **2.1 Tạo đường chạy đơn giản**
```
1. GameObject → 3D Object → Plane
   ├── Name: "Ground"
   ├── Scale: (10, 1, 50)
   ├── Position: (0, 0, 25)
   ├── Layer: Ground

2. Tạo 3 lanes:
   GameObject → 3D Object → Cube (x3)
   ├── Name: "Lane_Left", "Lane_Center", "Lane_Right"  
   ├── Scale: (0.1, 0.2, 50)
   ├── Positions: (-2, 0.1, 25), (0, 0.1, 25), (2, 0.1, 25)
   └── Material: Tạo material vàng cho visibility
```

### **2.2 Camera theo Player**
```
1. Main Camera settings:
   ├── Position: (0, 4, -5)  
   ├── Rotation: (20, 0, 0)
   └── Follow script: Thêm simple follow camera

2. Hoặc dùng Cinemachine:
   ├── GameObject → Cinemachine → Virtual Camera
   ├── Follow: Player Transform
   ├── Look At: Player Transform  
   └── Body: 3rd Person Follow
```

---

## ⚡ **BƯỚC 3: SETUP ITEMEFFECT SYSTEM**

### **3.1 Systems GameObject**
```
1. GameObject → Create Empty → Name: "_Systems"
   
2. Add Component → ItemEffectSystem
   ├── Enable Effect Logging: ✓
   ├── Update Frequency: 0.1
   ├── Max Active Effects: 20
   └── Show Debug Info: ✓
```

### **3.2 Sử dụng Prefabs có sẵn**

**Power-ups có sẵn:**
```
Assets/Prefabs/Powerup/
├── 🧲 CoinMagnet.prefab → Item Magnet
├── ⚡ ScoreMultiplier.prefab → Item Speed  
├── 👻 Invincibilty.prefab → Item Invisible
└── ❤️ ExtraLife.prefab → Item Health
```

**Cách sử dụng:**
```
1. Kéo từng prefab vào scene ở vị trí:
   ├── CoinMagnet: (0, 1, 5)
   ├── ScoreMultiplier: (2, 1, 8)  
   ├── Invincibilty: (-2, 1, 12)
   └── ExtraLife: (0, 1, 15)

2. Mỗi prefab kiểm tra có components:
   ✅ Collider (Is Trigger)
   ✅ ItemPickup script (nếu có)
   ✅ Visual effects/particles
```

### **3.3 Particles có sẵn**
```
Assets/Prefabs/Particles/
├── 🧲 MagnetParticles.prefab
├── ⚡ MultiplierParticles.prefab  
├── 👻 InvincibilityParticles.prefab
└── ❤️ ExtraLifeParticles.prefab

→ Attach vào Player để hiển thị khi effects active
```

---

## 🖥️ **BƯỚC 4: UI SETUP**

### **4.1 Tạo UI Canvas**
```
1. GameObject → UI → Canvas
   ├── Render Mode: Screen Space - Camera
   ├── Render Camera: Main Camera
   └── Order in Layer: 0

2. Add EventSystem (tự động tạo)
```

### **4.2 Health UI (đơn giản)**
```
1. Canvas → UI → Panel → Name: "HealthPanel"
   ├── Anchor: Top-Left
   ├── Position: (100, -50, 0)
   
2. HealthPanel → UI → Text → Name: "HealthText"  
   ├── Text: "Health: 3"
   ├── Font Size: 24
   └── Color: White
```

### **4.3 Effects UI (đơn giản)**
```
1. Canvas → UI → Panel → Name: "EffectsPanel"
   ├── Anchor: Top-Right  
   ├── Position: (-150, -50, 0)
   
2. EffectsPanel → UI → Text → Name: "EffectsText"
   ├── Text: "Effects: None"
   ├── Font Size: 20
   └── Color: Yellow
```

### **4.4 Debug Info**
```
1. Canvas → UI → Panel → Name: "DebugPanel"
   ├── Anchor: Bottom-Left
   ├── Size: (300, 150)
   
2. Add Text elements:
   ├── FPS Text
   ├── Speed Text  
   ├── Position Text
   └── Effects Count Text
```

---

## 🧪 **BƯỚC 5: DEBUG TOOLS**

### **5.1 Tạo Debug Item Spawner**
```csharp
// File: Assets/Scripts/Tools/DebugItemSpawner.cs
using UnityEngine;

public class DebugItemSpawner : MonoBehaviour
{
    [Header("Prefabs có sẵn")]
    public GameObject magnetPrefab;    // CoinMagnet.prefab
    public GameObject multiplierPrefab; // ScoreMultiplier.prefab  
    public GameObject invincibilityPrefab; // Invincibilty.prefab
    public GameObject extraLifePrefab;   // ExtraLife.prefab
    
    [Header("Spawn Settings")]
    public Transform player;
    public float spawnDistance = 3f;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SpawnItem(magnetPrefab, "Magnet");
        if (Input.GetKeyDown(KeyCode.Alpha2)) SpawnItem(multiplierPrefab, "Multiplier");
        if (Input.GetKeyDown(KeyCode.Alpha3)) SpawnItem(invincibilityPrefab, "Invincibility");
        if (Input.GetKeyDown(KeyCode.Alpha4)) SpawnItem(extraLifePrefab, "ExtraLife");
        
        // Clear all items
        if (Input.GetKeyDown(KeyCode.C))
        {
            var items = FindObjectsOfType<ItemPickup>();
            foreach(var item in items)
                DestroyImmediate(item.gameObject);
        }
    }
    
    void SpawnItem(GameObject prefab, string itemName)
    {
        if (prefab && player)
        {
            Vector3 spawnPos = player.position + player.forward * spawnDistance;
            spawnPos.y = 1f; // Fixed height
            GameObject item = Instantiate(prefab, spawnPos, Quaternion.identity);
            Debug.Log($"Spawned {itemName} at {spawnPos}");
        }
        else
        {
            Debug.LogWarning($"Cannot spawn {itemName}: prefab or player missing");
        }
    }
}
```

### **5.2 Setup Debug Spawner**
```
1. _Systems GameObject → Add Component: DebugItemSpawner

2. Inspector assignments:
   ├── Player: Player GameObject Transform
   ├── Magnet Prefab: CoinMagnet.prefab
   ├── Multiplier Prefab: ScoreMultiplier.prefab
   ├── Invincibility Prefab: Invincibilty.prefab  
   └── Extra Life Prefab: ExtraLife.prefab

3. Spawn Distance: 3
```

### **5.3 Performance Monitor**
```csharp  
// File: Assets/Scripts/Tools/SimplePerformanceMonitor.cs
using UnityEngine;
using UnityEngine.UI;

public class SimplePerformanceMonitor : MonoBehaviour
{
    public Text fpsText;
    public Text effectsText;
    public Text speedText;
    
    private float deltaTime;
    
    void Update()
    {
        // FPS Calculation
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        
        if (fpsText) fpsText.text = $"FPS: {fps:F1}";
        
        // Effects count (if ItemEffectSystem exists)
        var effectSystem = ItemEffectSystem.Instance;
        if (effectsText && effectSystem)
            effectsText.text = $"Effects: {effectSystem.ActiveEffectCount}";
            
        // Speed (if player has Rigidbody)
        var player = GameObject.FindWithTag("Player");
        if (speedText && player)
        {
            var rb = player.GetComponent<Rigidbody>();
            if (rb) speedText.text = $"Speed: {rb.velocity.magnitude:F1}";
        }
    }
}
```

---

## 🎮 **BƯỚC 6: INPUT SYSTEM**

### **6.1 Simple Movement Script**
```csharp
// File: Assets/Scripts/Tools/SimplePlayerMovement.cs  
using UnityEngine;

public class SimplePlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float laneDistance = 2f;
    
    private Rigidbody rb;
    private int currentLane = 1; // 0=left, 1=center, 2=right
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    void Update()
    {
        // Lane switching
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            SwitchLane(-1);
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))  
            SwitchLane(1);
            
        // Forward movement
        if (rb)
        {
            Vector3 velocity = rb.velocity;
            velocity.z = moveSpeed;
            rb.velocity = velocity;
        }
    }
    
    void SwitchLane(int direction)
    {
        currentLane = Mathf.Clamp(currentLane + direction, 0, 2);
        float targetX = (currentLane - 1) * laneDistance; // -2, 0, 2
        
        Vector3 pos = transform.position;
        pos.x = targetX;
        transform.position = pos;
    }
}
```

### **6.2 Add Movement**
```
Player GameObject:
└── Add Component: SimplePlayerMovement
    ├── Move Speed: 8
    └── Lane Distance: 2
```

---

## 🧪 **BƯỚC 7: TESTING & VALIDATION**

### **7.1 Play Mode Test**
```
✅ Nhấn Play:
├── Player model xuất hiện (Trash Cat)
├── Character tự động chạy về phía trước
├── WASD để chuyển làn trái/phải
├── FPS hiển thị trên UI
└── No console errors

✅ Item Testing:
├── Press '1' → Magnet spawns trước mặt → Chạy vào để pickup
├── Press '2' → Multiplier spawns → Visual effect khi pickup  
├── Press '3' → Invincibility spawns → Character effect
├── Press '4' → ExtraLife spawns → Health tăng
└── Press 'C' → Clear all spawned items
```

### **7.2 Expected Results**
```
🎮 Basic Gameplay:
├── ✅ Character moves forward automatically
├── ✅ Lane switching với A/D keys
├── ✅ Camera follows character smoothly  
├── ✅ FPS >60 trong empty scene

⚡ ItemEffect System:
├── ✅ Items spawn khi nhấn hotkeys
├── ✅ Pickup triggers effects (console logs)
├── ✅ UI updates với effect status
├── ✅ Particles play khi pickup (nếu có)
├── ✅ Effects expire sau time duration
└── ✅ No memory leaks sau nhiều pickups

📊 Performance:
├── ✅ <0.1ms ItemEffectSystem overhead  
├── ✅ Smooth 60+ FPS với 5+ active effects
└── ✅ Memory stable (~50-80MB)
```

---

## 🚨 **TROUBLESHOOTING**

### **Common Issues:**

**"Character không di chuyển"**
```
→ Check Rigidbody constraints (freeze Z position)
→ Verify SimplePlayerMovement script attached
→ Debug.Log velocity trong Update để check
```

**"Items không pickup được"**  
```
→ Check Collider Is Trigger = ✓
→ Verify Layer collision matrix Player ↔ Pickup  
→ Add OnTriggerEnter debug logs
```

**"ItemEffectSystem not found"**
```
→ Ensure _Systems có ItemEffectSystem component
→ Check singleton initialization trong Awake
→ Verify script compilation (no errors)
```

**"UI không hiển thị"**
```
→ Check Canvas Render Camera = Main Camera
→ Verify Text components có proper anchoring
→ Ensure Canvas sorting order correct
```

**"Performance issues"**
```
→ Profile với Unity Profiler window
→ Check particle systems playing indefinitely
→ Verify object pooling cho items
```

---

## 🎯 **SUCCESS CRITERIA**

Scene setup thành công khi:

```
✅ BASIC FUNCTIONALITY:
├── Character model hiển thị và animation chạy
├── Movement smooth với lane switching
├── Camera follows player correctly
├── UI displays FPS và game info
└── No console errors hoặc warnings

✅ ITEMEFFECT SYSTEM:
├── All 4 item types spawn và pickup correctly
├── Effects apply và display trong UI
├── Visual/audio feedback working
├── Performance <0.1ms overhead
└── Memory stable sau extended testing

✅ READY FOR PHASE 4:
├── Scene có thể load và play immediately  
├── Debug tools responsive cho testing
├── Performance baseline established
├── All systems integrated và functional
└── Ready cho advanced features implementation
```

---

## 🚀 **NEXT STEPS**

Sau khi hoàn thành basic setup:

1. **✅ Verify all functionality working**
2. **🔧 Customize visual effects với particles có sẵn**
3. **🎵 Add sound effects từ character prefab**
4. **📊 Profile performance để establish baseline**  
5. **🚀 Ready to implement Phase 4.1: Enhanced Particle Systems**

---

**🎊 Quick Setup Complete!**

Scene test sẵn sàng cho Phase 4 development với:
- ✅ Real Cat character model với animations
- ✅ Working ItemEffectSystem 
- ✅ Debug tools cho rapid testing
- ✅ Performance monitoring
- ✅ Production-ready foundation

**Time to Phase 4.1! 🚀**
