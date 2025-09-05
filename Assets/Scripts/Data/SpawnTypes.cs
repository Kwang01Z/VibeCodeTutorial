namespace EndlessRunner.Data
{
    /// <summary>
    /// Độ khó của chunk, ảnh hưởng đến spawn rules
    /// </summary>
    public enum DifficultyTag : byte
    {
        /// <summary>
        /// Dễ - ít obstacle, khoảng cách rộng
        /// </summary>
        Easy = 0,
        
        /// <summary>
        /// Trung bình - vừa phải
        /// </summary>
        Medium = 1,
        
        /// <summary>
        /// Khó - nhiều obstacle, khoảng cách hẹp. Không được spawn liên tiếp.
        /// </summary>
        Hard = 2
    }
    
    /// <summary>
    /// Thông tin obstacle trong chunk
    /// </summary>
    [System.Serializable]
    public struct ObstacleInfo
    {
        /// <summary>
        /// Prefab obstacle (phải có Collider với layer "Obstacle")
        /// </summary>
        public UnityEngine.GameObject prefab;
        
        /// <summary>
        /// Vị trí local trong chunk (Y thường = 0, Z forward từ anchor)
        /// </summary>
        public UnityEngine.Vector3 localPosition;
        
        /// <summary>
        /// Lane yêu cầu (0=Left, 1=Center, 2=Right, -1=bất kỳ lane nào)
        /// </summary>
        public int requiredLane;
        
        /// <summary>
        /// Constructor cho editor scripting
        /// </summary>
        /// <param name="prefab">Obstacle prefab</param>
        /// <param name="localPos">Local position trong chunk</param>
        /// <param name="lane">Lane index (-1 = any)</param>
        public ObstacleInfo(UnityEngine.GameObject prefab, UnityEngine.Vector3 localPos, int lane = -1)
        {
            this.prefab = prefab;
            this.localPosition = localPos;
            this.requiredLane = lane;
        }
        
        /// <summary>
        /// Kiểm tra obstacle info có valid không
        /// </summary>
        public bool IsValid => prefab != null && requiredLane >= -1 && requiredLane <= 2;
        
        /// <summary>
        /// So sánh 2 obstacle có conflict position không (cùng lane, gần nhau)
        /// </summary>
        /// <param name="other">Obstacle khác</param>
        /// <param name="tolerance">Khoảng cách tối thiểu (m)</param>
        /// <returns>True nếu conflict</returns>
        public bool ConflictsWith(in ObstacleInfo other, float tolerance = 0.2f)
        {
            // Khác lane thì không conflict (trừ khi có lane = -1)
            if (requiredLane != -1 && other.requiredLane != -1 && requiredLane != other.requiredLane)
                return false;
            
            // Kiểm tra khoảng cách Z (forward direction)
            float distanceZ = UnityEngine.Mathf.Abs(localPosition.z - other.localPosition.z);
            return distanceZ < tolerance;
        }
    }
}
