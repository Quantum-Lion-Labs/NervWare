using UnityEngine;


namespace NervBox
{
    public class AudioClipPlayer : MonoBehaviour
    {
        private enum LoopSpatialMode
        {
            Spatial,
            Ambient
        }

        [SerializeField] private AudioClip audioClip;
        [SerializeField] private bool loop;
        [SerializeField] private LoopSpatialMode loopSpatialMode = LoopSpatialMode.Spatial;
        [SerializeField] private bool playOnAwake = true;
        [SerializeField] private bool playOnStart = false;
        [SerializeField] private bool playOnDisable = false;
        [SerializeField] private bool playOnEnable = false;
        [SerializeField] private bool playOnDestroy = false;
        [SerializeField] private bool stopOnDisable = false;
        [SerializeField] private bool stopOnDestroy = true;

        [SerializeField] [Range(0f, 100f)] private float directivityIntensity = 0.0f;
        [SerializeField] [Range(0f, 1f)] private float earlyReflectionsSend = 0.6f;
        [SerializeField] [Range(0f, 10f)] private float layer1MinDistance = 0.01f;
        [SerializeField] [Range(0f, 1000f)] private float volumetricRadius = 0.0f;
        [SerializeField] [Range(0f, 100f)] private float hrtfIntensity = 100.0f;
        [SerializeField] [Range(0f, 100f)] private float occlusionIntensity = 100.0f;
        [SerializeField] [Range(0f, 150f)] private float reverbReach = 50.0f;
        [SerializeField] [Range(0f, 100f)] private float layer1MaxDistance = 0.2f;
        [SerializeField] [Range(0f, 1f)] private float reverbSend = 0.6f;
       
        public void Play()
        {
        }
    }
}