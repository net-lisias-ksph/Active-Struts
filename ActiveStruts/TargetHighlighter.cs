using UnityEngine;

namespace ActiveStruts
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class TargetHighlighter : MonoBehaviour
    {
        public void ActionMenuClosed(Part data)
        {
            Debug.Log("[AS] action menu closed");
            if (!_checkForModule(data))
            {
                return;
            }
            var targeter = data.Modules[ModuleActiveStrutBase.TargeterModuleName] as ModuleActiveStrutTargeter;
            if (targeter == null)
            {
                return;
            }
            if (targeter.Target == null && !targeter.HalfWayLink)
            {
                return;
            }
            if (targeter.HalfWayLink && targeter.HalfWayPartner != null)
            {
                targeter.HalfWayPartner.part.SetHighlightDefault();
                targeter.HalfWayPartner.SetTargetHighlighterOverride(false);
            }
            else if (targeter.Target != null)
            {
                targeter.Target.part.SetHighlightDefault();
                if (targeter.HasPartner)
                {
                    targeter.Target.GetPartner().SetTargetHighlighterOverride(false);
                }
            }
        }

        //must not be static
        public void ActionMenuCreated(Part data)
        {
            Debug.Log("[AS] action menu opened");
            if (!_checkForModule(data))
            {
                return;
            }
            var targeter = data.Modules[ModuleActiveStrutBase.TargeterModuleName] as ModuleActiveStrutTargeter;
            if (targeter == null)
            {
                return;
            }
            if (targeter.Target == null && !targeter.HalfWayLink)
            {
                return;
            }
            if (targeter.HalfWayLink && targeter.HalfWayPartner != null)
            {
                targeter.HalfWayPartner.part.SetHighlightColor(Color.cyan);
                targeter.HalfWayPartner.part.SetHighlight(true);
                targeter.HalfWayPartner.SetTargetHighlighterOverride(true);
            }
            else if (targeter.Target != null)
            {
                targeter.Target.part.SetHighlightColor(Color.cyan);
                targeter.Target.part.SetHighlight(true);
                if (targeter.HasPartner)
                {
                    targeter.Target.GetPartner().SetTargetHighlighterOverride(true);
                }
            }
        }

        public void OnDestroy()
        {
            GameEvents.onPartActionUICreate.Remove(this.ActionMenuCreated);
            GameEvents.onPartActionUIDismiss.Remove(this.ActionMenuClosed);
        }

        public void Start()
        {
            Debug.Log("[AS] starting target highlighter");
            GameEvents.onPartActionUICreate.Add(this.ActionMenuCreated);
            GameEvents.onPartActionUIDismiss.Add(this.ActionMenuClosed);
        }

        private static bool _checkForModule(Part part)
        {
            return (part.Modules.Contains(ModuleActiveStrutBase.TargeterModuleName));
        }

        //must not be static
    }
}