# Health UI Setup Guide - Phase 1.7

## Overview
Health UI system hiển thị số tim của player trên HUD thông qua heart icons. Hệ thống tự động đồng bộ với HealthComponent và cung cấp animations cũng như visual feedback.

## Components Developed
- ✅ **HealthUI.cs** - Main UI controller
- ⏳ **Heart Sprites** - Full/Empty heart assets
- ⏳ **Canvas Setup** - UI hierarchy setup
- ⏳ **Prefab Creation** - HealthHUD prefab

---

## 1. Required Assets

### Heart Sprites
Cần tạo 2 sprites cho heart system:

**Specifications:**
- **Full Heart**: Sprite hiển thị khi player còn health
- **Empty Heart**: Sprite hiển thị khi player mất health  
- **Size**: 64x64 pixels (để rõ nét ở 40px UI size)
- **Format**: PNG với transparent background
- **Style**: Simple, readable icons phù hợp game style

**Tạm thời có thể dùng Unity's Built-in UI sprites:**
- Full Heart: `UI/Skin/Knob.psd` (tint màu đỏ)
- Empty Heart: `UI/Skin/Knob.psd` (tint màu xám)

---

## 2. Canvas Setup

### UI Hierarchy Structure
```
Canvas (Screen Space - Overlay)
├── SafeArea
│   └── HUD
│       ├── HealthUI (HealthUI component)
│       │   └── HeartsContainer (HorizontalLayoutGroup)
│       │       ├── Heart_00 (Image) - auto-generated
│       │       ├── Heart_01 (Image) - auto-generated  
│       │       └── Heart_02 (Image) - auto-generated
│       └── [Other HUD elements...]
```

### Canvas Settings
```
Canvas:
- Render Mode: Screen Space - Overlay
- Pixel Perfect: ✓ (for crisp UI)
- Sort Order: 100

Canvas Scaler:
- UI Scale Mode: Scale With Screen Size  
- Reference Resolution: 1920x1080
- Screen Match Mode: Match Width Or Height
- Match: 0.5 (balance width/height)
```

### Safe Area (Mobile Support)
```
SafeArea (RectTransform):
- Anchor: Full stretch (0,0,1,1)
- Offset: (0,0,0,0)
- Components: SafeAreaHandler (optional)
```

### HUD Container
```
HUD (RectTransform):
- Anchor: Top-Left corner
- Position: (50, -50) từ top-left
- Size: Auto-fit children
```

---

## 3. HealthUI GameObject Setup

### Step-by-Step Setup:

#### 3.1 Create HealthUI GameObject
```
1. Right-click HUD → Create Empty → "HealthUI"
2. Add HealthUI component
3. Configure HealthUI properties trong Inspector
```

#### 3.2 HealthUI Component Configuration
```csharp
Heart Configuration:
- Full Heart Sprite: [Drag heart sprite]
- Empty Heart Sprite: [Drag empty heart sprite]  
- Max Hearts: 3 (theo design Phase 1.7)
- Heart Size: 40px
- Heart Spacing: 5px

Layout:
- Hearts Container: (auto-set to self)
- Auto Create Hearts: ✓

Animation:
- Enable Animations: ✓
- Animation Duration: 0.3s
- Critical Pulse Scale: 1.2x

Colors:
- Normal Color: White (1,1,1,1)
- Critical Color: Red (1,0.3,0.3,1)  
- Empty Color: Gray (0.5,0.5,0.5,0.8)

Debug:
- Debug Mode: ✓ (for development)
- Show Debug Text: ✗ (unless needed)
```

#### 3.3 Position HealthUI
```
HealthUI RectTransform:
- Anchor: Top-Left
- Anchor Position: (0, 1) 
- Pivot: (0, 1)
- Position: (20, -20) - khoảng cách từ góc màn hình
- Size: Auto-fit (based on heart count)
```

---

## 4. Auto-Generation Features

### Hearts Container
HealthUI tự động tạo **HorizontalLayoutGroup** nếu chưa có:
```csharp
Properties:
- Child Force Expand: None
- Child Control Size: None  
- Child Scale: None
- Spacing: theo _heartSpacing setting
```

### Heart Images  
Component sẽ tự động generate heart GameObjects:
```csharp
Generated Structure:
Heart_00, Heart_01, Heart_02...
- Image component với sprite
- RectTransform với size từ _heartSize
- Appropriate colors và states
```

---

## 5. Integration với Health System

### Automatic Detection
HealthUI tự động tìm **HealthComponent** trong scene qua `FindObjectOfType<HealthComponent>()`.

### Event Subscription
```csharp
Events được subscribe:
- OnHealthChanged: Update heart display
- OnDeath: Clear all hearts + stop animations
- OnHealthRestored: Restore hearts + optional animation
```

### Manual Assignment (Optional)
```csharp
// Nếu cần assign manually
healthUI.SetHealthSystem(customHealthComponent);
```

---

## 6. Animation Features

### Heart Loss Animation
- Scale effect khi mất tim
- Duration: configurable (default 0.3s)
- Curve: smooth pulse out

### Critical Health Pulse  
- Pulse animation khi còn 1 tim
- Color change sang critical red
- Scale multiplier: configurable (default 1.2x)

### Smooth Updates
- Chỉ update khi health thực sự thay đổi
- Avoid redundant calls

---

## 7. Testing & Validation

### Runtime Testing
```csharp
// Context Menu options (chỉ Play Mode):
[Right-click HealthUI component]
- "Test Health Loss" - Giảm 1 health
- "Test Health Restore" - Tăng 1 health  
- "Refresh Hearts" - Force refresh display
```

### Debug Information
```csharp
// Debug console logs (nếu Debug Mode enabled):
[HealthUI] Found HealthSystem: Player
[HealthUI] Initialized 3 hearts for max health 3
[HealthUI] Health changed: 2/3
[HealthUI] Display updated: 2/3
```

### Validation Points
- ✓ Hearts hiển thị đúng số lượng max health
- ✓ Hearts update khi health thay đổi
- ✓ Critical pulse hoạt động ở 1 tim cuối
- ✓ Colors thay đổi properly (normal/critical/empty)
- ✓ Animation mượt mà, không lag
- ✓ Layout responsive trên different screen sizes

---

## 8. Performance Considerations

### Optimizations Built-in
- **Skip redundant updates** nếu health không đổi
- **Coroutine management** cho animations
- **Object pooling** cho heart images (create once, reuse)
- **Event-driven updates** thay vì polling

### Memory Management
- Proper event unsubscription trong OnDestroy
- DestroyImmediate cho heart cleanup
- Stop coroutines khi component destroyed

---

## 9. Customization Options

### Visual Customization
```csharp
// Easy modifications:
- Heart sprites (full/empty)
- Colors (normal/critical/empty)  
- Sizes và spacing
- Max hearts displayed
- Animation settings
```

### Layout Flexibility
```csharp
// Có thể override:
- Hearts Container (custom layout)
- Layout Group settings
- Positioning và anchoring
```

### Extended Features (Future)
```csharp
// Ready for expansion:
- Fractional hearts (half hearts)
- Different heart types (shields, armor)
- Sound effects integration
- More animation types
```

---

## 10. Troubleshooting

### Common Issues

#### "No HealthComponent found"
```
Solution: Ensure HealthComponent exists trên Player GameObject trong scene
Alternative: Use SetHealthSystem() manually
```

#### Hearts không hiển thị
```
Check: 
- Heart sprites assigned?
- Canvas/HealthUI GameObject active?
- Max Hearts > 0?
- Health System có MaxHealth > 0?
```

#### Animation không mượt
```
Check:
- Enable Animations = true?
- Animation Duration hợp lý (0.1-1.0s)?
- Frame rate stable?
```

#### Layout issues
```
Check:
- HorizontalLayoutGroup settings
- RectTransform anchors
- Canvas Scaler settings
- Safe area setup (mobile)
```

---

## 11. Next Steps

### Integration Checklist
- [ ] Create heart sprites hoặc assign built-in sprites
- [ ] Setup Canvas hierarchy
- [ ] Create HealthUI prefab  
- [ ] Test integration với existing HealthComponent
- [ ] Verify responsive layout trên different resolutions
- [ ] Performance test với health spam

### Future Enhancements (Phase 2+)
- Sound effects cho health changes
- Particle effects cho heart loss/gain
- Advanced animations (bounce, glow effects)
- Multiple health types support
- Localization support

---

## File Structure
```
Assets/
├── Scripts/
│   └── UI/
│       └── HealthUI.cs ✅
├── Sprites/
│   └── UI/
│       ├── HeartFull.png (needed)
│       └── HeartEmpty.png (needed)
├── Prefabs/
│   └── UI/
│       └── HealthHUD.prefab (to create)
└── Scenes/
    └── [Current scene với Canvas setup]
```

This Health UI system provides a solid foundation cho Phase 1.7 và easily extensible cho future phases!
