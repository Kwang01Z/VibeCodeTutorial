using UnityEngine;

namespace EndlessRunner.Data
{
    /// <summary>
    /// Configuration cho lane system - vị trí làn, tốc độ chuyển làn, curves
    /// </summary>
    [CreateAssetMenu(fileName = "LaneConfig", menuName = "EndlessRunner/Lane Config", order = 1)]
    public class LaneConfig : ScriptableObject
    {
        [Header("Lane Positions")]
        [SerializeField, Tooltip("Tọa độ X của từng làn (Left, Center, Right)")]
        private float[] _lanePositions = { -2.5f, 0f, 2.5f };
        
        [SerializeField, Tooltip("Chiều rộng mỗi làn (để detect collision bounds)")]
        private float _laneWidth = 2.0f;
        
        [Header("Lane Change Settings")]
        [SerializeField, Tooltip("Thời gian để chuyển giữa 2 làn kế bên (seconds)")]
        private float _laneChangeTime = 0.2f;
        
        [SerializeField, Tooltip("Curve điều khiển tốc độ chuyển làn (0->1 over time)")]
        private AnimationCurve _laneChangeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [SerializeField, Tooltip("Force áp dụng cho lane change (nếu dùng AddForce)")]
        private float _laneChangeForce = 15f;
        
        [Header("Physics Integration")]
        [SerializeField, Tooltip("Dùng AddForce thay vì trực tiếp set velocity")]
        private bool _useForceBasedMovement = true;
        
        [SerializeField, Tooltip("Drag áp dụng khi chuyển làn (để smooth stop)")]
        private float _laneChangeDrag = 5f;
        
        [Header("Constraints & Limits")]
        [SerializeField, Tooltip("Cooldown giữa 2 lần chuyển làn (ms)")]
        private float _laneChangeCooldown = 0.15f;
        
        [SerializeField, Tooltip("Cho phép chain lane changes (không cần chờ hoàn thành)")]
        private bool _allowChainedChanges = true;
        
        #region Public Properties
        
        /// <summary>
        /// Số lượng làn có sẵn
        /// </summary>
        public int LaneCount => _lanePositions.Length;
        
        /// <summary>
        /// Chiều rộng mỗi làn
        /// </summary>
        public float LaneWidth => _laneWidth;
        
        /// <summary>
        /// Thời gian chuyển làn
        /// </summary>
        public float LaneChangeTime => _laneChangeTime;
        
        /// <summary>
        /// Animation curve cho chuyển làn
        /// </summary>
        public AnimationCurve LaneChangeCurve => _laneChangeCurve;
        
        /// <summary>
        /// Force cho physics-based lane change
        /// </summary>
        public float LaneChangeForce => _laneChangeForce;
        
        /// <summary>
        /// Sử dụng AddForce thay vì velocity
        /// </summary>
        public bool UseForceBasedMovement => _useForceBasedMovement;
        
        /// <summary>
        /// Drag khi chuyển làn
        /// </summary>
        public float LaneChangeDrag => _laneChangeDrag;
        
        /// <summary>
        /// Cooldown giữa lane changes
        /// </summary>
        public float LaneChangeCooldown => _laneChangeCooldown;
        
        /// <summary>
        /// Cho phép chain lane changes
        /// </summary>
        public bool AllowChainedChanges => _allowChainedChanges;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Lấy tọa độ X của lane index cụ thể
        /// </summary>
        /// <param name="laneIndex">Index của lane (0-based)</param>
        /// <returns>Tọa độ X của lane, hoặc 0 nếu index không hợp lệ</returns>
        public float GetLanePosition(int laneIndex)
        {
            if (IsValidLane(laneIndex))
            {
                return _lanePositions[laneIndex];
            }
            
            Debug.LogWarning($"[LaneConfig] Invalid lane index: {laneIndex}. Using center lane position.");
            return GetCenterLanePosition();
        }
        
        /// <summary>
        /// Kiểm tra lane index có hợp lệ không
        /// </summary>
        /// <param name="laneIndex">Index cần kiểm tra</param>
        /// <returns>True nếu lane index hợp lệ</returns>
        public bool IsValidLane(int laneIndex)
        {
            return laneIndex >= 0 && laneIndex < _lanePositions.Length;
        }
        
        /// <summary>
        /// Lấy tọa độ X của lane giữa (center)
        /// </summary>
        /// <returns>Tọa độ X của lane center</returns>
        public float GetCenterLanePosition()
        {
            int centerIndex = _lanePositions.Length / 2;
            return _lanePositions[centerIndex];
        }
        
        /// <summary>
        /// Lấy index của lane center
        /// </summary>
        /// <returns>Index của lane center</returns>
        public int GetCenterLaneIndex()
        {
            return _lanePositions.Length / 2;
        }
        
        /// <summary>
        /// Tìm lane index gần nhất với position cho trước
        /// </summary>
        /// <param name="xPosition">Tọa độ X cần tìm lane gần nhất</param>
        /// <returns>Lane index gần nhất</returns>
        public int GetNearestLaneIndex(float xPosition)
        {
            int nearestIndex = 0;
            float minDistance = Mathf.Abs(xPosition - _lanePositions[0]);
            
            for (int i = 1; i < _lanePositions.Length; i++)
            {
                float distance = Mathf.Abs(xPosition - _lanePositions[i]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestIndex = i;
                }
            }
            
            return nearestIndex;
        }
        
        /// <summary>
        /// Kiểm tra có thể chuyển từ lane này sang lane khác không
        /// </summary>
        /// <param name="fromLane">Lane hiện tại</param>
        /// <param name="toLane">Lane đích</param>
        /// <returns>True nếu có thể chuyển</returns>
        public bool CanChangeLane(int fromLane, int toLane)
        {
            if (!IsValidLane(fromLane) || !IsValidLane(toLane))
                return false;
                
            if (fromLane == toLane)
                return false;
                
            // Chỉ cho phép chuyển sang lane kế bên (không skip lane)
            int laneDistance = Mathf.Abs(toLane - fromLane);
            return laneDistance == 1;
        }
        
        /// <summary>
        /// Tính khoảng cách giữa 2 lane
        /// </summary>
        /// <param name="fromLane">Lane bắt đầu</param>
        /// <param name="toLane">Lane đích</param>
        /// <returns>Khoảng cách theo tọa độ X</returns>
        public float GetLaneDistance(int fromLane, int toLane)
        {
            if (!IsValidLane(fromLane) || !IsValidLane(toLane))
                return 0f;
                
            return Mathf.Abs(_lanePositions[toLane] - _lanePositions[fromLane]);
        }
        
        /// <summary>
        /// Kiểm tra position có nằm trong bounds của lane không
        /// </summary>
        /// <param name="xPosition">Tọa độ X cần kiểm tra</param>
        /// <param name="laneIndex">Index của lane</param>
        /// <returns>True nếu position nằm trong lane bounds</returns>
        public bool IsInLaneBounds(float xPosition, int laneIndex)
        {
            if (!IsValidLane(laneIndex))
                return false;
                
            float laneCenter = _lanePositions[laneIndex];
            float halfWidth = _laneWidth * 0.5f;
            
            return xPosition >= (laneCenter - halfWidth) && xPosition <= (laneCenter + halfWidth);
        }
        
        /// <summary>
        /// Evaluate animation curve tại time t
        /// </summary>
        /// <param name="normalizedTime">Time từ 0 đến 1</param>
        /// <returns>Giá trị curve tại time t</returns>
        public float EvaluateLaneChangeCurve(float normalizedTime)
        {
            return _laneChangeCurve.Evaluate(Mathf.Clamp01(normalizedTime));
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate configuration trong Editor
        /// </summary>
        private void OnValidate()
        {
            // Đảm bảo có ít nhất 2 làn
            if (_lanePositions.Length < 2)
            {
                Debug.LogWarning("[LaneConfig] Cần ít nhất 2 làn để chơi được.");
            }
            
            // Đảm bảo thời gian chuyển làn > 0
            if (_laneChangeTime <= 0f)
            {
                Debug.LogWarning("[LaneConfig] Lane change time phải > 0.");
                _laneChangeTime = 0.1f;
            }
            
            // Đảm bảo lane width > 0
            if (_laneWidth <= 0f)
            {
                Debug.LogWarning("[LaneConfig] Lane width phải > 0.");
                _laneWidth = 1f;
            }
            
            // Đảm bảo cooldown >= 0
            if (_laneChangeCooldown < 0f)
            {
                Debug.LogWarning("[LaneConfig] Lane change cooldown phải >= 0.");
                _laneChangeCooldown = 0f;
            }
            
            // Sort lane positions để đảm bảo thứ tự từ trái sang phải
            System.Array.Sort(_lanePositions);
        }
        
        #endregion
        
        #region Debug Utilities
        
        /// <summary>
        /// Debug info cho lane config
        /// </summary>
        /// <returns>String mô tả config</returns>
        public override string ToString()
        {
            return $"LaneConfig: {LaneCount} lanes, positions: [{string.Join(", ", _lanePositions)}], " +
                   $"changeTime: {_laneChangeTime}s, width: {_laneWidth}";
        }
        
        /// <summary>
        /// Lấy debug info cho lane cụ thể
        /// </summary>
        /// <param name="laneIndex">Lane index</param>
        /// <returns>Debug string</returns>
        public string GetLaneDebugInfo(int laneIndex)
        {
            if (!IsValidLane(laneIndex))
                return "Invalid Lane";
                
            return $"Lane {laneIndex}: X={_lanePositions[laneIndex]}, " +
                   $"Bounds=[{_lanePositions[laneIndex] - _laneWidth/2f}, {_lanePositions[laneIndex] + _laneWidth/2f}]";
        }
        
        #endregion
    }
}
