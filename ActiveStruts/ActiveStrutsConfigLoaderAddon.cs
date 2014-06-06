using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ActiveStruts
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class ActiveStrutsConfigLoaderAddon : MonoBehaviour
    {
        public void Awake()
        {
            Config.Load();
        }

        public void OnDestroy()
        {
            Config.Save();
        }
    }
}