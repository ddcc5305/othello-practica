using System;

namespace UnityEngine
{
    public class MonoBehaviour
    {
        public void Start() {}
    }

    public class GameObject
    {
        private static BoardManager cachedBoardManager = new BoardManager();

        public static GameObject FindGameObjectWithTag(string tag)
        {
            return new GameObject();
        }

        public T GetComponent<T>()
        {
            if (typeof(T) == typeof(BoardManager))
            {
                return (T)(object)cachedBoardManager;
            }
            return default;
        }
    }

    public static class Debug
    {
        public static void Log(object message)
        {
            // Suppress board print output to keep logs clean
        }
    }

    public class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header) {}
    }

    public class RangeAttribute : Attribute
    {
        public RangeAttribute(double min, double max) {}
    }

    public class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string tooltip) {}
    }
}
