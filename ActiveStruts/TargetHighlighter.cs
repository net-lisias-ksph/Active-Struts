using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ActiveStruts
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class TargetHighlighter : MonoBehaviour
    {
        public void OnDestroy()
        {
            GameEvents.onPartActionUICreate.Remove(ActionMenuCreated);
            GameEvents.onPartActionUIDismiss.Remove(ActionMenuClosed);
        }

        public void Start()
        {
            Debug.Log("[AS] starting target highlighter");
            GameEvents.onPartActionUICreate.Add(ActionMenuCreated);
            GameEvents.onPartActionUIDismiss.Add(ActionMenuClosed);
        }

        private static bool _checkForModule(Part part)
        {
            return (part.Modules.Contains(ModuleActiveStrutBase.TargeterModuleName));
        }

        public static void ActionMenuClosed(Part data)
        {
            Debug.Log("[AS] action menu closed");
            if (!_checkForModule(data))
            {
                return;
            }
            var targeter = data.Modules[ModuleActiveStrutBase.TargeterModuleName] as ModuleActiveStrutTargeter;
            if (targeter == null || targeter.Target == null)
            {
                return;
            }
            targeter.Target.part.SetHighlight(false);
        }

        public static void ActionMenuCreated(Part data)
        {
            Debug.Log("[AS] action menu opened");
            if (!_checkForModule(data))
            {
                return;
            }
            var targeter = data.Modules[ModuleActiveStrutBase.TargeterModuleName] as ModuleActiveStrutTargeter;
            if (targeter == null || targeter.Target == null)
            {
                return;
            }
            targeter.Target.part.SetHighlightColor(Color.cyan);
            targeter.Target.part.SetHighlight(true);
        }
    }
}
