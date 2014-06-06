using UnityEngine;

namespace ActiveStruts
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ActiveStrutsOSDAddon : MonoBehaviour
    {
        // ReSharper disable once InconsistentNaming
        public void OnGUI()
        {
            OSD.Update();
        }
    }
}