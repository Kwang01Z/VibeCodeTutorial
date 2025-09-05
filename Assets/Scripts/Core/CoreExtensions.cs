using UnityEngine;

namespace EndlessRunner.Core
{
    /// <summary>
    /// Extension methods hữu ích cho Unity types
    /// </summary>
    public static class CoreExtensions
    {
        #region Vector3 Extensions
        
        /// <summary>
        /// Set chỉ X component của Vector3
        /// </summary>
        public static Vector3 WithX(this Vector3 vector, float x)
        {
            return new Vector3(x, vector.y, vector.z);
        }
        
        /// <summary>
        /// Set chỉ Y component của Vector3
        /// </summary>
        public static Vector3 WithY(this Vector3 vector, float y)
        {
            return new Vector3(vector.x, y, vector.z);
        }
        
        /// <summary>
        /// Set chỉ Z component của Vector3
        /// </summary>
        public static Vector3 WithZ(this Vector3 vector, float z)
        {
            return new Vector3(vector.x, vector.y, z);
        }
        
        /// <summary>
        /// Add giá trị vào X component
        /// </summary>
        public static Vector3 AddX(this Vector3 vector, float deltaX)
        {
            return new Vector3(vector.x + deltaX, vector.y, vector.z);
        }
        
        /// <summary>
        /// Add giá trị vào Y component
        /// </summary>
        public static Vector3 AddY(this Vector3 vector, float deltaY)
        {
            return new Vector3(vector.x, vector.y + deltaY, vector.z);
        }
        
        /// <summary>
        /// Add giá trị vào Z component
        /// </summary>
        public static Vector3 AddZ(this Vector3 vector, float deltaZ)
        {
            return new Vector3(vector.x, vector.y, vector.z + deltaZ);
        }
        
        /// <summary>
        /// Flatten vector về XZ plane (y = 0)
        /// </summary>
        public static Vector3 FlattenY(this Vector3 vector)
        {
            return new Vector3(vector.x, 0f, vector.z);
        }
        
        #endregion
        
        #region Transform Extensions
        
        /// <summary>
        /// Set position X coordinate
        /// </summary>
        public static void SetPositionX(this Transform transform, float x)
        {
            transform.position = transform.position.WithX(x);
        }
        
        /// <summary>
        /// Set position Y coordinate
        /// </summary>
        public static void SetPositionY(this Transform transform, float y)
        {
            transform.position = transform.position.WithY(y);
        }
        
        /// <summary>
        /// Set position Z coordinate
        /// </summary>
        public static void SetPositionZ(this Transform transform, float z)
        {
            transform.position = transform.position.WithZ(z);
        }
        
        /// <summary>
        /// Add to position X coordinate
        /// </summary>
        public static void AddPositionX(this Transform transform, float deltaX)
        {
            transform.position = transform.position.AddX(deltaX);
        }
        
        /// <summary>
        /// Add to position Y coordinate
        /// </summary>
        public static void AddPositionY(this Transform transform, float deltaY)
        {
            transform.position = transform.position.AddY(deltaY);
        }
        
        /// <summary>
        /// Add to position Z coordinate
        /// </summary>
        public static void AddPositionZ(this Transform transform, float deltaZ)
        {
            transform.position = transform.position.AddZ(deltaZ);
        }
        
        /// <summary>
        /// Reset transform về default values
        /// </summary>
        public static void ResetTransform(this Transform transform)
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
        
        /// <summary>
        /// Copy transform từ source
        /// </summary>
        public static void CopyFrom(this Transform transform, Transform source)
        {
            transform.position = source.position;
            transform.rotation = source.rotation;
            transform.localScale = source.localScale;
        }
        
        #endregion
        
        #region GameObject Extensions
        
        /// <summary>
        /// Safe GetComponent - không throw exception nếu không tìm thấy
        /// </summary>
        public static bool TryGetComponent<T>(this GameObject gameObject, out T component) where T : Component
        {
            component = gameObject.GetComponent<T>();
            return component != null;
        }
        
        /// <summary>
        /// Get hoặc add component
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }
        
        /// <summary>
        /// Set layer recursively cho tất cả children
        /// </summary>
        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            
            foreach (Transform child in gameObject.transform)
            {
                child.gameObject.SetLayerRecursively(layer);
            }
        }
        
        /// <summary>
        /// Kiểm tra GameObject có trong layer mask không
        /// </summary>
        public static bool IsInLayerMask(this GameObject gameObject, LayerMask layerMask)
        {
            return (layerMask & (1 << gameObject.layer)) != 0;
        }
        
        #endregion
        
        #region Rigidbody Extensions
        
        /// <summary>
        /// Add force chỉ theo X axis
        /// </summary>
        public static void AddForceX(this Rigidbody rigidbody, float forceX, ForceMode mode = ForceMode.Force)
        {
            rigidbody.AddForce(Vector3.right * forceX, mode);
        }
        
        /// <summary>
        /// Add force chỉ theo Y axis
        /// </summary>
        public static void AddForceY(this Rigidbody rigidbody, float forceY, ForceMode mode = ForceMode.Force)
        {
            rigidbody.AddForce(Vector3.up * forceY, mode);
        }
        
        /// <summary>
        /// Add force chỉ theo Z axis
        /// </summary>
        public static void AddForceZ(this Rigidbody rigidbody, float forceZ, ForceMode mode = ForceMode.Force)
        {
            rigidbody.AddForce(Vector3.forward * forceZ, mode);
        }
        
        /// <summary>
        /// Set velocity component riêng biệt
        /// </summary>
        public static void SetVelocityX(this Rigidbody rigidbody, float velocityX)
        {
            rigidbody.velocity = rigidbody.velocity.WithX(velocityX);
        }
        
        public static void SetVelocityY(this Rigidbody rigidbody, float velocityY)
        {
            rigidbody.velocity = rigidbody.velocity.WithY(velocityY);
        }
        
        public static void SetVelocityZ(this Rigidbody rigidbody, float velocityZ)
        {
            rigidbody.velocity = rigidbody.velocity.WithZ(velocityZ);
        }
        
        #endregion
        
        #region Math Extensions
        
        /// <summary>
        /// Remap giá trị từ range này sang range khác
        /// </summary>
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }
        
        /// <summary>
        /// Clamp angle trong range [-180, 180]
        /// </summary>
        public static float ClampAngle(this float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }
        
        /// <summary>
        /// Kiểm tra float approximately equal (tránh floating point errors)
        /// </summary>
        public static bool Approximately(this float a, float b, float threshold = 0.01f)
        {
            return Mathf.Abs(a - b) < threshold;
        }
        
        #endregion
        
        #region Collection Extensions
        
        /// <summary>
        /// Get random element từ array
        /// </summary>
        public static T GetRandomElement<T>(this T[] array)
        {
            if (array == null || array.Length == 0)
                return default(T);
                
            return array[Random.Range(0, array.Length)];
        }
        
        /// <summary>
        /// Shuffle array in place (Fisher-Yates)
        /// </summary>
        public static void Shuffle<T>(this T[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                T temp = array[i];
                array[i] = array[randomIndex];
                array[randomIndex] = temp;
            }
        }
        
        #endregion
    }
}
