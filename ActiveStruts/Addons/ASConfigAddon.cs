using ActiveStruts.Util;
using UnityEngine;

namespace ActiveStruts.Addons
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class ASConfigAddon : MonoBehaviour
    {
        public static Config Config { get; private set; }

        public void Awake()
        {
            //System.IO.Directory.GetCurrentDirectory();

            Config = new Config();
            Config.Load();
        }

        public void OnDestroy()
        {
            Config.Save();
            Config = null;
        }
    }
}