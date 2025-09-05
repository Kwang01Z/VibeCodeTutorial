using UnityEngine;

namespace EndlessRunner.Core
{
    /// <summary>
    /// Component animation đơn giản cho test pickups
    /// Thực hiện rotation và bob effect
    /// </summary>
    public class TestPickupAnimation : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _rotationSpeed = 90f;
        [SerializeField] private float _bobSpeed = 2f;
        [SerializeField] private float _bobHeight = 0.3f;
        
        private Vector3 _initialPosition;
        private float _bobTimer;
        
        private void Awake()
        {
            _initialPosition = transform.position;
            _bobTimer = 0f;
        }
        
        private void Update()
        {
            // Rotation animation
            transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
            
            // Bob animation
            _bobTimer += Time.deltaTime;
            var newY = _initialPosition.y + Mathf.Sin(_bobTimer * _bobSpeed) * _bobHeight;
            transform.position = new Vector3(_initialPosition.x, newY, _initialPosition.z);
        }
        
        private void OnDisable()
        {
            // Reset position when disabled
            transform.position = _initialPosition;
            _bobTimer = 0f;
        }
    }
}
