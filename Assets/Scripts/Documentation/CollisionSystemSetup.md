# Collision System Setup Guide

## Overview
CollisionDetector system provides collision detection between the player and game objects (obstacles and pickups) in the Endless Runner game. This document explains how to set up and use the system.

## Architecture

### Components
- **CollisionDetector**: Main component that handles collision detection and routing
- **CollisionEventData**: Data structure containing collision information
- **IHealthSystem**: Interface for health management (implemented by HealthComponent)
- **IRunnerController**: Interface for player controller integration

### Key Features
- Layer-based collision filtering
- Collision cooldown to prevent spam
- Distance-based optimization
- Event-driven architecture
- Debug tools and runtime monitoring
- Integration with health and controller systems

## Setup Instructions

### Step 1: Create Unity Layers
1. Go to **Edit → Project Settings → Tags and Layers**
2. Create the following layers:
   - `Obstacle` (suggested: Layer 8)
   - `Pickup` (suggested: Layer 10)

### Step 2: Setup Player GameObject
1. Select your Player GameObject in the hierarchy
2. Add required components:
   ```csharp
   // Required components for CollisionDetector
   - Collider (set as Trigger)
   - HealthComponent (implements IHealthSystem)
   - CollisionDetector
   ```
3. Or use the menu: **EndlessRunner → Collision → Add Collision Detector**

### Step 3: Configure CollisionDetector
In the CollisionDetector Inspector:

#### Layer Configuration
- **Obstacle Layer**: Set to the layer mask containing your obstacle layer
- **Pickup Layer**: Set to the layer mask containing your pickup layer

#### Collision Settings
- **Collision Cooldown**: Time between obstacle collisions (default: 0.1s)
- **Max Detection Distance**: Maximum range for collision detection (default: 3.0)

#### Debug Settings
- **Debug Mode**: Enable logging and debug features
- **Show Gizmos**: Draw debug gizmos in Scene view

### Step 4: Setup Game Objects

#### Obstacle Objects
```csharp
// Example obstacle setup
GameObject obstacle = new GameObject("Obstacle");
obstacle.layer = LayerMask.NameToLayer("Obstacle");

BoxCollider collider = obstacle.AddComponent<BoxCollider>();
collider.isTrigger = true;
collider.size = Vector3.one;

// Add visual representation
MeshRenderer renderer = obstacle.AddComponent<MeshRenderer>();
MeshFilter filter = obstacle.AddComponent<MeshFilter>();
filter.mesh = // Your obstacle mesh
renderer.material = // Your obstacle material
```

#### Pickup Objects
```csharp
// Example pickup setup
GameObject pickup = new GameObject("Pickup");
pickup.layer = LayerMask.NameToLayer("Pickup");

SphereCollider collider = pickup.AddComponent<SphereCollider>();
collider.isTrigger = true;
collider.radius = 0.5f;

// Add visual representation and pickup behavior
```

### Step 5: Integration with Existing Systems

#### Health System Integration
The CollisionDetector automatically integrates with IHealthSystem:
```csharp
// Obstacle collision automatically calls:
healthSystem.TakeDamage(1, "Obstacle_" + obstacleName);
```

#### Runner Controller Integration
Integration with IRunnerController for hit reactions:
```csharp
// On obstacle hit, triggers:
runnerController.PerformAction(RunnerAction.Hit);
```

#### Speed Manager Integration
Gets current speed for collision event data:
```csharp
// Automatically finds and uses ISpeedManager for relative speed calculation
```

## Usage Examples

### Basic Event Handling
```csharp
public class GameController : MonoBehaviour
{
    private CollisionDetector collisionDetector;
    
    void Start()
    {
        collisionDetector = FindObjectOfType<CollisionDetector>();
        
        // Subscribe to collision events
        collisionDetector.OnObstacleHit += HandleObstacleHit;
        collisionDetector.OnPickupCollected += HandlePickupCollected;
        collisionDetector.OnAnyCollision += HandleAnyCollision;
    }
    
    private void HandleObstacleHit(CollisionEventData data)
    {
        Debug.Log($"Player hit obstacle: {data.gameObject.name}");
        // Play hit effect, update UI, etc.
    }
    
    private void HandlePickupCollected(CollisionEventData data)
    {
        Debug.Log($"Player collected pickup: {data.gameObject.name}");
        // Update score, play sound, etc.
    }
    
    private void HandleAnyCollision(CollisionEventData data)
    {
        Debug.Log($"Collision at {data.collisionPoint} with {data.gameObject.name}");
        // General collision handling
    }
    
    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (collisionDetector != null)
        {
            collisionDetector.OnObstacleHit -= HandleObstacleHit;
            collisionDetector.OnPickupCollected -= HandlePickupCollected;
            collisionDetector.OnAnyCollision -= HandleAnyCollision;
        }
    }
}
```

### Advanced Usage
```csharp
public class AdvancedCollisionHandler : MonoBehaviour
{
    private CollisionDetector collisionDetector;
    private ParticleSystem hitEffect;
    private AudioSource audioSource;
    
    void Start()
    {
        collisionDetector = GetComponent<CollisionDetector>();
        hitEffect = GetComponentInChildren<ParticleSystem>();
        audioSource = GetComponent<AudioSource>();
        
        collisionDetector.OnObstacleHit += OnObstacleHit;
    }
    
    private void OnObstacleHit(CollisionEventData data)
    {
        // Play hit effect at collision point
        if (hitEffect != null)
        {
            hitEffect.transform.position = data.collisionPoint;
            hitEffect.Play();
        }
        
        // Play hit sound with pitch variation based on speed
        if (audioSource != null)
        {
            float pitch = 1f + (data.relativeSpeed / 20f);
            audioSource.pitch = Mathf.Clamp(pitch, 0.8f, 1.5f);
            audioSource.Play();
        }
        
        // Camera shake based on collision impact
        float shakeIntensity = Mathf.Clamp01(data.relativeSpeed / 15f);
        CameraShake.Instance.Shake(0.3f, shakeIntensity);
        
        // Debug information
        Debug.Log($"Obstacle hit details:\n{GetCollisionInfo(data)}");
    }
    
    private string GetCollisionInfo(CollisionEventData data)
    {
        return $"Object: {data.gameObject.name}\n" +
               $"Layer: {LayerMask.LayerToName(data.layer)}\n" +
               $"Position: {data.collisionPoint}\n" +
               $"Speed: {data.relativeSpeed:F1}\n" +
               $"Time: {data.timestamp:F2}";
    }
}
```

## Debug Tools

### Runtime Monitoring
During play mode, the CollisionDetector Inspector shows:
- **Obstacle Hits**: Total number of obstacle collisions
- **Pickups Collected**: Total number of pickup collisions
- **In Cooldown**: Current cooldown status
- **Debug Controls**: Force collisions, reset stats, show detailed info

### Debug Methods
```csharp
// Force an obstacle collision for testing
collisionDetector.ForceObstacleCollision("TestObstacle");

// Reset statistics
collisionDetector.ResetStats();

// Get debug information
string debugInfo = collisionDetector.GetDebugStats();
Debug.Log(debugInfo);
```

### Scene Gizmos
Enable "Show Gizmos" to see:
- Yellow circle: Detection range
- Red cube: Obstacle detection indicator
- Green cube: Pickup detection indicator

## Testing

### Unit Tests
Run the collision system tests:
1. Open **Window → General → Test Runner**
2. Switch to **EditMode** tab
3. Run **CollisionSystemTests**

### Manual Testing Checklist
- [ ] Player collides with obstacles and takes damage
- [ ] Player collects pickups without taking damage
- [ ] Collision cooldown prevents rapid hits
- [ ] Distance filtering works correctly
- [ ] Events fire properly
- [ ] Debug tools function correctly
- [ ] Integration with health system works
- [ ] Integration with runner controller works

## Troubleshooting

### Common Issues

#### No Collisions Detected
- **Check**: Collider is set as Trigger
- **Check**: Correct layers assigned to objects
- **Check**: Layer masks configured in CollisionDetector
- **Check**: Objects are within detection distance

#### Rapid Damage/Multiple Hits
- **Solution**: Increase collision cooldown
- **Check**: Objects don't have multiple overlapping colliders

#### Performance Issues
- **Solution**: Reduce max detection distance
- **Solution**: Disable debug mode in production
- **Check**: Too many objects with colliders in scene

#### Events Not Firing
- **Check**: Event subscriptions are set up correctly
- **Check**: Components implement required interfaces
- **Solution**: Subscribe to OnAnyCollision to debug all collisions

### Debug Information
Use these console commands for debugging:
```csharp
// In console or debug script:
FindObjectOfType<CollisionDetector>().GetDebugStats();
FindObjectOfType<CollisionDetector>().ForceObstacleCollision("DebugTest");
```

## Performance Considerations

### Optimization Tips
1. **Layer Filtering**: Use specific layer masks to avoid unnecessary checks
2. **Detection Distance**: Set appropriate max detection distance
3. **Collider Complexity**: Use simple colliders (Box, Sphere) for better performance
4. **Object Pooling**: Pool collision objects to reduce instantiation overhead
5. **Debug Mode**: Disable in production builds

### Memory Management
- Events are automatically cleared on component disable
- Use proper event subscription/unsubscription patterns
- Avoid holding references to CollisionEventData longer than necessary

## Integration with Other Systems

### Phase 2: Item System
```csharp
// Example of future pickup integration
private void HandlePickupCollected(CollisionEventData data)
{
    // Get pickup component
    var pickup = data.gameObject.GetComponent<IPickup>();
    if (pickup != null)
    {
        // Apply pickup effects
        pickup.OnCollected(playerController);
    }
}
```

### UI Integration
```csharp
// Example UI feedback
private void HandleObstacleHit(CollisionEventData data)
{
    // Flash screen red
    UIManager.Instance.FlashDamage();
    
    // Show floating damage text
    DamageTextSpawner.Instance.SpawnDamageText("1", data.collisionPoint);
    
    // Update health UI
    UIManager.Instance.UpdateHealthDisplay();
}
```

## Future Enhancements
- Collision damage variation based on obstacle type
- Multiple damage per obstacle support
- Collision force calculation for physics effects
- Custom collision shapes and detection patterns
- Network synchronization support for multiplayer
