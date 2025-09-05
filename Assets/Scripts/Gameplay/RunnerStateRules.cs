using System.Collections.Generic;

namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// Static class định nghĩa rules cho state transitions
    /// Centralized logic để validate và thực hiện state changes
    /// </summary>
    public static class RunnerStateRules
    {
        #region State Transition Matrix
        
        /// <summary>
        /// Ma trận định nghĩa các state transitions hợp lệ
        /// Key: (fromState, action) -> Value: (toState, requiresGrounded, requiresAlive)
        /// </summary>
        private static readonly Dictionary<(RunnerState, RunnerAction), (RunnerState toState, bool requiresGrounded, bool requiresAlive)> _transitionRules = new()
        {
            // From Running
            [(RunnerState.Running, RunnerAction.Jump)] = (RunnerState.Jumping, true, true),
            [(RunnerState.Running, RunnerAction.Slide)] = (RunnerState.Sliding, true, true),
            [(RunnerState.Running, RunnerAction.LaneChange)] = (RunnerState.LaneChanging, false, true),
            [(RunnerState.Running, RunnerAction.Hit)] = (RunnerState.Hit, false, true),
            
            // From Jumping
            [(RunnerState.Jumping, RunnerAction.LaneChange)] = (RunnerState.LaneChanging, false, true), // Cho phép lane change khi jump
            [(RunnerState.Jumping, RunnerAction.Hit)] = (RunnerState.Hit, false, true),
            // Jump -> Running (auto khi grounded, không qua action)
            
            // From Sliding  
            [(RunnerState.Sliding, RunnerAction.LaneChange)] = (RunnerState.LaneChanging, false, true), // Cho phép lane change khi slide
            [(RunnerState.Sliding, RunnerAction.Hit)] = (RunnerState.Hit, false, true),
            // Slide -> Running (auto khi timer hết)
            
            // From LaneChanging
            [(RunnerState.LaneChanging, RunnerAction.Jump)] = (RunnerState.Jumping, true, true), // Chain action
            [(RunnerState.LaneChanging, RunnerAction.Slide)] = (RunnerState.Sliding, true, true), // Chain action  
            [(RunnerState.LaneChanging, RunnerAction.Hit)] = (RunnerState.Hit, false, true),
            // LaneChanging -> Running (auto khi lane change completed)
            
            // From Hit
            [(RunnerState.Hit, RunnerAction.Hit)] = (RunnerState.IFrames, false, true), // Hit -> IFrames transition
            
            // From IFrames
            // IFrames -> Running (auto khi timer hết)
            // IFrames -> Dead (nếu health <= 0)
            
            // Dead state: terminal, no transitions allowed
        };
        
        #endregion
        
        #region Validation Methods
        
        /// <summary>
        /// Kiểm tra có thể thực hiện action từ state hiện tại không
        /// </summary>
        /// <param name="currentState">Trạng thái hiện tại</param>
        /// <param name="action">Hành động muốn thực hiện</param>
        /// <param name="isGrounded">Có đang grounded không</param>
        /// <param name="isAlive">Có còn sống không</param>
        /// <returns>True nếu hành động hợp lệ</returns>
        public static bool CanPerformAction(RunnerState currentState, RunnerAction action, bool isGrounded, bool isAlive)
        {
            // Dead state không thể thực hiện gì
            if (currentState == RunnerState.Dead)
                return false;
                
            // Pause luôn được phép (trừ khi Dead)
            if (action == RunnerAction.Pause)
                return true;
                
            // Manual item có thể dùng trong hầu hết trạng thái (trừ Dead, Hit)
            if (action == RunnerAction.UseManualItem)
                return currentState != RunnerState.Dead && currentState != RunnerState.Hit;
            
            // Kiểm tra trong transition matrix
            if (!_transitionRules.TryGetValue((currentState, action), out var rule))
                return false;
            
            // Validate điều kiện bổ sung
            if (rule.requiresGrounded && !isGrounded)
                return false;
                
            if (rule.requiresAlive && !isAlive)
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Lấy trạng thái đích khi thực hiện action
        /// </summary>
        /// <param name="currentState">Trạng thái hiện tại</param>
        /// <param name="action">Hành động</param>
        /// <returns>Trạng thái đích, hoặc currentState nếu không hợp lệ</returns>
        public static RunnerState GetTargetState(RunnerState currentState, RunnerAction action)
        {
            if (_transitionRules.TryGetValue((currentState, action), out var rule))
            {
                return rule.toState;
            }
            
            return currentState; // Không thay đổi nếu không hợp lệ
        }
        
        /// <summary>
        /// Tạo StateTransition struct để logging và debugging
        /// </summary>
        /// <param name="fromState">Trạng thái nguồn</param>
        /// <param name="action">Hành động trigger</param>
        /// <param name="isGrounded">Grounded state</param>
        /// <param name="isAlive">Alive state</param>
        /// <returns>StateTransition struct</returns>
        public static StateTransition CreateTransition(RunnerState fromState, RunnerAction action, bool isGrounded, bool isAlive)
        {
            var targetState = GetTargetState(fromState, action);
            var isValid = CanPerformAction(fromState, action, isGrounded, isAlive);
            
            return new StateTransition(fromState, targetState, action, isValid);
        }
        
        #endregion
        
        #region State Duration Defaults
        
        /// <summary>
        /// Thời lượng mặc định cho các trạng thái có timer
        /// </summary>
        public static readonly Dictionary<RunnerState, float> DefaultStateDurations = new()
        {
            [RunnerState.Jumping] = 0.6f,      // 600ms jump duration
            [RunnerState.Sliding] = 0.7f,      // 700ms slide duration  
            [RunnerState.Hit] = 0.1f,          // 100ms hit reaction
            [RunnerState.IFrames] = 1.2f,      // 1200ms invincibility
            [RunnerState.LaneChanging] = 0f,   // Controlled by LaneController
            [RunnerState.Running] = 0f,        // Indefinite
            [RunnerState.Dead] = 0f            // Terminal state
        };
        
        /// <summary>
        /// Coyote time - thời gian vẫn có thể jump sau khi rời ground
        /// </summary>
        public const float CoyoteTime = 0.1f; // 100ms
        
        /// <summary>
        /// Input buffer time - thời gian lưu input khi chưa thể thực hiện
        /// </summary>
        public const float InputBufferTime = 0.15f; // 150ms
        
        #endregion
        
        #region Validation Helpers
        
        /// <summary>
        /// Kiểm tra có phải trạng thái tạm thời (có timer) không
        /// </summary>
        /// <param name="state">Trạng thái cần kiểm tra</param>
        /// <returns>True nếu là trạng thái có thời hạn</returns>
        public static bool IsTimedState(RunnerState state)
        {
            return state switch
            {
                RunnerState.Jumping or RunnerState.Sliding or RunnerState.Hit or RunnerState.IFrames => true,
                _ => false
            };
        }
        
        /// <summary>
        /// Kiểm tra có thể transition về Running tự động không
        /// </summary>
        /// <param name="state">Trạng thái hiện tại</param>
        /// <returns>True nếu có thể auto-transition về Running</returns>
        public static bool CanAutoTransitionToRunning(RunnerState state)
        {
            return state switch
            {
                RunnerState.Jumping or RunnerState.Sliding or RunnerState.IFrames => true,
                _ => false
            };
        }
        
        /// <summary>
        /// Lấy trạng thái tiếp theo khi auto-transition (timer hết)
        /// </summary>
        /// <param name="currentState">Trạng thái hiện tại</param>
        /// <param name="hasHealth">Còn máu không (cho IFrames -> Running/Dead)</param>
        /// <returns>Trạng thái tiếp theo</returns>
        public static RunnerState GetAutoTransitionState(RunnerState currentState, bool hasHealth)
        {
            return currentState switch
            {
                RunnerState.Jumping => RunnerState.Running,
                RunnerState.Sliding => RunnerState.Running,
                RunnerState.Hit => RunnerState.IFrames,
                RunnerState.IFrames => hasHealth ? RunnerState.Running : RunnerState.Dead,
                _ => currentState // Không có auto-transition
            };
        }
        
        #endregion
    }
}
