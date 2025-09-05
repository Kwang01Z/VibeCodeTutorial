using System;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// Interface định nghĩa contract cho RunnerController
    /// Quản lý state machine và xử lý các hành động của runner
    /// </summary>
    public interface IRunnerController
    {
        #region Properties
        
        /// <summary>
        /// Trạng thái hiện tại của runner
        /// </summary>
        RunnerState CurrentState { get; }
        
        /// <summary>
        /// Có đang trong trạng thái có thể thực hiện hành động không
        /// </summary>
        bool IsAlive { get; }
        
        /// <summary>
        /// Có đang grounded không (để kiểm tra jump/slide validity)
        /// </summary>
        bool IsGrounded { get; }
        
        /// <summary>
        /// Có đang trong I-frames không
        /// </summary>
        bool IsInIFrames { get; }
        
        /// <summary>
        /// Thời gian còn lại của trạng thái hiện tại (0 nếu không có timer)
        /// </summary>
        float StateTimeRemaining { get; }
        
        #endregion
        
        #region Methods
        
        /// <summary>
        /// Kiểm tra có thể thực hiện hành động này không
        /// </summary>
        /// <param name="action">Hành động muốn thực hiện</param>
        /// <param name="parameter">Tham số bổ sung (vd: direction cho lane change)</param>
        /// <returns>True nếu có thể thực hiện</returns>
        bool CanPerformAction(RunnerAction action, int parameter = 0);
        
        /// <summary>
        /// Thực hiện hành động
        /// </summary>
        /// <param name="action">Hành động cần thực hiện</param>
        /// <param name="parameter">Tham số bổ sung (vd: direction cho lane change)</param>
        /// <returns>True nếu thực hiện thành công</returns>
        bool PerformAction(RunnerAction action, int parameter = 0);
        
        /// <summary>
        /// Force change state (dùng cho collision system, testing)
        /// </summary>
        /// <param name="newState">Trạng thái mới</param>
        /// <param name="duration">Thời lượng (0 = vô hạn)</param>
        void ForceChangeState(RunnerState newState, float duration = 0f);
        
        /// <summary>
        /// Reset về trạng thái Running (dùng khi respawn)
        /// </summary>
        void ResetToRunning();
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Event khi trạng thái thay đổi
        /// Args: (previousState, newState, duration)
        /// </summary>
        event Action<RunnerState, RunnerState, float> OnStateChanged;
        
        /// <summary>
        /// Event khi bắt đầu thực hiện hành động
        /// Args: (action, parameter, success)
        /// </summary>
        event Action<RunnerAction, int, bool> OnActionPerformed;
        
        /// <summary>
        /// Event khi runner chết
        /// </summary>
        event Action OnDeath;
        
        /// <summary>
        /// Event khi runner được revive
        /// </summary>
        event Action OnRevive;
        
        #endregion
    }
    
    /// <summary>
    /// Data structure chứa thông tin state transition
    /// Dùng để validate và log state changes
    /// </summary>
    public readonly struct StateTransition
    {
        public readonly RunnerState FromState;
        public readonly RunnerState ToState;
        public readonly RunnerAction TriggerAction;
        public readonly bool IsValid;
        
        public StateTransition(RunnerState fromState, RunnerState toState, RunnerAction triggerAction, bool isValid)
        {
            FromState = fromState;
            ToState = toState;
            TriggerAction = triggerAction;
            IsValid = isValid;
        }
        
        public override string ToString()
        {
            return $"{FromState} --[{TriggerAction}]--> {ToState} ({(IsValid ? "Valid" : "Invalid")})";
        }
    }
}
