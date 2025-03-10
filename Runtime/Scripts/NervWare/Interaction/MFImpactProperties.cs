
using UnityEngine;
using UnityEngine.Serialization;
using Vector3 = UnityEngine.Vector3;

namespace Realmsmith.Interaction
{
    public enum SurfaceType : byte
    {
        NoSurface,
        Brick,
        Carpet,
        Ceramic,
        Concrete,
        Default,
        Dirt,
        Glass,
        Grass,
        Gravel,
        Metal,
        Plaster,
        Rock,
        Thatch,
        Wood
    }

    public enum SurfaceHardness : byte
    {
        None,
        Soft,
        Medium,
        Hard
    }
    
    /// <summary>
    /// A ScriptableObject for holding various information about an object.
    /// </summary>
    [CreateAssetMenu(menuName = "ScriptableObjects/ImpactProperties/Create Data", fileName = "NewImpactProperties")]
    public class MFImpactProperties : ScriptableObject
    {
        public static MFImpactProperties Default() => CreateInstance<MFImpactProperties>();
        [SerializeField] private GameObject[] decalPrefab;
        [SerializeField] private GameObject[] impactPrefab;
        [SerializeField] private float decalSize = 0.1f;
        [SerializeField] private float decalPositioningOffset = 0.02f;
        [SerializeField] private SurfaceType surfaceType = SurfaceType.Default;
        [SerializeField] private SurfaceHardness surfaceHardness = SurfaceHardness.Hard;
        [SerializeField] private bool canBeStabbed = true;
        [SerializeField] private float stabDepth = 0.01f;
        [SerializeField] private float stabDamper = 20000f;
        [SerializeField] private float stabSolidObjectDamper = 1000f;
        [SerializeField] private bool spawnDecal = true;
        [SerializeField] private bool spawnImpact = true;
        public SurfaceType SurfaceType { get; set; }
        public SurfaceHardness SurfaceHardness => surfaceHardness;

    }
}