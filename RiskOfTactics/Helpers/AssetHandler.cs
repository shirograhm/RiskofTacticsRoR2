using R2API;
using System.IO;
using UnityEngine;

namespace RiskOfTactics.Helpers
{
    public static class AssetHandler
    {
        public static AssetBundle bundle;
        public const string bundleName = "rotassets";

        public static string AssetBundlePath
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(RiskOfTactics.PInfo.Location), bundleName);
            }
        }

        public static void Init()
        {
            bundle = AssetBundle.LoadFromFile(AssetBundlePath);
        }
    }
}
