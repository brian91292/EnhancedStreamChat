using EnhancedStreamChat.Images;
using EnhancedStreamChat.UI;
using StreamCore;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EnhancedStreamChat.Textures
{
    class AnimationDecoder
    {
        class GifInfo
        {
            public List<FrameInfo> frames = new List<FrameInfo>();
            public int frameCount = 0;
            public bool initialized = false;
        };

        class FrameInfo
        {
            public int width, height;
            public Color32[] colors;
            public float delay = 0;
            public FrameInfo(int width, int height)
            {
                this.width = width;
                this.height = height;
            }
        };

        private static int GetTextureSize(GifInfo frameInfo, int i)
        {
            int testNum = 2;
        retry:
            int numFrames = frameInfo.frameCount;
            // Make sure the number of frames is cleanly divisible by our testNum
            if (!(numFrames % testNum != 0))
                numFrames += numFrames % testNum;

            int numFramesInRow = numFrames / testNum;
            int numFramesInColumn = numFrames / numFramesInRow;

            if (numFramesInRow > numFramesInColumn)
            {
                testNum += 2;
                goto retry;
            }

            var textureWidth = Mathf.Clamp(numFramesInRow * frameInfo.frames[i].width, 0, 2048);
            var textureHeight = Mathf.Clamp(numFramesInColumn * frameInfo.frames[i].height, 0, 2048);
            return Mathf.Max(textureWidth, textureHeight);
        }

        public static IEnumerator Process(byte[] gifData, Action<Texture2D, Rect[], float, int, int, TextureDownloadInfo> callback, TextureDownloadInfo imageDownloadInfo)
        {
            Plugin.Log($"Started decoding gif {imageDownloadInfo.spriteIndex}");

            List<Texture2D> texList = new List<Texture2D>();
            GifInfo frameInfo = new GifInfo();
            DateTime startTime = DateTime.Now;
            Task.Run(() => ProcessingThread(gifData, frameInfo));
            yield return new WaitUntil(() => { return frameInfo.initialized; });


            int textureSize = 2048, width = 0, height = 0;
            Texture2D texture = null;
            float delay = -1f;
            for (int i = 0; i < frameInfo.frameCount; i++)
            {
                if (frameInfo.frames.Count <= i)
                {
                    yield return new WaitUntil(() => { return frameInfo.frames.Count > i; });
                    //Plugin.Log($"Frame {i} is ready for processing! Frame is {frameInfo.frames[i].width}x{frameInfo.frames[i].height}");
                }

                if(texture == null)
                {
                    textureSize = GetTextureSize(frameInfo, i);
                    texture = new Texture2D(textureSize, textureSize);
                }

                FrameInfo currentFrameInfo = frameInfo.frames[i];
                if (delay == -1f)
                    delay = currentFrameInfo.delay;
                
                var frameTexture = new Texture2D(currentFrameInfo.width, currentFrameInfo.height);
                frameTexture.wrapMode = TextureWrapMode.Clamp;
                try
                {
                    frameTexture.SetPixels32(currentFrameInfo.colors);
                    frameTexture.Apply(i == 0);
                }
                catch (Exception e)
                {
                    Plugin.Log($"Exception while decoding gif! Frame: {i}, Exception: {e}");
                    yield break;
                }
                yield return null;
                
                texList.Add(frameTexture);
                
                // Instant callback after we decode the first frame in order to display a still image until the animated one is finished loading
                if (i == 0) 
                {
                    width = frameInfo.frames[i].width;
                    height = frameInfo.frames[i].height;
                    callback?.Invoke(frameTexture, texture.PackTextures(new Texture2D[] { frameTexture }, 2, textureSize, true), delay, width, height, imageDownloadInfo);
                }
            }
            Rect[] atlas = texture.PackTextures(texList.ToArray(), 2, textureSize, true);

            yield return null;

            callback?.Invoke(texture, atlas, delay, width, height, imageDownloadInfo);
            Plugin.Log($"Finished decoding gif {imageDownloadInfo.spriteIndex}! Elapsed time: {(DateTime.Now - startTime).TotalSeconds} seconds.");
        }

        private static void ProcessingThread(byte[] gifData, GifInfo frameInfo)
        {
            var gifImage = EmojiUtilities.byteArrayToImage(gifData);
            var dimension = new System.Drawing.Imaging.FrameDimension(gifImage.FrameDimensionsList[0]);
            int frameCount = gifImage.GetFrameCount(dimension);

            frameInfo.frameCount = frameCount;
            frameInfo.initialized = true;

            int index = 0;
            for (int i = 0; i < frameCount; i++)
            {
                gifImage.SelectActiveFrame(dimension, i);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(gifImage.Width, gifImage.Height);
                System.Drawing.Graphics.FromImage(bitmap).DrawImage(gifImage, System.Drawing.Point.Empty);
                LockBitmap frame = new LockBitmap(bitmap);
                
                frame.LockBits();
                FrameInfo currentFrame = new FrameInfo(bitmap.Width, bitmap.Height);

                if (currentFrame.colors == null)
                    currentFrame.colors = new Color32[frame.Height * frame.Width];
                
                for (int x = 0; x < frame.Width; x++)
                {
                    for (int y = 0; y < frame.Height; y++)
                    {
                        System.Drawing.Color sourceColor = frame.GetPixel(x, y);
                        currentFrame.colors[(frame.Height - y - 1) * frame.Width + x] = new Color32(sourceColor.R, sourceColor.G, sourceColor.B, sourceColor.A);
                    }
                }

                int delayPropertyValue = BitConverter.ToInt32(gifImage.GetPropertyItem(20736).Value, index);
                // If the delay property is 0, assume that it's a 10fps emote
                if (delayPropertyValue == 0)
                    delayPropertyValue = 10;
                
                currentFrame.delay = (float)delayPropertyValue / 100.0f;
                frameInfo.frames.Add(currentFrame);
                index += 4;
                
                Thread.Sleep(Globals.IsAtMainMenu ? 0 : 10);
            }
        }
    };
}
