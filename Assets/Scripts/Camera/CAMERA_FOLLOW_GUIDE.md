# Camera Follow System - Hướng dẫn sử dụng

## 📋 Tổng quan
Camera Follow System cung cấp 2 scripts để camera theo dõi player trong endless runner game:
- **SimpleCameraFollow**: Đơn giản, dễ setup
- **CameraFollow**: Đầy đủ tính năng, có constraints và debug tools

## 🚀 Setup nhanh - SimpleCameraFollow

### Bước 1: Thêm Script
1. Select **Main Camera** trong Hierarchy
2. **Add Component** → Search "SimpleCameraFollow"
3. Script sẽ tự động tìm Player với tag "Player"

### Bước 2: Cấu hình
```
Target: (auto-detected Player)
Offset: (0, 4, -5)         # Camera ở phía sau và cao hơn player
Follow Speed: 8            # Tốc độ smooth follow
Look At Player: ✓          # Camera luôn nhìn về player
Fixed Rotation: (20, 0, 0) # Rotation cố định nếu không look at player
```

### Bước 3: Test
- Chạy game và di chuyển player
- Camera sẽ follow smooth với offset được set
- Sử dụng Context Menu "Snap to Player" để test instant positioning

## ⚙️ Setup nâng cao - CameraFollow

### Bước 1: Thêm Script  
1. Select **Main Camera**
2. **Add Component** → Search "CameraFollow"
3. Configure settings theo nhu cầu

### Bước 2: Cấu hình chi tiết

#### Target Settings
- **Target**: Player transform (auto-detect nếu null)

#### Position Settings
- **Offset**: Khoảng cách từ camera đến player `(0, 4, -5)`
- **Follow Speed**: Tốc độ follow (1-10) `5`

#### Rotation Settings  
- **Look At Target**: Camera tự động nhìn về player
- **Rotation Speed**: Tốc độ rotation smoothing `3`
- **Fixed Rotation**: Rotation cố định nếu không look at target

#### Constraints (Tùy chọn)
- **Constrain X**: Giới hạn movement theo trục X
- **X Constraint**: Min/Max position X `(-5, 5)`
- **Constrain Y**: Giới hạn movement theo trục Y  
- **Y Constraint**: Min/Max position Y `(1, 10)`

#### Smoothing
- **Use Smooth Damping**: Dùng SmoothDamp thay vì Lerp
- **Damp Time**: Thời gian smooth damping `0.3`

#### Debug
- **Debug Mode**: Enable debug logs
- **Show Gizmos**: Hiển thị gizmos trong Scene view

### Bước 3: Runtime API
```csharp
// Get component
var cameraFollow = Camera.main.GetComponent<CameraFollow>();

// Change settings runtime
cameraFollow.SetTarget(newPlayerTransform);
cameraFollow.SetOffset(new Vector3(0, 6, -8));
cameraFollow.SetFollowSpeed(10f);

// Instant snap
cameraFollow.SnapToTarget();

// Reset to defaults
cameraFollow.ResetSettings();
```

## 🎯 Use Cases

### Endless Runner (Recommended)
```
SimpleCameraFollow:
- Offset: (0, 4, -5)
- Follow Speed: 8
- Look At Player: ✓
```

### Third Person Adventure
```
CameraFollow:
- Offset: (0, 2, -3)
- Follow Speed: 5
- Constraints: Enable Y constraint (1, 8)
- Smooth Damping: ✓
```

### Cinematic Following
```
CameraFollow:
- Offset: (-2, 3, -4)
- Follow Speed: 2
- Look At Target: ✓
- Rotation Speed: 1
```

## 🔧 Troubleshooting

### Camera không follow
✅ **Giải pháp:**
- Kiểm tra Player có tag "Player"
- Assign Target manually trong Inspector
- Kiểm tra Follow Speed > 0

### Camera giật cục
✅ **Giải pháp:**
- Sử dụng **Use Smooth Damping** = true
- Tăng **Damp Time** lên 0.5-1.0
- Giảm **Follow Speed** xuống 3-5

### Camera không nhìn đúng hướng
✅ **Giải pháp:**
- Enable **Look At Target**
- Điều chỉnh **Rotation Speed**
- Kiểm tra **Fixed Rotation** nếu không dùng look at

### Performance issues
✅ **Giải pháp:**
- Disable **Debug Mode** và **Show Gizmos** 
- Sử dụng **SimpleCameraFollow** thay vì **CameraFollow**
- Giảm **Follow Speed** và **Rotation Speed**

## 🎮 Context Menu Commands

### SimpleCameraFollow
- **Snap to Player**: Instant teleport đến vị trí player

### CameraFollow  
- **Snap to Target**: Instant teleport đến target
- **Reset Settings**: Reset về cấu hình mặc định

## 🎨 Gizmos Debug

Khi **Show Gizmos** = true:
- **Yellow line**: Kết nối camera với target
- **Green cube**: Vị trí target + offset
- **Red lines**: X constraints (nếu enable)
- **Blue lines**: Y constraints (nếu enable)  
- **Cyan ray**: Look direction (nếu look at target)

## 💡 Tips & Best Practices

### Performance
- Sử dụng **SimpleCameraFollow** cho mobile/VR
- **CameraFollow** cho desktop với đầy đủ tính năng
- Disable debug features trong build production

### Smooth Movement
- **Smooth Damping** cho movement tự nhiên
- **Lerp** cho movement có control tốt hơn
- **Damp Time** 0.2-0.5 cho responsive
- **Damp Time** 0.5-1.0 cho cinematic

### Constraints Usage
- **X Constraint** để giữ camera trong bounds level
- **Y Constraint** để tránh camera quá thấp/cao
- **Look At Target** cho third-person games
- **Fixed Rotation** cho side-scrollers

## 🔗 Integration với hệ thống khác

### Với Cinemachine (Alternative)
```csharp
// Nếu muốn dùng Cinemachine thay thế
// GameObject → Cinemachine → Virtual Camera
// - Follow: Player Transform  
// - Look At: Player Transform
// - Body: 3rd Person Follow
```

### Với RunnerController
```csharp
// Camera có thể integrate với speed changes
public class CameraSpeedSync : MonoBehaviour
{
    private CameraFollow cameraFollow;
    private ISpeedManager speedManager;
    
    void Start()
    {
        cameraFollow = GetComponent<CameraFollow>();
        speedManager = FindObjectOfType<SpeedManager>();
    }
    
    void Update()
    {
        if (speedManager != null)
        {
            float speedMultiplier = speedManager.CurrentSpeed / 10f;
            cameraFollow.SetFollowSpeed(5f * speedMultiplier);
        }
    }
}
```

---

## ✅ Kết luận

Camera Follow System cung cấp giải pháp hoàn chỉnh cho camera tracking trong Unity:

- **SimpleCameraFollow**: Setup 2 phút, hoạt động ngay
- **CameraFollow**: Full control, debug tools, production ready
- **Auto-detection**: Tự tìm player, ít configuration
- **Runtime API**: Thay đổi settings trong game
- **Performance optimized**: Smooth 60+ FPS

**Namespace đã fix hoàn toàn - scripts compile clean và ready to use!** 🎯
