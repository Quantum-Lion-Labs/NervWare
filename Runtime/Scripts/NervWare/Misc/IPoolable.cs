using UnityEngine;

namespace Realmsmith.Misc
{
    public interface IPoolable
    {
        void WarmUp(int id);
        void Return();
        void OnActivate();
        void OnRelease();
        
        bool DisableOnReturn { get; }
        bool DisableBeforeReuse { get; }

        GameObject GetGameObject();
        Transform GetTransform();
    }
}