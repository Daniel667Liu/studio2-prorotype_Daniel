using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace PixelCrushers.DialogueSystem.InkSupport
{

    public static class InkEditorUtility
    {
        public static List<InkEntrypoint> GetAllEntrypoints(out List<string> fullPaths)
        {
            var filesProcessed = new List<string>();
            var entrypoints = new List<InkEntrypoint>();
            var paths = new List<string>();
            var dialogueSystemInkIntegration = GameObject.FindObjectOfType<DialogueSystemInkIntegration>();
            if (dialogueSystemInkIntegration != null && dialogueSystemInkIntegration.inkJSONAssets.Count > 0)
            {
                dialogueSystemInkIntegration.inkJSONAssets.ForEach(asset => AddInkJsonAssetToEntrypoints(asset, entrypoints, paths, filesProcessed));
            }
            fullPaths = paths;
            ResolveDotDotPaths(fullPaths);
            SimplifySubmenus(fullPaths);
            ReplaceSlashesInSharedSubdirectories(fullPaths);
            return entrypoints;
        }

        public static string[] EntrypointsToStrings(List<InkEntrypoint> entrypoints)
        {
            var entrypointStrings = new string[entrypoints.Count];
            for (int i = 0; i < entrypoints.Count; i++)
            {
                entrypointStrings[i] = entrypoints[i].ToPopupString();
            }
            return entrypointStrings;
        }

        private static void AddInkJsonAssetToEntrypoints(TextAsset asset, List<InkEntrypoint> entrypoints, List<string> fullPaths, List<string> filesProcessed)
        {
            try
            {
                if (asset == null) return;
                var assetPath = AssetDatabase.GetAssetPath(asset).Substring("Assets".Length).Replace(".json", ".ink");
                var inkFullPath = Application.dataPath + assetPath;
                var rootPath = Path.GetDirectoryName(inkFullPath);
                var rootFilename = Path.GetFileName(inkFullPath);
                entrypoints.Add(new InkEntrypoint(asset.name, string.Empty, string.Empty));
                assetPath = assetPath.Substring(1);
                fullPaths.Add(assetPath);
                ProcessFile(asset, entrypoints, assetPath, rootPath, rootFilename, fullPaths, filesProcessed);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void ProcessFile(TextAsset asset, List<InkEntrypoint> entrypoints, string assetPath, 
            string rootPath, string inkFilePath, List<string> fullPaths, List<string> filesProcessed)
        {
            var inkFullPath = rootPath + "/" + inkFilePath;
            if (filesProcessed.Contains(inkFullPath)) return;
            //Debug.Log("ProcessFile " + inkFullPath);
            filesProcessed.Add(inkFullPath);
            var inkAssetPath = rootPath.Replace("\\", "/") + "/" + inkFilePath;
            inkAssetPath = inkAssetPath.Substring(Application.dataPath.Length + 1);
            var lines = System.IO.File.ReadAllLines(inkFullPath);
            var knot = string.Empty;
            foreach (var line in lines)
            {
                if (line.StartsWith("=="))
                {
                    // Knot:
                    var s = line;
                    while (s.Length > 0 && (s[0] == '=' || s[0] == ' '))
                    {
                        s = s.Substring(1);
                    }
                    while (s.Length > 0 && (s[s.Length - 1] == '=' || s[s.Length - 1] == ' '))
                    {
                        s = s.Substring(0, s.Length - 1);
                    }
                    if (s.Length > 0 && !s.StartsWith("function", System.StringComparison.OrdinalIgnoreCase))
                    {
                        knot = s;
                        entrypoints.Add(new InkEntrypoint(asset.name, knot, string.Empty));
                        fullPaths.Add(inkAssetPath + "/" + knot);
                    }
                }
                else if (line.StartsWith("= "))
                {
                    // Stitch:
                    var stitch = line.Substring(2).Trim();
                    entrypoints.Add(new InkEntrypoint(asset.name, knot, stitch));
                    fullPaths.Add(inkAssetPath + "/" + knot + "/" + stitch);
                }
                else if (line.StartsWith("INCLUDE "))
                {
                    // Include:
                    var includedFilename = line.Substring("INCLUDE ".Length).Trim();
                    ProcessFile(asset, entrypoints, assetPath, rootPath, includedFilename, fullPaths, filesProcessed);
                }
            }
        }

        private static void ResolveDotDotPaths(List<string> fullPaths)
        {
            // Convert paths like "A/B/../C/D" to "A/C/D":
            for (int i = 0; i < fullPaths.Count; i++)
            {
                if (fullPaths[i].Contains(".."))
                {
                    var folders = new List<string>(fullPaths[i].Split('/'));
                    int j = folders.Count - 2;
                    while (j > 0)
                    {
                        if (folders[j] == "..")
                        {
                            folders.RemoveRange(j - 1, 2);
                            j--;
                        }
                        j--;
                    }
                    fullPaths[i] = string.Join("/", folders);
                }
            }
        }

        private static void SimplifySubmenus(List<string> fullPaths)
        {
            for (int i = 0; i < fullPaths.Count; i++)
            {
                var submenu = fullPaths[i] + "/";
                if (fullPaths.Find(x => x.StartsWith(submenu)) != null)
                {
                    fullPaths[i] += "/<knot>";
                }
            }
        }

        private static void ReplaceSlashesInSharedSubdirectories(List<string> fullPaths)
        {
            // Find the shared subpaths for each path:
            List<int> fullPathRIndices = new List<int>();
            for (int i = 0; i < fullPaths.Count; i++)
            {
                var rindex = fullPaths[i].IndexOf(".ink/", System.StringComparison.OrdinalIgnoreCase);
                fullPathRIndices.Add(rindex);
            }

            // Replace slashes in shared subpaths:
            for (int i = 0; i < fullPaths.Count; i++)
            {
                var path = fullPaths[i];
                var rindex = fullPathRIndices[i];
                if (rindex > 0)
                {
                    var subPath = path.Substring(0, rindex).Replace("/", "\u2215");
                    fullPaths[i] = subPath + path.Substring(rindex);
                }
            }
        }

        private static bool IsUniquePath(List<string> fullPaths, int pathIndex, int rindex)
        {
            var subPath = fullPaths[pathIndex].Substring(0, rindex);
            int count = 0;
            for (int i = 0; i < fullPaths.Count; i++)
            {
                if (i == pathIndex) continue;
                if (fullPaths[i].StartsWith(subPath)) count++;
            }
            return count < 2;
        }

        public static int GetEntrypointIndex(string conversation, string path, List<InkEntrypoint> entrypoints)
        {
            var knot = string.Empty;
            var stitch = string.Empty;
            if (!string.IsNullOrEmpty(path))
            {
                var parts = path.Split('.');
                knot = parts[0];
                if (parts.Length > 1) stitch = parts[1];
            }
            for (int i = 0; i < entrypoints.Count; i++)
            {
                var entrypoint = entrypoints[i];
                if (string.Equals(entrypoint.story, conversation) &&
                    string.Equals(entrypoint.knot, knot) &&
                    string.Equals(entrypoint.stitch, stitch))
                    return i;
            }
            return -1;
        }

    }
}