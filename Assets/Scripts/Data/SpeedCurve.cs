using UnityEngine;

namespace EndlessRunner.Data
{
    /// <summary>
    /// ScriptableObject định nghĩa speed curve cho game progression
    /// Tính speed dựa trên distance run với smooth acceleration
    /// </summary>
    [CreateAssetMenu(fileName = "New Speed Curve", menuName = "EndlessRunner/Speed Curve")]
    public class SpeedCurve : ScriptableObject
    {
        [Header("Speed Settings")]
        [SerializeField] private float _baseSpeed = 8f;
        [SerializeField] private float _maxSpeed = 20f;
        [SerializeField] private float _accelerationSmoothness = 2f;
        
        [Header("Speed Progression")]
        [SerializeField] private AnimationCurve _speedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private float _distanceScale = 1000f; // Per km
        
        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogs = false;
        
        #region Properties
        
        public float BaseSpeed => _baseSpeed;
        public float MaxSpeed => _maxSpeed;
        public float AccelerationSmoothness => _accelerationSmoothness;
        public float DistanceScale => _distanceScale;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Evaluate speed dựa trên distance đã chạy
        /// </summary>
        /// <param name="distance">Distance in meters</param>
        /// <returns>Target speed tại distance này</returns>
        public float EvaluateSpeed(float distance)
        {
            if (distance < 0f) return _baseSpeed;
            
            // Normalize distance theo scale (default: per km)
            float normalizedDistance = distance / _distanceScale;
            
            // Evaluate curve value (0-1)
            float curveValue = _speedCurve.Evaluate(normalizedDistance);
            
            // Convert to actual speed
            float speedRange = _maxSpeed - _baseSpeed;
            float targetSpeed = _baseSpeed + (curveValue * speedRange);
            
            // Clamp result
            targetSpeed = Mathf.Clamp(targetSpeed, _baseSpeed, _maxSpeed);
            
            if (_enableDebugLogs)
            {
                Debug.Log($"[SpeedCurve] Distance: {distance:F1}m, Normalized: {normalizedDistance:F3}, " +
                         $"Curve: {curveValue:F3}, Speed: {targetSpeed:F1}");
            }
            
            return targetSpeed;
        }
        
        /// <summary>
        /// Get speed at specific percentage of progression (0-1)
        /// </summary>
        /// <param name="progress">Progress 0-1</param>
        /// <returns>Speed at progress point</returns>
        public float EvaluateSpeedAtProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            float curveValue = _speedCurve.Evaluate(progress);
            float speedRange = _maxSpeed - _baseSpeed;
            return _baseSpeed + (curveValue * speedRange);
        }
        
        /// <summary>
        /// Get normalized curve value at distance
        /// </summary>
        /// <param name="distance">Distance in meters</param>
        /// <returns>Curve value 0-1</returns>
        public float GetCurveValueAtDistance(float distance)
        {
            float normalizedDistance = distance / _distanceScale;
            return _speedCurve.Evaluate(normalizedDistance);
        }
        
        /// <summary>
        /// Calculate distance cần để đạt target speed
        /// </summary>
        /// <param name="targetSpeed">Target speed</param>
        /// <returns>Distance needed (approximate)</returns>
        public float GetDistanceForSpeed(float targetSpeed)
        {
            targetSpeed = Mathf.Clamp(targetSpeed, _baseSpeed, _maxSpeed);
            
            // Convert speed to curve value
            float speedRange = _maxSpeed - _baseSpeed;
            float targetCurveValue = (targetSpeed - _baseSpeed) / speedRange;
            
            // Find distance by sampling curve (approximate)
            // For more accuracy, could use binary search
            for (float distance = 0f; distance <= _distanceScale * 10f; distance += 10f)
            {
                float normalizedDistance = distance / _distanceScale;
                float curveValue = _speedCurve.Evaluate(normalizedDistance);
                
                if (curveValue >= targetCurveValue)
                {
                    return distance;
                }
            }
            
            return _distanceScale * 10f; // Max search range
        }
        
        #endregion
        
        #region Validation
        
        private void OnValidate()
        {
            // Ensure valid values
            _baseSpeed = Mathf.Max(0.1f, _baseSpeed);
            _maxSpeed = Mathf.Max(_baseSpeed + 0.1f, _maxSpeed);
            _accelerationSmoothness = Mathf.Max(0.1f, _accelerationSmoothness);
            _distanceScale = Mathf.Max(1f, _distanceScale);
            
            // Ensure curve starts at 0 and is monotonic increasing
            if (_speedCurve == null || _speedCurve.keys.Length < 2)
            {
                _speedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            }
            else
            {
                // Ensure first key starts at 0
                Keyframe firstKey = _speedCurve.keys[0];
                if (firstKey.time != 0f || firstKey.value != 0f)
                {
                    Keyframe[] keys = _speedCurve.keys;
                    keys[0] = new Keyframe(0f, 0f);
                    _speedCurve.keys = keys;
                }
            }
        }
        
        #endregion
        
        #region Debug Utilities
        
        /// <summary>
        /// Get debug info về speed curve
        /// </summary>
        /// <returns>Debug string</returns>
        public string GetDebugInfo()
        {
            return $"SpeedCurve: {name}\n" +
                   $"Base Speed: {_baseSpeed:F1} m/s\n" +
                   $"Max Speed: {_maxSpeed:F1} m/s\n" +
                   $"Scale: {_distanceScale:F0}m\n" +
                   $"Smoothness: {_accelerationSmoothness:F1}\n" +
                   $"Curve Keys: {_speedCurve.keys.Length}";
        }
        
        /// <summary>
        /// Sample speed curve for debugging
        /// </summary>
        /// <param name="sampleCount">Number of samples</param>
        /// <returns>Array of (distance, speed) pairs</returns>
        public (float distance, float speed)[] SampleCurve(int sampleCount = 10)
        {
            sampleCount = Mathf.Max(2, sampleCount);
            var samples = new (float, float)[sampleCount];
            
            for (int i = 0; i < sampleCount; i++)
            {
                float progress = (float)i / (sampleCount - 1);
                float distance = progress * _distanceScale;
                float speed = EvaluateSpeed(distance);
                samples[i] = (distance, speed);
            }
            
            return samples;
        }
        
        #endregion
    }
}
