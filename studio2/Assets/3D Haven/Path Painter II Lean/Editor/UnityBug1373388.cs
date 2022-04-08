// Copyright © 2022 3D Haven.  All Rights Reserved.
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Haven.PathPainter2
{
    internal class UnityBug1373388
    {
        const string ARTICLE_URL = "https://3dhaven.freshdesk.com/en/support/solutions/articles/43000658046";

        #region Menu items
        /// <summary>
        /// Pings the Documentations
        /// </summary>
        [MenuItem("Window/3D Haven/Path Painter II/UntyBug#1373388 Fix", false, 80)]
        public static void ShowDocumentation()
        {
            Fix();
        }

        /// <summary>
        /// Show tutorials
        /// </summary>
        [MenuItem("Window/3D Haven/Path Painter II/UntyBug#1373388 More Info...", false, 81)]
        public static void MoreInfo()
        {
            Application.OpenURL(ARTICLE_URL);
        } 
        #endregion

        private static void Fix()
        {
            int t = -1;
#if UNITY_2018_3_OR_NEWER
#if !UNITY_2019_1_OR_NEWER
#if NET_4_6
            var groupOfCurrentBuildTarget = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var andrCompLevel = PlayerSettings.GetApiCompatibilityLevel(groupOfCurrentBuildTarget);
            if (andrCompLevel == ApiCompatibilityLevel.NET_4_6)
            {
                t = 0;
            }
            else
            {
                t = 1;
            }
#else
            t = 1; 
#endif
#else
            t = 2;
#endif
#endif
            string[,] cfs = new string[,]
            {
                {"e479c83858bcc874184bb19d1eb7d50e", "f1c84fcbcac7eee44b5243199a6e2fba"},
                {"08a7fc3c592f44943a72d4a670e8ce4c", "49c05de8773584f46b3e0e7807d87b02"},
                {"5a836d66a457a224f83b3fbe9cc732b5", "470686b0ae67fa44fa5688666801e8c3"},
            };

            for (int i = 0; i < 3; i++)
            {
                if (i == t)
                {
                    continue;
                }
                Del(cfs[i, 0]);
                Del(cfs[i, 1]);
            }
        }

        /// <summary>
        /// Del
        /// </summary>
        private static void Del(string id)
        {
            string path = AssetDatabase.GUIDToAssetPath(id);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            AssetDatabase.DeleteAsset(path);
        }
    }
}
#endif
