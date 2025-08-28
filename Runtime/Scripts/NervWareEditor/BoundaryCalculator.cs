using UnityEngine;

namespace NervWareSDK.Editor
{
    public static class BoundaryCalculator
    {
        public static Vector3 CalculateExtents(GameObject prefab)
        {
            if (!prefab)
            {
                return Vector3.zero;
            }
            var go = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            Collider[] colliders = go.GetComponentsInChildren<Collider>();
            bool isFirst = true;
            Bounds bounds = new Bounds();
            foreach (var collider in colliders)
            {
                if (collider.isTrigger)
                {
                    continue;
                }

                if (isFirst)
                {
                    bounds = collider.bounds;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
                isFirst = false;
            }
            bounds.center -= go.transform.position;
            Object.DestroyImmediate(go);
            return bounds.extents;
        }
    }
}