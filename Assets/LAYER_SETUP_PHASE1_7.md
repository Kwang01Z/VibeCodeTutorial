# Layer Setup Requirements - Phase 1.7 Collision & Health System

**Version:** 1.0  
**Last Updated:** 2025-09-03  
**Status:** Required for Phase 1.7 Implementation  

---

## 🎯 **Collision Layer Architecture**

### **Required Layers:**

| Layer Name | Layer Index | Purpose | Components |
|------------|-------------|---------|------------|
| `Default` | 0 | Ground, Environment | Ground planes, walls, static environment |
| `Player` | 8 | Runner character | Player GameObject với HealthComponent, CollisionDetector |
| `Obstacle` | 9 | Collision obstacles | Spawned obstacles từ ObstacleSpawner |
| `Pickup` | 10 | Items & collectibles | Future Phase 2 - coins, power-ups |
| `PlayerIFrame` | 11 | Player trong I-frames | Layer switch để ignore collision |

### **Physics Collision Matrix:**

```
           Default  Player  Obstacle  Pickup  PlayerIFrame
Default      ✓       ✓        ✓        ✓        ✓
Player       ✓       ❌       ✓        ✓        ❌
Obstacle     ✓       ✓        ❌       ❌       ❌
Pickup       ✓       ✓        ❌       ❌       ❌
PlayerIFrame ✓       ❌       ❌       ❌       ❌
```

**Logic:**
- ✓ = Collision enabled
- ❌ = Collision disabled
- **Player ↔ Obstacle**: Trigger collision để detect damage
- **Player ↔ Pickup**: Trigger collision để collect items
- **PlayerIFrame**: Immune to all obstacles và pickups
- **Obstacle ↔ Obstacle**: Disabled để tránh physics conflicts

---

## 🔧 **Component Requirements**

### **Player GameObject Setup:**

```
Player (GameObject)
├── Transform (position, rotation, scale)
├── Rigidbody (Kinematic=false, Freeze Rot XYZ, Freeze Pos Z)
├── Collider (IsTrigger=true, Layer="Player")
├── HealthComponent (MaxHealth=3, IFrameDuration=1.2s)
├── CollisionDetector (ObstacleLayer=Obstacle, PickupLayer=Pickup)
├── RunnerController (State machine & physics)
├── LaneController (Lane movement)
├── InputHandler (Input processing)
└── SpeedManager (Speed progression)
```

### **Obstacle Prefab Setup:**

```
ObstaclePrefab (GameObject)
├── Transform 
├── Collider (IsTrigger=true, Layer="Obstacle")
├── MeshRenderer + MeshFilter (3D model)
└── ObstacleComponent (Optional: damage amount, effects)
```

### **Ground/Environment Setup:**

```
Ground (GameObject)
├── Transform
├── Collider (IsTrigger=false, Layer="Default")
└── MeshRenderer + MeshFilter
```

---

## 📋 **Setup Checklist**

### **Manual Steps (Unity Editor):**

1. **Create Layers:**
   - [ ] Open `Edit > Project Settings > Tags and Layers`
   - [ ] Add new layers:
     - Layer 8: `Player`
     - Layer 9: `Obstacle` 
     - Layer 10: `Pickup`
     - Layer 11: `PlayerIFrame`

2. **Configure Physics Matrix:**
   - [ ] Open `Edit > Project Settings > Physics`
   - [ ] Set collision matrix theo table trên
   - [ ] Verify: Player ✓ Obstacle, Player ✓ Pickup
   - [ ] Verify: PlayerIFrame ❌ Obstacle, PlayerIFrame ❌ Pickup

3. **Update Prefabs:**
   - [ ] Player GameObject: Set layer = "Player"
   - [ ] All Obstacle prefabs: Set layer = "Obstacle" 
   - [ ] All Pickup prefabs: Set layer = "Pickup"
   - [ ] Ground/Environment: Set layer = "Default"

4. **Configure Components:**
   - [ ] Player Rigidbody: Kinematic=false, Freeze Rotation XYZ, Freeze Position Z
   - [ ] Player Collider: IsTrigger=true
   - [ ] Obstacle Colliders: IsTrigger=true
   - [ ] CollisionDetector: ObstacleLayer mask = Obstacle (layer 9)
   - [ ] CollisionDetector: PickupLayer mask = Pickup (layer 10)

---

## 🧪 **Validation & Testing**

### **Component Validation Script:**

```csharp
// Auto-validate setup trong Editor
public class LayerValidation
{
    [MenuItem("EndlessRunner/Validate Layer Setup")]
    public static void ValidateLayers()
    {
        // Check layer names exist
        // Check physics matrix settings  
        // Check prefab layer assignments
        // Check component configurations
    }
}
```

### **Runtime Testing:**

1. **Collision Detection Test:**
   - [ ] Player hits obstacle → Health giảm → I-frames active
   - [ ] During I-frames: Player immune to obstacles
   - [ ] After I-frames: Player vulnerable again

2. **Physics Integration Test:**
   - [ ] Player falls on ground (layer collision)
   - [ ] Player moves through air (no unwanted collisions)
   - [ ] Obstacles don't interfere với nhau

3. **Performance Test:**
   - [ ] 15+ obstacles spawned: No physics lag
   - [ ] Collision events: 0 allocation per frame
   - [ ] Layer switching: Smooth transitions

---

## ⚠️ **Common Issues & Solutions**

### **Issue: Collision không hoạt động**
**Solutions:**
- Check layer assignments on both Player và Obstacle
- Verify Physics Matrix settings
- Ensure Colliders are set as Trigger
- Check CollisionDetector layer masks

### **Issue: Double damage từ same obstacle**
**Solutions:**  
- Verify collision cooldown trong CollisionDetector
- Check I-frames implementation trong HealthComponent
- Ensure obstacles use correct layer

### **Issue: Player falls through ground**
**Solutions:**
- Ground layer = "Default", not "Obstacle"
- Ground Collider IsTrigger = false
- Player Rigidbody UseGravity = true

### **Issue: Performance lag với nhiều obstacles** 
**Solutions:**
- Verify Obstacle ↔ Obstacle collisions disabled
- Use Object Pooling cho obstacle spawning
- Limit collision detection distance

---

## 🚀 **Integration Notes**

### **với ObstacleSpawner:**
- ChunkDefinition spawns obstacles với Layer "Obstacle"
- ObstacleInfo validates collider setup
- Pooling system preserves layer assignments

### **với HealthComponent:**
- TakeDamage checks I-frames trước khi apply damage
- I-frames implementation sử dụng timer
- Events fired cho UI updates

### **với RunnerController:**
- Hit state triggers knockback physics
- I-frames state changes collision behavior  
- Layer switching cho immunity (future feature)

---

## ✅ **Success Criteria**

**Phase 1.7 Ready khi:**
- [ ] Tất cả required layers created
- [ ] Physics matrix configured correctly
- [ ] Player prefab setup với all required components
- [ ] Obstacle prefabs setup với correct layers
- [ ] Collision detection hoạt động: damage + I-frames
- [ ] Performance stable với 15+ obstacles
- [ ] Zero compile errors
- [ ] Basic validation tests pass

**Validation Command:** `EndlessRunner/Validate Layer Setup`

---

**Next Phase:** Phase 2.1 sẽ extend Layer system cho Items, Currency, và Effects.
