using System;
using UnityEngine;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// Interface cho Lane Controller - quản lý chuyển làn và vị trí hiện tại
    /// </summary>
    public interface ILaneController
    {
        #region Properties
        
        /// <summary>
        /// Lane hiện tại (0-based index)
        /// </summary>
        int CurrentLane { get; }
        
        /// <summary>
        /// Lane đích (khi đang chuyển làn)
        /// </summary>
        int TargetLane { get; }
        
        /// <summary>
        /// Đang trong quá trình chuyển làn
        /// </summary>
        bool IsChangingLane { get; }
        
        /// <summary>
        /// Progress chuyển làn (0.0 - 1.0)
        /// </summary>
        float LaneChangeProgress { get; }
        
        /// <summary>
        /// Vị trí X hiện tại
        /// </summary>
        float CurrentXPosition { get; }
        
        /// <summary>
        /// Có thể thực hiện lane change không (check cooldown, constraints)
        /// </summary>
        bool CanChangeLaneGeneral { get; }
        
        #endregion
        
        #region Methods
        
        /// <summary>
        /// Yêu cầu chuyển sang lane cụ thể
        /// </summary>
        /// <param name="targetLane">Lane index đích</param>
        /// <returns>True nếu request được chấp nhận</returns>
        bool RequestLaneChange(int targetLane);
        
        /// <summary>
        /// Yêu cầu chuyển sang lane kế bên (relative)
        /// </summary>
        /// <param name="direction">-1 cho trái, +1 cho phải</param>
        /// <returns>True nếu request được chấp nhận</returns>
        bool RequestLaneChangeRelative(int direction);
        
        /// <summary>
        /// Hủy lane change hiện tại (nếu đang chuyển)
        /// </summary>
        void CancelLaneChange();
        
        /// <summary>
        /// Kiểm tra có thể chuyển sang lane cụ thể không
        /// </summary>
        /// <param name="targetLane">Lane index đích</param>
        /// <returns>True nếu có thể chuyển</returns>
        bool CanChangeLane(int targetLane);
        
        /// <summary>
        /// Force set lane mà không animation (cho initialization)
        /// </summary>
        /// <param name="laneIndex">Lane index cần set</param>
        void SetLaneImmediate(int laneIndex);
        
        /// <summary>
        /// Lấy tọa độ X của lane cụ thể
        /// </summary>
        /// <param name="laneIndex">Lane index</param>
        /// <returns>Tọa độ X của lane</returns>
        float GetLaneXPosition(int laneIndex);
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Event khi bắt đầu chuyển làn
        /// </summary>
        /// <param name="fromLane">Lane xuất phát</param>
        /// <param name="toLane">Lane đích</param>
        event Action<int, int> OnLaneChangeStarted;
        
        /// <summary>
        /// Event khi hoàn thành chuyển làn
        /// </summary>
        /// <param name="newLane">Lane mới sau khi chuyển</param>
        event Action<int> OnLaneChangeCompleted;
        
        /// <summary>
        /// Event khi lane change bị hủy
        /// </summary>
        /// <param name="currentLane">Lane hiện tại</param>
        event Action<int> OnLaneChangeCancelled;
        
        /// <summary>
        /// Event khi progress chuyển làn update (cho animation)
        /// </summary>
        /// <param name="progress">Progress từ 0.0 đến 1.0</param>
        /// <param name="currentXPosition">Vị trí X hiện tại</param>
        event Action<float, float> OnLaneChangeProgress;
        
        /// <summary>
        /// Event khi lane change request bị reject
        /// </summary>
        /// <param name="targetLane">Lane đích bị reject</param>
        /// <param name="reason">Lý do reject</param>
        event Action<int, string> OnLaneChangeRejected;
        
        #endregion
    }
    
    /// <summary>
    /// Enum mô tả trạng thái lane controller
    /// </summary>
    public enum LaneControllerState
    {
        /// <summary>Đứng yên tại lane</summary>
        Idle,
        
        /// <summary>Đang chuyển làn</summary>
        Changing,
        
        /// <summary>Trong cooldown (không thể chuyển làn)</summary>
        Cooldown,
        
        /// <summary>Bị disable (không thể control)</summary>
        Disabled
    }
    
    /// <summary>
    /// Data structure cho lane change request
    /// </summary>
    [System.Serializable]
    public struct LaneChangeRequest
    {
        public int fromLane;
        public int toLane;
        public float requestTime;
        public bool isRelativeChange;
        public int direction; // -1 left, +1 right
        
        public LaneChangeRequest(int from, int to, bool relative = false, int dir = 0)
        {
            fromLane = from;
            toLane = to;
            requestTime = Time.time;
            isRelativeChange = relative;
            direction = dir;
        }
    }
}
