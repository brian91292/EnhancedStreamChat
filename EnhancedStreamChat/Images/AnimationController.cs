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
    class AnimControllerData
    {
        public string textureIndex;
        public int uvIndex = 0;
        public DateTime lastSwitch = DateTime.Now;
        public Rect[] uvs;
        public float[] delays;
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

        public int Register(string textureIndex, Rect[] uvs, float[] delays)
        {
            AnimControllerData newAnim = new AnimControllerData(textureIndex, uvs, delays);
            registeredAnimations.Add(newAnim);
            return registeredAnimations.IndexOf(newAnim);
        }

        private void CheckFrame(AnimControllerData animation, DateTime now)
        {
            var difference = now - animation.lastSwitch;
            if (difference.Milliseconds < animation.delays[animation.uvIndex])
            {
                return;
            }

            var cachedTextureData = ImageDownloader.CachedTextures[animation.textureIndex];
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
                CheckFrame(animation, now);
            }
        }
    };
}
