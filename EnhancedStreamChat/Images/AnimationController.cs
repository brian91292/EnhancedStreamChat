using EnhancedStreamChat.UI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;

namespace EnhancedStreamChat.Textures
{
    public class AnimControllerData
    {
        public string textureIndex;
        public int uvIndex = 0;
        public DateTime lastSwitch = DateTime.UtcNow;
        public Rect[] uvs;
        public float[] delays;
        private int _refCtr = 0;
        public int RefCtr {
            get 
            {
                return _refCtr;
            }
        }
        public void IncRefs()
        {
            lock(this)
            {
                if (_refCtr == 0)
                {
                    uvIndex = 0;
                    lastSwitch = DateTime.UtcNow;
                }
                _refCtr++;
            }
        }
        public void DecRefs()
        {
            lock(this)
            {
                _refCtr--;
                if (_refCtr < 0)
                    _refCtr = 0;
            }
        }

        public AnimControllerData(string textureIndex, Rect[] uvs, float[] delays)
        {
            this.textureIndex = textureIndex;
            this.uvs = uvs;
            this.delays = delays;
        }
    };

    class AnimationController : MonoBehaviour
    {
        public static AnimationController Instance = null;
        public List<AnimControllerData> registeredAnimations = new List<AnimControllerData>();

        public static void OnLoad()
        {
            if (Instance) return;
            new GameObject("AnimationController").AddComponent<AnimationController>();
        }

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public AnimControllerData Register(string textureIndex, Rect[] uvs, float[] delays)
        {
            AnimControllerData newAnim = new AnimControllerData(textureIndex, uvs, delays);
            registeredAnimations.Add(newAnim);
            return newAnim;
        }

        private void CheckFrame(AnimControllerData animation, DateTime now)
        {
            var difference = now - animation.lastSwitch;
            if (difference.Milliseconds < animation.delays[animation.uvIndex])
            {
                // Frame still has time remaining
                return;
            }

            var cachedTextureData = ImageDownloader.CachedTextures[animation.textureIndex];
            if (cachedTextureData.isDelayConsistent && animation.delays[animation.uvIndex] <= 10 && difference.Milliseconds < 100)
            {
                // Bump animations with consistently 10ms or lower frame timings to 100ms
                return;
            }


            animation.lastSwitch = now;
            do
            {
                animation.uvIndex++;
                if (animation.uvIndex >= animation.uvs.Length)
                    animation.uvIndex = 0;                                
            }
            while (!cachedTextureData.isDelayConsistent && animation.delays[animation.uvIndex] == 0);

            Rect uv = animation.uvs[animation.uvIndex];
            cachedTextureData.animInfo.shadowMaterial?.SetVector("_CropFactors", new Vector4(uv.x, uv.y, uv.width, uv.height));
            cachedTextureData.animInfo.imageMaterial?.SetVector("_CropFactors", new Vector4(uv.x, uv.y, uv.width, uv.height));
        }

        void Update()
        {
            var now = DateTime.UtcNow;
            foreach (AnimControllerData animation in registeredAnimations)
            {
                if(animation.RefCtr > 0)
                    CheckFrame(animation, now);
            }
        }
    };
}
