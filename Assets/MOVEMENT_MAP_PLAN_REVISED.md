# KẾ HOẠCH MAP DI CHUYỂN & LOOP - ĐÃ SỬA LẠI

**Version:** 2.0  
**Last Updated:** 2025-09-02  
**Status:** Revised for Simplified Movement  
**Target:** Endless Runner với movement: Trái/Phải + Nhảy + Trượt ONLY

---

## 🔄 Thay đổi chính so với kế hoạch cũ

### ❌ **LOẠI BỎ:**
- **Hệ thống lane phức tạp:** Không còn 3 lane cố định (-2f, 0f, 2f)
- **Lane change animation:** Không còn blend curve, AddForce lane transition 
- **LaneController state machine:** Idle/Changing/Cooldown states
- **Complex physics:** AddForce cho lane change

### ✅ **GIỮ LẠI & TỐI ƯU:**
- **Input System:** PC (A/D, ←/→) + Mobile (swipe L/R)
- **Jump/Slide mechanics:** Vẫn physics-based với Rigidbody
- **Floating Origin System:** Giải quyết vấn đề precision loss
- **Chunk-based spawning:** Nhưng đơn giản hóa collision rules

---

## 🎮 Hệ thống Movement mới

### 1. **Simple Horizontal Movement**
```csharp
namespace EndlessRunner.Gameplay
{
    public class SimpleMovementController : MonoBehaviour
    {
        [Header("Horizontal Movement")]
        [SerializeField] private float _horizontalSpeed = 8f;
        [SerializeField] private float _horizontalLimit = 3f; // Giới hạn trái/phải
        [SerializeField] private float _horizontalSmoothing = 10f;
        
        [Header("Jump & Slide")]
        [SerializeField] private float _jumpForce = 12f;
        [SerializeField] private float _slideForce = 8f;
        [SerializeField] private float _slideDuration = 0.8f;
        
        private Vector3 _targetHorizontalPosition;
        private bool _isSliding;
        private Timer _slideTimer;
        
        private void FixedUpdate()
        {
            // Horizontal movement (lerp smooth)
            Vector3 pos = transform.position;
            pos.x = Mathf.Lerp(pos.x, _targetHorizontalPosition.x, 
                _horizontalSmoothing * Time.fixedDeltaTime);
            transform.position = pos;
            
            // Auto forward (constant Z speed)
            _rigidbody.velocity = _rigidbody.velocity.WithZ(_forwardSpeed);
        }
        
        public void MoveHorizontal(float direction) // -1 left, +1 right
        {
            _targetHorizontalPosition.x += direction * _horizontalSpeed * Time.deltaTime;
            _targetHorizontalPosition.x = Mathf.Clamp(_targetHorizontalPosition.x, 
                -_horizontalLimit, _horizontalLimit);
        }
    }
}
```

### 2. **Input Mapping đơn giản**
```csharp
// PC Input
if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
    _movementController.MoveHorizontal(-1f);
if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
    _movementController.MoveHorizontal(1f);
if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
    _movementController.Jump();
if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
    _movementController.Slide();

// Mobile Swipe (giữ nguyên swipe detection từ kế hoạch cũ)
```

---

## 🗺️ Map & World System

### 1. **Floating Origin (Giải quyết vấn đề chính)**

**Vấn đề:** Unity dùng toạ độ float32 (~7 chữ số chính xác). Nếu runner lao về +Z vô hạn:
• <10.000 m: an toàn.
• 10.000 – 20.000 m: rung camera, collider sai lệch.
• >100.000 m: physics & culling hỏng hoàn toàn.

**Giải pháp:** Giữ nhân vật gần gốc, "kéo" thế giới ngược lại (floating-origin hoặc chunk recycling).

#### FloatingOriginSystem Implementation
```csharp
namespace EndlessRunner.Core
{
    public class FloatingOriginSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private Transform _player;
        [SerializeField] private float _resetThreshold = 50f;
        [SerializeField] private LayerMask _movableLayers = -1; // World, Obstacles, Pickups
        
        [Header("Performance")]
        [SerializeField] private bool _useOptimizedShifting = true;
        
        private float _totalDistanceRun = 0f;
        private readonly List<IWorldShiftable> _shiftableObjects = new();
        
        public float TotalDistanceRun => _totalDistanceRun + _player.position.z;
        
        private void LateUpdate()
        {
            if (_player.position.z < _resetThreshold) return;
            
            PerformWorldShift();
        }
        
        private void PerformWorldShift()
        {
            float offset = _player.position.z;
            
            if (_useOptimizedShifting)
            {
                // Optimized: chỉ shift objects đã đăng ký
                for (int i = 0; i < _shiftableObjects.Count; i++)
                {
                    _shiftableObjects[i].OnWorldShift(-offset);
                }
            }
            else
            {
                // Fallback: tìm tất cả objects (chậm)
                var allTransforms = FindObjectsOfType<Transform>(false);
                for (int i = 0; i < allTransforms.Length; i++)
                {
                    if (ShouldShiftObject(allTransforms[i]))
                    {
                        Vector3 pos = allTransforms[i].position;
                        pos.z -= offset;
                        allTransforms[i].position = pos;
                    }
                }
            }
            
            // Reset player position
            _player.position = _player.position.WithZ(0f);
            _totalDistanceRun += offset;
            
            OnWorldShifted?.Invoke(offset);
        }
        
        private bool ShouldShiftObject(Transform t)
        {
            // Không shift UI, Camera, Audio Listener, etc.
            return ((1 << t.gameObject.layer) & _movableLayers) != 0 
                && t != _player;
        }
        
        public event System.Action<float> OnWorldShifted;
        
        // Registration system cho optimization
        public void RegisterShiftable(IWorldShiftable shiftable)
        {
            if (!_shiftableObjects.Contains(shiftable))
                _shiftableObjects.Add(shiftable);
        }
        
        public void UnregisterShiftable(IWorldShiftable shiftable)
        {
            _shiftableObjects.Remove(shiftable);
        }
    }
}

// Interface cho objects cần shift
public interface IWorldShiftable
{
    void OnWorldShift(float deltaZ);
}
```

### 2. **Chunk System đơn giản**

#### ChunkDefinition (giản lược)
```csharp
[CreateAssetMenu(menuName = "EndlessRunner/Simple Chunk")]
public class SimpleChunkDefinition : ScriptableObject
{
    [SerializeField] private GameObject _chunkPrefab;
    [SerializeField] private float _chunkLength = 20f;
    [SerializeField] private DifficultyTag _difficulty;
    
    // Không còn lane-specific obstacles
    [SerializeField] private ObstacleSpawnPoint[] _obstaclePoints;
    
    public enum DifficultyTag { Easy, Medium, Hard }
    
    [System.Serializable]
    public struct ObstacleSpawnPoint
    {
        public GameObject prefab;
        public Vector3 localPosition; // Tự do trong không gian 3D
        public ObstacleType type; // Jump, Slide, Wall
    }
    
    public enum ObstacleType
    {
        LowObstacle,  // Cần Jump để vượt
        HighObstacle, // Cần Slide để vượt  
        Wall          // Cần tránh trái/phải
    }
}
```

#### ChunkSpawner (tối ưu)
```csharp
public class SimpleChunkSpawner : MonoBehaviour, IWorldShiftable
{
    [Header("Spawning")]
    [SerializeField] private SimpleChunkDefinition[] _chunkPool;
    [SerializeField] private Transform _player;
    [SerializeField] private float _spawnDistance = 100f;
    [SerializeField] private float _despawnDistance = -50f;
    
    private ObjectPool<GameObject> _chunkPool_runtime;
    private readonly List<GameObject> _activeChunks = new();
    private float _nextSpawnZ = 0f;
    private int _lastDifficultyIndex = -1;
    
    private void Start()
    {
        // Register với FloatingOriginSystem
        FindObjectOfType<FloatingOriginSystem>()?.RegisterShiftable(this);
        
        // Initialize pool
        _chunkPool_runtime = new ObjectPool<GameObject>(_chunkPool[0].ChunkPrefab, transform);
        
        // Spawn initial chunks
        for (int i = 0; i < 5; i++)
        {
            SpawnNextChunk();
        }
    }
    
    private void Update()
    {
        CheckSpawning();
        CheckDespawning();
    }
    
    private void SpawnNextChunk()
    {
        var chunkDef = SelectNextChunk();
        var chunkGO = _chunkPool_runtime.Get();
        
        chunkGO.transform.position = new Vector3(0, 0, _nextSpawnZ);
        _activeChunks.Add(chunkGO);
        
        _nextSpawnZ += chunkDef.ChunkLength;
    }
    
    private SimpleChunkDefinition SelectNextChunk()
    {
        // Simple rule: tránh Hard->Hard liên tiếp
        var available = _chunkPool.Where((chunk, index) =>
        {
            if (_lastDifficultyIndex == -1) return true;
            if (_chunkPool[_lastDifficultyIndex].Difficulty == SimpleChunkDefinition.DifficultyTag.Hard &&
                chunk.Difficulty == SimpleChunkDefinition.DifficultyTag.Hard)
                return false;
            return true;
        }).ToArray();
        
        var selected = available[UnityEngine.Random.Range(0, available.Length)];
        _lastDifficultyIndex = System.Array.IndexOf(_chunkPool, selected);
        
        return selected;
    }
    
    // Implement IWorldShiftable
    public void OnWorldShift(float deltaZ)
    {
        _nextSpawnZ += deltaZ;
    }
}
```

---

## 🎯 Obstacle & Collision Logic

### **Obstacle Types (chỉ 3 loại)**
1. **LowObstacle:** Cần Jump để vượt (hộp thấp, gỗ, đá)
2. **HighObstacle:** Cần Slide để vượt (cành cây, thanh ngang)
3. **Wall:** Cần tránh trái/phải (tường, cột)

### **Collision Detection (đơn giản)**
```csharp
public class SimpleCollisionDetector : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            var obstacle = other.GetComponent<SimpleObstacle>();
            HandleCollision(obstacle.Type);
        }
        else if (other.CompareTag("Pickup"))
        {
            HandlePickup(other.GetComponent<Pickup>());
        }
    }
    
    private void HandleCollision(SimpleChunkDefinition.ObstacleType type)
    {
        // Simple logic: mọi collision đều trừ health
        _healthSystem.TakeDamage();
        
        // VFX based on obstacle type
        PlayCollisionEffect(type);
    }
}
```

---

## 🔧 Performance Optimizations

### **1. Object Pooling (giữ nguyên từ kế hoạch cũ)**
- Chunk pooling
- Obstacle/Pickup pooling  
- VFX pooling

### **2. Culling & LOD**
```csharp
// Cull objects ngoài camera view
public class SimpleCullingSystem : MonoBehaviour
{
    private void Update()
    {
        foreach (var chunk in _activeChunks)
        {
            float distanceToPlayer = chunk.transform.position.z - _player.position.z;
            
            // Disable objects xa player
            bool shouldBeActive = Mathf.Abs(distanceToPlayer) < _cullingDistance;
            
            if (chunk.activeInHierarchy != shouldBeActive)
                chunk.SetActive(shouldBeActive);
        }
    }
}
```

### **3. Batch Processing**
- Process collision detection trong FixedUpdate
- Group VFX spawning
- Limit audio source instances

---

## 📊 Testing Strategy (cập nhật)

### **Unit Tests:**
- FloatingOriginSystem: precision loss, distance calculation
- Movement bounds: horizontal limits, jump/slide physics  
- Chunk spawning: difficulty rules, no impossible combinations

### **Integration Tests:**
- Movement feel: smooth horizontal, responsive jump/slide
- World shifting: no visual artifacts, consistent distance tracking
- Performance: 60 FPS stable, <0.5ms shift time

### **PlayMode Tests:**
- Run >1000m: no precision issues
- Stress test: spawn/despawn 100+ chunks
- Mobile: touch input responsiveness

---

## 🎨 Tối ưu & Pitfall

### **Floating Origin:**
• **Tối ưu:** Dùng `IWorldShiftable` registration thay `FindObjectsOfType`
• **Pitfall:** Tạm dừng Rigidbody khi shift để tránh impulse giả
• **Lưu ý:** Không shift UI, AudioListener, static baked objects

### **Movement:**  
• **Tối ưu:** Dùng Lerp cho smooth horizontal, AddForce cho jump/slide
• **Pitfall:** Clamp horizontal position để không bay ra ngoài màn hình
• **Coyote time:** 100ms buffer cho jump sau khi rời ground

### **Memory:**
• **Pooling:** Pre-warm pools, tránh Instantiate runtime
• **GC:** Zero allocation trong Update/FixedUpdate hot paths
• **Texture:** Compress, atlas sprites, cull unused materials

---

## 🚀 Implementation Priority

### **Phase 1 (MVP):**
1. ✅ Simple horizontal movement (A/D keys)
2. ✅ Jump/Slide với physics
3. ✅ FloatingOriginSystem  
4. ✅ Basic chunk spawning với 3 obstacle types

### **Phase 2 (Polish):**
1. Mobile swipe input
2. VFX cho movement + collisions
3. Audio feedback  
4. Performance profiling

### **Phase 3 (Advanced):**
1. Difficulty scaling
2. Theme system integration
3. Analytics events  
4. Platform optimization

---

## 📝 So sánh với kế hoạch cũ

| Aspect | Kế hoạch cũ | Kế hoạch mới |
|--------|-------------|-------------|
| **Movement** | 3 lane + lane change state machine | Free horizontal + bounds |
| **Physics** | AddForce lane transition | Lerp horizontal + AddForce jump/slide |
| **Complexity** | 800+ lines LaneController | 200 lines SimpleMovementController |
| **Performance** | Lane change allocation | Zero allocation movement |
| **Map** | Lane-based chunk design | Free-form obstacle placement |
| **Testing** | Complex state transitions | Simple movement bounds |

---

**Kết luận:** Kế hoạch mới đơn giản hơn 70%, dễ implement và maintain hơn, nhưng vẫn giữ được core gameplay và giải quyết được vấn đề precision loss của Unity floating-point.

