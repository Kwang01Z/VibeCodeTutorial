using UnityEngine;
using UnityEngine.Events;
using EndlessRunner.Data;
using System.Collections.Generic;

namespace EndlessRunner.Items
{
    /// <summary>
    /// Static event system cho item effects để UI và systems khác có thể subscribe
    /// </summary>
    public static class ItemEffectEvents
    {
        #region Event Classes

        [System.Serializable]
        public class EffectStartedEvent : UnityEvent<ActiveItemEffect> { }

        [System.Serializable]
        public class EffectEndedEvent : UnityEvent<ItemType, string> { }

        [System.Serializable]
        public class EffectUpdatedEvent : UnityEvent<ActiveItemEffect> { }

        [System.Serializable]
        public class AllEffectsEvent : UnityEvent<Dictionary<string, ActiveItemEffect>> { }

        [System.Serializable]
        public class EffectStackedEvent : UnityEvent<ActiveItemEffect, int> { } // Effect và previous stack count

        #endregion

        #region Events

        /// <summary>
        /// Fired khi một effect mới được activate
        /// </summary>
        public static EffectStartedEvent OnEffectStarted { get; private set; } = new EffectStartedEvent();

        /// <summary>
        /// Fired khi một effect expires hoặc bị remove
        /// </summary>
        public static EffectEndedEvent OnEffectEnded { get; private set; } = new EffectEndedEvent();

        /// <summary>
        /// Fired mỗi frame cho effect có timer (để update UI countdown)
        /// </summary>
        public static EffectUpdatedEvent OnEffectUpdated { get; private set; } = new EffectUpdatedEvent();

        /// <summary>
        /// Fired khi toàn bộ active effects list thay đổi
        /// </summary>
        public static AllEffectsEvent OnAllEffectsChanged { get; private set; } = new AllEffectsEvent();

        /// <summary>
        /// Fired khi một effect được stack (duplicate item picked up)
        /// </summary>
        public static EffectStackedEvent OnEffectStacked { get; private set; } = new EffectStackedEvent();

        #endregion

        #region Public Methods

        /// <summary>
        /// Clears all event subscriptions (useful for cleanup)
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            OnEffectStarted?.RemoveAllListeners();
            OnEffectEnded?.RemoveAllListeners();
            OnEffectUpdated?.RemoveAllListeners();
            OnAllEffectsChanged?.RemoveAllListeners();
            OnEffectStacked?.RemoveAllListeners();
            
            Debug.Log("[ItemEffectEvents] All subscriptions cleared");
        }

        /// <summary>
        /// Gets subscription count for debugging
        /// </summary>
        public static string GetSubscriptionInfo()
        {
            return $"Effect Events Subscriptions:\n" +
                   $"- Started: {OnEffectStarted?.GetPersistentEventCount() ?? 0}\n" +
                   $"- Ended: {OnEffectEnded?.GetPersistentEventCount() ?? 0}\n" +
                   $"- Updated: {OnEffectUpdated?.GetPersistentEventCount() ?? 0}\n" +
                   $"- All Changed: {OnAllEffectsChanged?.GetPersistentEventCount() ?? 0}\n" +
                   $"- Stacked: {OnEffectStacked?.GetPersistentEventCount() ?? 0}";
        }

        #endregion

        #region Internal Methods (used by ItemEffectSystem)

        internal static void InvokeEffectStarted(ActiveItemEffect effect)
        {
            OnEffectStarted?.Invoke(effect);
        }

        internal static void InvokeEffectEnded(ItemType itemType, string itemId)
        {
            OnEffectEnded?.Invoke(itemType, itemId);
        }

        internal static void InvokeEffectUpdated(ActiveItemEffect effect)
        {
            OnEffectUpdated?.Invoke(effect);
        }

        internal static void InvokeAllEffectsChanged(Dictionary<string, ActiveItemEffect> activeEffects)
        {
            OnAllEffectsChanged?.Invoke(activeEffects);
        }

        internal static void InvokeEffectStacked(ActiveItemEffect effect, int previousStackCount)
        {
            OnEffectStacked?.Invoke(effect, previousStackCount);
        }

        #endregion
    }

    #region Helper Extensions

    /// <summary>
    /// Extension methods để dễ dàng subscribe/unsubscribe events
    /// </summary>
    public static class ItemEffectEventsExtensions
    {
        public static void SubscribeToEffectEvents(this MonoBehaviour component,
            UnityAction<ActiveItemEffect> onStarted = null,
            UnityAction<ItemType, string> onEnded = null,
            UnityAction<ActiveItemEffect> onUpdated = null,
            UnityAction<Dictionary<string, ActiveItemEffect>> onAllChanged = null,
            UnityAction<ActiveItemEffect, int> onStacked = null)
        {
            onStarted?.Let(ItemEffectEvents.OnEffectStarted.AddListener);
            onEnded?.Let(ItemEffectEvents.OnEffectEnded.AddListener);
            onUpdated?.Let(ItemEffectEvents.OnEffectUpdated.AddListener);
            onAllChanged?.Let(ItemEffectEvents.OnAllEffectsChanged.AddListener);
            onStacked?.Let(ItemEffectEvents.OnEffectStacked.AddListener);
        }

        public static void UnsubscribeFromEffectEvents(this MonoBehaviour component,
            UnityAction<ActiveItemEffect> onStarted = null,
            UnityAction<ItemType, string> onEnded = null,
            UnityAction<ActiveItemEffect> onUpdated = null,
            UnityAction<Dictionary<string, ActiveItemEffect>> onAllChanged = null,
            UnityAction<ActiveItemEffect, int> onStacked = null)
        {
            onStarted?.Let(ItemEffectEvents.OnEffectStarted.RemoveListener);
            onEnded?.Let(ItemEffectEvents.OnEffectEnded.RemoveListener);
            onUpdated?.Let(ItemEffectEvents.OnEffectUpdated.RemoveListener);
            onAllChanged?.Let(ItemEffectEvents.OnAllEffectsChanged.RemoveListener);
            onStacked?.Let(ItemEffectEvents.OnEffectStacked.RemoveListener);
        }

        // Helper method for null-safe action execution
        private static void Let<T>(this T obj, System.Action<T> action)
        {
            if (obj != null) action?.Invoke(obj);
        }
    }

    #endregion
}
