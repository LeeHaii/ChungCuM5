using System;
using UnityEngine;

namespace BimRuntime
{
    [DisallowMultipleComponent]
    public sealed class BimCombinedElementMap : MonoBehaviour
    {
        [SerializeField] private int[] triangleToElement = Array.Empty<int>();

        public int TriangleCount => triangleToElement?.Length ?? 0;

        public bool TryGetElementIndex(int triangleIndex, out int elementIndex)
        {
            if (triangleToElement != null && (uint)triangleIndex < (uint)triangleToElement.Length)
            {
                elementIndex = triangleToElement[triangleIndex];
                return elementIndex >= 0;
            }

            elementIndex = -1;
            return false;
        }

#if UNITY_EDITOR
        public void SetTriangleMapForEditor(int[] mapping)
        {
            triangleToElement = mapping ?? Array.Empty<int>();
        }
#endif
    }
}
