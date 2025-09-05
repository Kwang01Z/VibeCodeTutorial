using System.Runtime.CompilerServices;

namespace EndlessRunner.Core
{
    /// <summary>
    /// XorShift128 PRNG implementation cho deterministic cross-platform random.
    /// Thay thế UnityEngine.Random để đảm bảo giống nhau giữa Editor và IL2CPP.
    /// </summary>
    [System.Serializable]
    public struct XorShift128
    {
        // State variables - 128 bit state
        private uint _x, _y, _z, _w;
        
        /// <summary>
        /// Khởi tạo với seed. Seed không được = 0 cho cả 4 state.
        /// </summary>
        /// <param name="seed">Seed value (nếu 0 sẽ dùng default seeds)</param>
        public XorShift128(uint seed)
        {
            if (seed == 0)
            {
                // Default seeds nếu seed = 0
                _x = 123456789u;
                _y = 362436069u;
                _z = 521288629u;
                _w = 88675123u;
            }
            else
            {
                // Khởi tạo từ seed với Splitmix32
                _x = SplitMix32(ref seed);
                _y = SplitMix32(ref seed);
                _z = SplitMix32(ref seed);
                _w = SplitMix32(ref seed);
            }
        }
        
        /// <summary>
        /// Tạo số ngẫu nhiên 32-bit
        /// </summary>
        /// <returns>Random uint [0, uint.MaxValue]</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint NextUInt()
        {
            uint t = _x ^ (_x << 11);
            _x = _y;
            _y = _z;
            _z = _w;
            _w = _w ^ (_w >> 19) ^ t ^ (t >> 8);
            return _w;
        }
        
        /// <summary>
        /// Tạo số ngẫu nhiên int [0, max)
        /// </summary>
        /// <param name="max">Giá trị max (exclusive)</param>
        /// <returns>Random int [0, max)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int NextInt(int max)
        {
            if (max <= 0) return 0;
            return (int)(NextUInt() % (uint)max);
        }
        
        /// <summary>
        /// Tạo số ngẫu nhiên int [min, max)
        /// </summary>
        /// <param name="min">Giá trị min (inclusive)</param>
        /// <param name="max">Giá trị max (exclusive)</param>
        /// <returns>Random int [min, max)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int NextInt(int min, int max)
        {
            if (min >= max) return min;
            return min + NextInt(max - min);
        }
        
        /// <summary>
        /// Tạo số ngẫu nhiên float [0.0f, 1.0f)
        /// </summary>
        /// <returns>Random float [0.0f, 1.0f)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float NextFloat()
        {
            // Chia cho 2^32 để có range [0, 1)
            return NextUInt() * (1.0f / 4294967296.0f);
        }
        
        /// <summary>
        /// Tạo số ngẫu nhiên float [min, max)
        /// </summary>
        /// <param name="min">Giá trị min</param>
        /// <param name="max">Giá trị max</param>
        /// <returns>Random float [min, max)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float NextFloat(float min, float max)
        {
            return min + NextFloat() * (max - min);
        }
        
        /// <summary>
        /// Tạo bool ngẫu nhiên
        /// </summary>
        /// <returns>Random boolean</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool NextBool()
        {
            return (NextUInt() & 1) == 1;
        }
        
        /// <summary>
        /// Tạo bool với xác suất cụ thể
        /// </summary>
        /// <param name="probability">Xác suất True [0.0, 1.0]</param>
        /// <returns>True với xác suất probability</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool NextBool(float probability)
        {
            return NextFloat() < probability;
        }
        
        /// <summary>
        /// Chọn element ngẫu nhiên từ array
        /// </summary>
        /// <typeparam name="T">Type của element</typeparam>
        /// <param name="array">Array để chọn</param>
        /// <returns>Random element, default(T) nếu array null/empty</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Choose<T>(T[] array)
        {
            if (array == null || array.Length == 0)
                return default(T);
            return array[NextInt(array.Length)];
        }
        
        /// <summary>
        /// Shuffle array in-place (Fisher-Yates)
        /// </summary>
        /// <typeparam name="T">Type của element</typeparam>
        /// <param name="array">Array cần shuffle</param>
        public void Shuffle<T>(T[] array)
        {
            if (array == null || array.Length <= 1) return;
            
            for (int i = array.Length - 1; i > 0; i--)
            {
                int randomIndex = NextInt(i + 1);
                T temp = array[i];
                array[i] = array[randomIndex];
                array[randomIndex] = temp;
            }
        }
        
        /// <summary>
        /// Reset state về seed ban đầu
        /// </summary>
        /// <param name="seed">Seed mới</param>
        public void Reseed(uint seed)
        {
            this = new XorShift128(seed);
        }
        
        /// <summary>
        /// Lấy state hiện tại (để save/restore)
        /// </summary>
        /// <returns>State tuple</returns>
        public (uint x, uint y, uint z, uint w) GetState()
        {
            return (_x, _y, _z, _w);
        }
        
        /// <summary>
        /// Restore state từ tuple
        /// </summary>
        /// <param name="state">State đã save</param>
        public void SetState((uint x, uint y, uint z, uint w) state)
        {
            _x = state.x;
            _y = state.y;
            _z = state.z;
            _w = state.w;
        }
        
        /// <summary>
        /// SplitMix32 để khởi tạo state từ seed
        /// </summary>
        /// <param name="seed">Seed reference sẽ được modify</param>
        /// <returns>Next pseudo-random number</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint SplitMix32(ref uint seed)
        {
            seed += 0x9e3779b9u;
            uint result = seed;
            result ^= result >> 16;
            result *= 0x85ebca6bu;
            result ^= result >> 13;
            result *= 0xc2b2ae35u;
            result ^= result >> 16;
            return result;
        }
    }
    
    /// <summary>
    /// Utility methods cho weighted random và advanced operations
    /// </summary>
    public static class RandomUtility
    {
        /// <summary>
        /// Weighted random selection
        /// </summary>
        /// <typeparam name="T">Type của item</typeparam>
        /// <param name="prng">PRNG instance</param>
        /// <param name="items">Array items</param>
        /// <param name="weights">Array weights (cùng length với items)</param>
        /// <returns>Selected item theo weight</returns>
        public static T WeightedChoice<T>(ref XorShift128 prng, T[] items, float[] weights)
        {
            if (items == null || weights == null || items.Length != weights.Length || items.Length == 0)
                return default(T);
            
            // Tính tổng weights
            float totalWeight = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                totalWeight += weights[i];
            }
            
            if (totalWeight <= 0f)
                return items[0]; // Fallback
            
            // Random value [0, totalWeight)
            float randomValue = prng.NextFloat() * totalWeight;
            
            // Tìm item tương ứng
            float currentWeight = 0f;
            for (int i = 0; i < items.Length; i++)
            {
                currentWeight += weights[i];
                if (randomValue < currentWeight)
                    return items[i];
            }
            
            // Fallback (shouldn't happen)
            return items[items.Length - 1];
        }
        
        /// <summary>
        /// Hash uint từ Vector3 position (cho seed derivation)
        /// </summary>
        /// <param name="position">World position</param>
        /// <returns>Hash value</returns>
        public static uint HashPosition(UnityEngine.Vector3 position)
        {
            // Simple hash combining X, Y, Z
            uint hash = 17;
            hash = hash * 31 + (uint)UnityEngine.Mathf.RoundToInt(position.x * 1000f);
            hash = hash * 31 + (uint)UnityEngine.Mathf.RoundToInt(position.y * 1000f);
            hash = hash * 31 + (uint)UnityEngine.Mathf.RoundToInt(position.z * 1000f);
            return hash;
        }
    }
}
