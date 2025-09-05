# Phase 1.6 - ObstacleSpawner & Pooling System - Setup Guide

**Status:** ✅ COMPLETED  
**Date:** 2025-09-03  
**Dependencies:** Phase 1.1-1.5 (Foundation, LaneSystem, InputHandler, RunnerController, SpeedManager)

## 📋 Components Hoàn Thành

### 1. Core System Files
- ✅ `Scripts/Data/SpawnTypes.cs` - DifficultyTag enum & ObstacleInfo struct
- ✅ `Scripts/Core/RandomUtility.cs` - XorShift128 PRNG cho determinism
- ✅ `Scripts/Data/ChunkDefinition.cs` - ScriptableObject với validation
- ✅ `Scripts/Gameplay/ChunkAnchor.cs` - Component quản lý chunk instance
- ✅ `Scripts/Gameplay/ObstacleSpawner.cs` - Main spawner system
- ✅ `Scripts/Core/ObjectPool.cs` - Generic pooling system (từ Phase 1.1)

### 2. Editor Tools
- ✅ `Scripts/Editor/ChunkDefinitionEditor.cs` - Custom Inspector với validation display
- ✅ `Scripts/Editor/ObstacleSpawnerEditor.cs` - Runtime debugging tools
- ✅ Menu: Assets > Create > EndlessRunner > Chunk Definition

### 3. Testing Framework
- ✅ `Tests/Editor/ObstacleSpawnerTests.cs` - Comprehensive unit tests
  - Spawn rules validation (no Hard→Hard)
  - Deterministic spawning
  - Cross-platform consistency
  - Pool functionality
  - Weighted difficulty progression

## 🚀 Setup Instructions

### Step 1: Project Validation
1. Đảm bảo Unity 2022.3+ LTS với URP
2. Package dependencies:
   - Input System
   - Addressables (cho future phases)
   - Test Framework (NUnit)

### Step 2: Tạo Layers
Unity Layer Settings → tạo layer "Obstacle" (Layer 8)

### Step 3: Tạo Test Chunks
1. **Create ChunkDefinition Assets:**
   ```
   Assets > Create > EndlessRunner > Chunk Definition
   ```

2. **Tạo 3 test chunks:**
   - **EasyChunk_01:**
     - Length: 20m
     - Difficulty: Easy  
     - Spawn Weight: 1.0
     - Unlock Distance: 0m
     - Obstacles: 0-1 simple obstacles

   - **MediumChunk_01:**
     - Length: 25m
     - Difficulty: Medium
     - Spawn Weight: 1.0  
     - Unlock Distance: 100m
     - Obstacles: 2-3 obstacles

   - **HardChunk_01:**
     - Length: 30m
     - Difficulty: Hard
     - Spawn Weight: 0.5
     - Unlock Distance: 500m
     - Obstacles: 3-4 complex obstacles

### Step 4: Tạo Chunk Prefabs
1. **Base ChunkAnchor Prefab:**
   ```
   GameObject > 3D Object > Cube (for floor)
   - Scale: (6, 0.1, 20)
   - Add ChunkAnchor component
   - Save as Prefab: "ChunkAnchorBase"
   ```

2. **Obstacle Prefabs:**
   ```
   GameObject > 3D Object > Cube
   - Scale: (1, 2, 1)  
   - Layer: "Obstacle"
   - Add Collider (Trigger)
   - Save as Prefab: "SimpleObstacle"
   ```

3. **Assign Prefabs:**
   - Set ChunkDefinition.ChunkPrefab = ChunkAnchorBase prefab
   - Set ObstacleInfo.prefab = SimpleObstacle prefab

### Step 5: Scene Setup
1. **Create ObstacleSpawner GameObject:**
   ```
   GameObject > Create Empty > "ObstacleSpawner"
   - Add ObstacleSpawner component
   - Assign Available Chunks array (3 chunks từ Step 3)
   - Player Transform: auto-detect SimpleRunnerController
   ```

2. **Configure Settings:**
   ```
   Spawn Distance: 60m
   Despawn Distance: 30m
   Prewarm Count: 10
   Seed: 12345 (or any uint)
   Debug Mode: ✓ Enabled
   ```

## 🔧 Testing & Validation

### Unit Tests (Window > General > Test Runner)
```bash
# Editor Tests
- PickNextChunk_NeverSpawnsConsecutiveHardChunks ✅
- DeterministicSpawning_SameSeedProducesSameSequence ✅ 
- CrossPlatformDeterminism_XorShift128Consistency ✅
- ChunkDefinition_ValidationWorksCorrectly ✅
- SpawnRules_RespectsUnlockDistance ✅
- ObjectPool_PrewarmingAndReuse ✅
- WeightedDifficultyProgression_IncreaseOverDistance ✅
```

### Runtime Testing
1. **Play Mode:**
   - Start game → ObstacleSpawner auto-initializes
   - Move player forward → chunks spawn ahead, despawn behind
   - Check Inspector stats realtime

2. **Debug Controls (Inspector):**
   - "Reset Spawner" - reset system
   - "Force Easy/Medium/Hard Chunk" - test specific spawning
   - Progress bars cho active chunks & spawn distance
   - Seed control cho determinism testing

3. **Performance Validation:**
   - Profiler: GC.Alloc ≈ 0 trong spawning hot path
   - Memory: Pool size stable, không leak
   - FPS: Stable 60+ với 10+ active chunks

## 🎯 Key Features

### 1. Deterministic Spawning
```csharp
// Same seed = same chunk sequence across platforms
spawner.SetSeed(42);
// Generates identical sequence Editor vs IL2CPP build
```

### 2. Spawn Rules
```csharp  
// Hard chunks never spawn consecutively
// Unlock distance progression
// Weighted difficulty by distance curve
```

### 3. Performance Optimized
```csharp
// Object pooling cho zero Instantiate/Destroy
// Pre-allocated collections
// No LINQ trong hot paths
// Event-driven architecture
```

### 4. Comprehensive Validation
```csharp
// ChunkDefinition validation:
// - Obstacle conflicts detection  
// - Density warnings
// - Difficulty consistency
// - Bounds checking
```

## 🚨 Troubleshooting

### Common Issues:

1. **"Chunk Prefab phải có ChunkAnchor component"**
   - Ensure chunk prefabs have ChunkAnchor script attached

2. **"Không tìm thấy player transform"**
   - ObstacleSpawner auto-finds SimpleRunnerController
   - Manually assign if needed

3. **"Pool returned null chunk"**
   - Check prewarm count > 0
   - Verify chunk prefab setup

4. **Hard chunks spawning consecutively (test fails)**
   - Check CanSpawnAfter() logic
   - Verify last difficulty tracking

### Debug Gizmos:
- Green sphere: Spawn distance ahead
- Red sphere: Despawn distance behind  
- Yellow cube: Next spawn position
- Cyan wireframes: Active chunks

## 📈 Performance Benchmarks

### Target Metrics:
- **GC Allocation:** 0 bytes/frame trong spawning
- **Memory Usage:** < 50MB cho chunk system
- **FPS Impact:** < 1% với 15 active chunks
- **Pool Efficiency:** > 95% reuse rate

### Measurement Tools:
- Unity Profiler (CPU & Memory)
- Frame Debugger
- Memory Profiler
- Custom Editor runtime stats

## 🔄 Integration với Phases khác

### Dependencies Required:
- ✅ Phase 1.1: Foundation (Timer, ObjectPool)
- ✅ Phase 1.2: LaneSystem (LaneConfig)  
- ✅ Phase 1.3: InputHandler (SimpleRunnerController detection)
- ✅ Phase 1.5: SpeedManager (ISpeedManager interface)

### Ready for Next Phases:
- Phase 1.7: Collision System (ChunkAnchor provides obstacles)
- Phase 2.1: ItemSystem (chunks có thể chứa item pickups)
- Phase 3.1: ThemeSystem (ChunkDefinition supports theme-specific chunks)

## 💡 Advanced Usage

### Custom Chunk Creation:
```csharp
// Editor scripting để tạo chunks từ scene layout
// Custom difficulty curves
// Procedural obstacle placement
```

### Seed-based Replay System:
```csharp
// Store seed per run → reproducible gameplay
// Cross-platform deterministic recording
// Debug specific sequences
```

### Performance Scaling:
```csharp
// Dynamic pool sizing based on performance
// LOD system cho distant chunks
// Async chunk setup với UniTask
```

---

**Next Phase:** 1.7 - Collision & Health System  
**Estimate:** Phase 1.6 fully functional và tested ✅  
**Status:** Ready for production use trong MVP build
