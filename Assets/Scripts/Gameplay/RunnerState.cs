namespace EndlessRunner.Gameplay
{
    /// <summary>
    /// Định nghĩa các trạng thái có thể của Runner
    /// </summary>
    public enum RunnerState
    {
        /// <summary>
        /// Trạng thái chạy bình thường - có thể thực hiện mọi hành động
        /// </summary>
        Running,
        
        /// <summary>
        /// Đang nhảy - không thể nhảy tiếp nhưng có thể lane change
        /// </summary>
        Jumping,
        
        /// <summary>
        /// Đang trượt - không thể slide tiếp nhưng có thể lane change
        /// </summary>
        Sliding,
        
        /// <summary>
        /// Đang chuyển lane - có thể chain với jump/slide
        /// </summary>
        LaneChanging,
        
        /// <summary>
        /// Vừa bị hit - trạng thái ngắn trước khi vào I-frames
        /// </summary>
        Hit,
        
        /// <summary>
        /// Trong thời gian bất tử sau khi bị hit
        /// </summary>
        IFrames,
        
        /// <summary>
        /// Đã chết - trạng thái terminal
        /// </summary>
        Dead
    }

    /// <summary>
    /// Định nghĩa các hành động có thể thực hiện
    /// </summary>
    public enum RunnerAction
    {
        /// <summary>
        /// Nhảy lên
        /// </summary>
        Jump,
        
        /// <summary>
        /// Trượt xuống
        /// </summary>
        Slide,
        
        /// <summary>
        /// Chuyển lane (kèm theo direction parameter)
        /// </summary>
        LaneChange,
        
        /// <summary>
        /// Sử dụng item thủ công
        /// </summary>
        UseManualItem,
        
        /// <summary>
        /// Bị hit bởi obstacle
        /// </summary>
        Hit,
        
        /// <summary>
        /// Pause game
        /// </summary>
        Pause
    }
}
