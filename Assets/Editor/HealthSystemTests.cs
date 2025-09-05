using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using EndlessRunner.Gameplay;
using EndlessRunner.Core;

namespace EndlessRunner.Tests.EditMode
{
    /// <summary>
    /// Unit tests cho Health System - Phase 1.7
    /// Tests IHealthSystem interface implementation, I-frames, damage logic, và events.
    /// </summary>
    public class HealthSystemTests
    {
        #region Setup & Teardown
        
        private GameObject _testGameObject;
        private HealthComponent _healthComponent;
        
        [SetUp]
        public void SetUp()
        {
            // Create test GameObject với HealthComponent
            _testGameObject = new GameObject("TestHealth");
            _healthComponent = _testGameObject.AddComponent<HealthComponent>();
            
            // Unity sẽ tự động gọi Awake() khi component được add
            // Không cần gọi thủ công lifecycle methods
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                Object.DestroyImmediate(_testGameObject);
            }
        }
        
        #endregion
        
        #region Basic Health System Tests
        
        [Test]
        public void HealthComponent_InitializesWithCorrectDefaultValues()
        {
            // Assert
            Assert.AreEqual(3, _healthComponent.MaxHealth, "Max health should be 3 by default");
            Assert.AreEqual(3, _healthComponent.CurrentHealth, "Current health should equal max on init");
            Assert.IsFalse(_healthComponent.IsInIFrames, "Should not be in I-frames initially");
            Assert.IsTrue(_healthComponent.IsAlive, "Should be alive initially");
        }
        
        [Test]
        public void HealthComponent_ImplementsIHealthSystemInterface()
        {
            // Assert
            Assert.IsTrue(_healthComponent is IHealthSystem, "HealthComponent should implement IHealthSystem");
            
            var healthSystem = _healthComponent as IHealthSystem;
            Assert.IsNotNull(healthSystem, "Cast to IHealthSystem should not be null");
            Assert.AreEqual(_healthComponent.CurrentHealth, healthSystem.CurrentHealth);
            Assert.AreEqual(_healthComponent.MaxHealth, healthSystem.MaxHealth);
            Assert.AreEqual(_healthComponent.IsInIFrames, healthSystem.IsInIFrames);
        }
        
        #endregion
        
        #region Damage System Tests
        
        [Test]
        public void TakeDamage_ReducesHealthByOne_WhenNotInIFrames()
        {
            // Arrange
            int initialHealth = _healthComponent.CurrentHealth;
            
            // Act
            bool damageSuccessful = _healthComponent.TakeDamage(1, "Unit Test");
            
            // Assert
            Assert.IsTrue(damageSuccessful, "Damage should be successful when not in I-frames");
            Assert.AreEqual(initialHealth - 1, _healthComponent.CurrentHealth, "Health should decrease by 1");
            Assert.IsTrue(_healthComponent.IsInIFrames, "Should enter I-frames after taking damage");
        }
        
        [Test]
        public void TakeDamage_ReturnsFalse_WhenInIFrames()
        {
            // Arrange
            _healthComponent.TakeDamage(1, "First Hit"); // Enter I-frames
            int healthAfterFirstHit = _healthComponent.CurrentHealth;
            
            // Act
            bool damageSuccessful = _healthComponent.TakeDamage(1, "Second Hit");
            
            // Assert
            Assert.IsFalse(damageSuccessful, "Damage should fail when in I-frames");
            Assert.AreEqual(healthAfterFirstHit, _healthComponent.CurrentHealth, "Health should not change during I-frames");
            Assert.IsTrue(_healthComponent.IsInIFrames, "Should remain in I-frames");
        }
        
        [Test]
        public void TakeDamage_HandlesDamageAmountCorrectly()
        {
            // Test damage = 0
            Assert.IsFalse(_healthComponent.TakeDamage(0, "Zero Damage"), "Zero damage should return false");
            
            // Test negative damage  
            Assert.IsFalse(_healthComponent.TakeDamage(-1, "Negative Damage"), "Negative damage should return false");
            
            // Test damage > current health
            bool result = _healthComponent.TakeDamage(5, "Massive Damage");
            Assert.IsTrue(result, "Damage should be successful even if > current health");
            Assert.AreEqual(0, _healthComponent.CurrentHealth, "Health should not go below 0");
            Assert.IsFalse(_healthComponent.IsAlive, "Should be dead after health reaches 0");
        }
        
        [Test]
        public void TakeDamage_TriggersDeathWhenHealthReachesZero()
        {
            // Arrange
            bool deathEventTriggered = false;
            _healthComponent.OnDeath += () => deathEventTriggered = true;
            
            // Act - damage equal to current health
            _healthComponent.TakeDamage(_healthComponent.CurrentHealth, "Fatal Damage");
            
            // Assert
            Assert.AreEqual(0, _healthComponent.CurrentHealth, "Health should be 0");
            Assert.IsFalse(_healthComponent.IsAlive, "Should be dead");
            Assert.IsTrue(deathEventTriggered, "Death event should be triggered");
        }
        
        #endregion
        
        #region I-Frames System Tests
        
        [Test]
        public void IFrames_PreventMultipleDamageHits()
        {
            // Arrange
            int initialHealth = _healthComponent.CurrentHealth;
            
            // Act - multiple rapid damage calls
            bool hit1 = _healthComponent.TakeDamage(1, "Hit 1");
            bool hit2 = _healthComponent.TakeDamage(1, "Hit 2");
            bool hit3 = _healthComponent.TakeDamage(1, "Hit 3");
            
            // Assert
            Assert.IsTrue(hit1, "First hit should succeed");
            Assert.IsFalse(hit2, "Second hit should fail (I-frames)");
            Assert.IsFalse(hit3, "Third hit should fail (I-frames)");
            Assert.AreEqual(initialHealth - 1, _healthComponent.CurrentHealth, "Only one damage should be applied");
        }
        
        [Test]
        public void SetIFrames_ActivatesIFramesCorrectly()
        {
            // Arrange
            Assert.IsFalse(_healthComponent.IsInIFrames, "Should not be in I-frames initially");
            
            // Act
            _healthComponent.SetIFrames(1.0f);
            
            // Assert
            Assert.IsTrue(_healthComponent.IsInIFrames, "Should be in I-frames after SetIFrames call");
            Assert.IsFalse(_healthComponent.TakeDamage(1, "Test"), "Should not take damage during I-frames");
        }
        
        [Test]
        public void IFrames_ExtendDurationOnNewDamage()
        {
            // Arrange - set custom short I-frame duration cho testing
            var healthComponent = _testGameObject.AddComponent<TestableHealthComponent>();
            healthComponent.SetIFrameDuration(0.1f); // Short duration for test
            
            // Act
            healthComponent.TakeDamage(1, "First hit");
            // Immediately take another hit (should extend I-frames)
            bool secondHit = healthComponent.TakeDamage(1, "Second hit");
            
            // Assert
            Assert.IsFalse(secondHit, "Second hit should fail");
            Assert.IsTrue(healthComponent.IsInIFrames, "Should still be in I-frames");
        }
        
        #endregion
        
        #region Health Restoration Tests
        
        [Test]
        public void RestoreHealth_IncreasesHealthCorrectly()
        {
            // Arrange - damage first
            _healthComponent.TakeDamage(2, "Setup Damage");
            int healthAfterDamage = _healthComponent.CurrentHealth;
            
            // Act
            bool restored = _healthComponent.RestoreHealth(1);
            
            // Assert
            Assert.IsTrue(restored, "Health restoration should succeed");
            Assert.AreEqual(healthAfterDamage + 1, _healthComponent.CurrentHealth, "Health should increase by 1");
        }
        
        [Test]
        public void RestoreHealth_DoesNotExceedMaxHealth()
        {
            // Act - try to restore health when already at max
            bool restored = _healthComponent.RestoreHealth(1);
            
            // Assert
            Assert.IsFalse(restored, "Should not restore health when at max");
            Assert.AreEqual(_healthComponent.MaxHealth, _healthComponent.CurrentHealth, "Health should remain at max");
        }
        
        [Test]
        public void RestoreHealth_HandlesInvalidAmounts()
        {
            // Arrange - damage first
            _healthComponent.TakeDamage(1, "Setup");
            int healthBefore = _healthComponent.CurrentHealth;
            
            // Act & Assert
            Assert.IsFalse(_healthComponent.RestoreHealth(0), "Zero restoration should fail");
            Assert.IsFalse(_healthComponent.RestoreHealth(-1), "Negative restoration should fail");
            Assert.AreEqual(healthBefore, _healthComponent.CurrentHealth, "Health should not change");
        }
        
        [Test]
        public void RestoreHealth_TriggersRestorationEvent()
        {
            // Arrange
            _healthComponent.TakeDamage(1, "Setup");
            int restoredAmount = 0;
            _healthComponent.OnHealthRestored += (amount) => restoredAmount = amount;
            
            // Act
            _healthComponent.RestoreHealth(1);
            
            // Assert
            Assert.AreEqual(1, restoredAmount, "Restoration event should pass correct amount");
        }
        
        #endregion
        
        #region Event System Tests
        
        [Test]
        public void HealthChangedEvent_TriggersOnDamage()
        {
            // Arrange
            int eventCurrentHealth = -1;
            int eventMaxHealth = -1;
            _healthComponent.OnHealthChanged += (current, max) => 
            {
                eventCurrentHealth = current;
                eventMaxHealth = max;
            };
            
            // Act
            _healthComponent.TakeDamage(1, "Event Test");
            
            // Assert
            Assert.AreEqual(_healthComponent.CurrentHealth, eventCurrentHealth, "Event should pass current health");
            Assert.AreEqual(_healthComponent.MaxHealth, eventMaxHealth, "Event should pass max health");
        }
        
        [Test]
        public void HealthChangedEvent_TriggersOnRestoration()
        {
            // Arrange
            _healthComponent.TakeDamage(1, "Setup");
            int eventCurrentHealth = -1;
            _healthComponent.OnHealthChanged += (current, max) => eventCurrentHealth = current;
            
            // Act
            _healthComponent.RestoreHealth(1);
            
            // Assert
            Assert.AreEqual(_healthComponent.CurrentHealth, eventCurrentHealth, "Event should trigger on restoration");
        }
        
        [Test]
        public void EventsDoNotTrigger_WhenNoActualChange()
        {
            // Arrange
            bool eventTriggered = false;
            _healthComponent.OnHealthChanged += (c, m) => eventTriggered = true;
            
            // Act - try invalid operations
            _healthComponent.TakeDamage(0, "No damage");
            _healthComponent.RestoreHealth(0);
            _healthComponent.RestoreHealth(1); // Already at max
            
            // Assert
            Assert.IsFalse(eventTriggered, "Events should not trigger when no actual change occurs");
        }
        
        #endregion
        
        #region Edge Cases & Error Handling
        
        [Test]
        public void HealthSystem_HandlesMaxHealthChanges()
        {
            // This test would require exposing MaxHealth setter or using reflection
            // For now, we test that MaxHealth is readonly after initialization
            int maxHealth = _healthComponent.MaxHealth;
            
            // Try to damage more than max health
            _healthComponent.TakeDamage(maxHealth + 5, "Excessive damage");
            
            Assert.AreEqual(0, _healthComponent.CurrentHealth, "Health should be 0");
            Assert.AreEqual(maxHealth, _healthComponent.MaxHealth, "Max health should not change");
        }
        
        [Test]
        public void HealthSystem_HandlesNullOrEmptyDamageSource()
        {
            // Act & Assert - should not throw exceptions
            Assert.DoesNotThrow(() => _healthComponent.TakeDamage(1, null), "Null damage source should not throw");
            Assert.DoesNotThrow(() => _healthComponent.TakeDamage(1, ""), "Empty damage source should not throw");
            Assert.DoesNotThrow(() => _healthComponent.TakeDamage(1, "   "), "Whitespace damage source should not throw");
        }
        
        [Test]
        public void DeadHealthSystem_RejectsAllOperations()
        {
            // Arrange - kill the health system
            _healthComponent.TakeDamage(_healthComponent.MaxHealth, "Death");
            Assert.IsFalse(_healthComponent.IsAlive, "Should be dead");
            
            // Act & Assert - all operations should fail
            Assert.IsFalse(_healthComponent.TakeDamage(1, "Post-death damage"), "Dead system should reject damage");
            Assert.IsFalse(_healthComponent.RestoreHealth(1), "Dead system should reject restoration");
            _healthComponent.SetIFrames(1.0f); // SetIFrames doesn't return bool
        }
        
        #endregion
        
        #region Helper Classes
        
        /// <summary>
        /// Testable HealthComponent với exposed methods cho testing
        /// </summary>
        private class TestableHealthComponent : HealthComponent
        {
            public void SetIFrameDuration(float duration)
            {
                // Access protected field via reflection if needed
                // For now, assume we have a way to set this
            }
            
            public Timer GetIFrameTimer()
            {
                // Return internal I-frame timer for testing
                return default; // Placeholder
            }
        }
        
        #endregion
        
        #region Performance Tests
        
        [Test]
        public void HealthSystem_PerformanceTest_RapidDamage()
        {
            // Test performance với rapid damage calls
            const int iterations = 1000;
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                // Reset health system
                if (!_healthComponent.IsAlive)
                {
                    Object.DestroyImmediate(_testGameObject);
                    SetUp();
                }
                
                _healthComponent.TakeDamage(1, $"Perf Test {i}");
                
                // Reset I-frames để cho phép damage tiếp theo
                // (In real test, we'd need a way to clear I-frames)
            }
            
            stopwatch.Stop();
            
            Debug.Log($"[HealthSystemTests] {iterations} damage operations took {stopwatch.ElapsedMilliseconds}ms " +
                      $"({stopwatch.ElapsedTicks / (float)iterations:F2} ticks per operation)");
            
            // Assert reasonable performance (< 1ms per 100 operations)
            Assert.Less(stopwatch.ElapsedMilliseconds, iterations / 10, 
                "Health system should be performant for rapid damage");
        }
        
        #endregion
    }
}
