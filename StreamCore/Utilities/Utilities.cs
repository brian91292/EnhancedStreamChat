using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Drawing;
using IllusionInjector;
using IllusionPlugin;
using System.Reflection;
using System.IO.Compression;

namespace StreamCore.Utils
{
    public class Utilities
    {
        public static void EmptyDirectory(string directory, bool delete = true)
        {
            if (Directory.Exists(directory))
            {
                var directoryInfo = new DirectoryInfo(directory);
                foreach (System.IO.FileInfo file in directoryInfo.GetFiles()) file.Delete();
                foreach (System.IO.DirectoryInfo subDirectory in directoryInfo.GetDirectories()) subDirectory.Delete(true);

                if (delete) Directory.Delete(directory);
            }
        }

        public static void MoveFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                MoveFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
            {
                string newFilePath = Path.Combine(target.FullName, file.Name);
                if (File.Exists(newFilePath))
                {
                    try
                    {
                        File.Delete(newFilePath);
                    }
                    catch (Exception)
                    {
                        //Plugin.Log($"Failed to delete file {Path.GetFileName(newFilePath)}! File is in use!");
                        string filesToDelete = Path.Combine(Environment.CurrentDirectory, "FilesToDelete");
                        if (!Directory.Exists(filesToDelete))
                            Directory.CreateDirectory(filesToDelete);
                        File.Move(newFilePath, Path.Combine(filesToDelete, file.Name));
                        //Plugin.Log("Moved file into FilesToDelete directory!");
                    }
                }
                file.MoveTo(newFilePath);
            }
        }

        public static IEnumerator ExtractZip(string zipPath, string extractPath)
        {
            if (File.Exists(zipPath))
            {
                bool extracted = false;
                try
                {
                    ZipFile.ExtractToDirectory(zipPath, ".requestcache");
                    extracted = true;
                }
                catch (Exception)
                {
                    Plugin.Log($"An error occured while trying to extract \"{zipPath}\"!");
                    yield break;
                }

                yield return new WaitForSeconds(0.25f);

                File.Delete(zipPath);

                try
                {
                    if (extracted)
                    {
                        if (!Directory.Exists(extractPath))
                            Directory.CreateDirectory(extractPath);

                        MoveFilesRecursively(new DirectoryInfo($"{Environment.CurrentDirectory}\\.requestcache"), new DirectoryInfo(extractPath));
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log($"An exception occured while trying to move files into their final directory! {e.ToString()}");
                }
            }
        }
        
        public enum DownloadType
        {
            Raw,
            Texture,
            Audio,
            AssetBundle
        }

        private static UnityWebRequest WebRequestForType(string url, DownloadType type, AudioType audioType = AudioType.OGGVORBIS)
        {
            switch(type)
            {
                case DownloadType.Raw:
                    return UnityWebRequest.Get(url);
                case DownloadType.Texture:
                    return UnityWebRequestTexture.GetTexture(url);
                case DownloadType.Audio:
                    return UnityWebRequestMultimedia.GetAudioClip(url, audioType);
                case DownloadType.AssetBundle:
                    return UnityWebRequestAssetBundle.GetAssetBundle(url);
            }
            return null;
        }

        public static IEnumerator Download(string url, DownloadType type, Action<UnityWebRequest> beforeSend, Action<UnityWebRequest> downloadCompleted, Action<UnityWebRequest> downloadFailed = null)
        {
            using (UnityWebRequest web = WebRequestForType(url, type))
            {
                if (web == null) yield break;

                beforeSend?.Invoke(web);

                // Send the web request
                yield return web.SendWebRequest();

                // Write the error if we encounter one
                if (web.isNetworkError || web.isHttpError)
                {
                    downloadFailed?.Invoke(web);
                    Plugin.Log($"Http error {web.responseCode} occurred during web request to url {url}. Error: {web.error}");
                    yield break;
                }
                downloadCompleted?.Invoke(web);
            }
        }
        
        public static IEnumerator DownloadFile(string url, string path)
        {
            yield return Download(url, DownloadType.Raw, null, (web) =>
            {
                byte[] data = web.downloadHandler.data;
                try
                {
                    if (!Directory.Exists(Path.GetDirectoryName(path)))
                        Directory.CreateDirectory(Path.GetDirectoryName(path));

                    File.WriteAllBytes(path, data);
                }
                catch (Exception)
                {
                    Plugin.Log("Failed to download file!");
                }
            });
        }

        public static bool IsModInstalled(string modName)
        {
            foreach (IPlugin p in PluginManager.Plugins)
            {
                if (p.Name == modName)
                {
                    return true;
                }
            }
            return false;
        }

        public static string StripHTML(string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }
    };
}
