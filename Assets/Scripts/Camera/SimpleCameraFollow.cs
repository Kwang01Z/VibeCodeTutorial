using UnityEngine;

namespace EndlessRunner.Camera
{
    /// <summary>
    /// Simple camera follow cho quick setup
    /// Đơn giản, ít settings, dễ sử dụng
    /// </summary>
    public class SimpleCameraFollow : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField, Tooltip("Player để follow (auto-find nếu null)")]
        private Transform target;
        
        [SerializeField, Tooltip("Khoảng cách camera từ player")]
        private Vector3 offset = new Vector3(0f, 4f, -5f);
        
        [SerializeField, Tooltip("Tốc độ follow (càng cao càng nhanh)")]
        [Range(1f, 20f)]
        private float followSpeed = 8f;
        
        [SerializeField, Tooltip("Camera có look at player không")]
        private bool lookAtPlayer = true;
        
        [Header("Optional")]
        [SerializeField, Tooltip("Rotation cố định nếu không look at player")]
        private Vector3 fixedRotation = new Vector3(20f, 0f, 0f);
        
        private void Start()
        {
            // Auto-find player nếu chưa assign
            if (target == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                    Debug.Log($"[SimpleCameraFollow] Found player: {target.name}");
                }
                else
                {
                    Debug.LogWarning("[SimpleCameraFollow] No Player found! Assign target manually.");
                }
            }
            
            // Set initial position
            if (target != null)
            {
                transform.position = target.position + offset;
                if (lookAtPlayer)
                {
                    transform.LookAt(target);
                }
                else
                {
                    transform.rotation = Quaternion.Euler(fixedRotation);
                }
            }
        }
        
        private void LateUpdate()
        {
            if (target == null) return;
            
            // Follow position
            Vector3 targetPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
            
            // Handle rotation
            if (lookAtPlayer)
            {
                Vector3 direction = (target.position - transform.position).normalized;
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, followSpeed * Time.deltaTime);
            }
            else
            {
                Quaternion targetRot = Quaternion.Euler(fixedRotation);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, followSpeed * Time.deltaTime);
            }
        }
        
        /// <summary>
        /// Snap ngay đến vị trí target (không smooth)
        /// </summary>
        [ContextMenu("Snap to Player")]
        public void SnapToPlayer()
        {
            if (target != null)
            {
                transform.position = target.position + offset;
                if (lookAtPlayer)
                {
                    transform.LookAt(target);
                }
                else
                {
                    transform.rotation = Quaternion.Euler(fixedRotation);
                }
                Debug.Log("[SimpleCameraFollow] Snapped to player position");
            }
        }
        
        /// <summary>
        /// Set target mới
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
        
        /// <summary>
        /// Set offset mới
        /// </summary>
        public void SetOffset(Vector3 newOffset)
        {
            offset = newOffset;
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (target == null) return;
            
            // Draw line connecting camera to target
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);
            
            // Draw target position
            Vector3 targetPos = target.position + offset;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPos, 0.3f);
            
            // Draw offset vector
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(target.position, target.position + offset);
        }
#endif
    }
}
