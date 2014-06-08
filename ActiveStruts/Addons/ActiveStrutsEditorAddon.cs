using System;
using System.Collections.Generic;
using ActiveStruts.Modules;
using ActiveStruts.Util;
using UnityEngine;

namespace ActiveStruts.Addons
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class ActiveStrutsEditorAddon : MonoBehaviour
    {
        private static List<ModuleActiveStrut> _modules;

        public void Awake()
        {
            _modules = new List<ModuleActiveStrut>();
        }

        public static List<ModuleActiveStrut> GetAllActiveStruts()
        {
            return _modules;
        }

        public void OnDestroy()
        {
            GameEvents.onPartAttach.Remove(this.ProcessPartAttach);
            GameEvents.onPartRemove.Remove(this.ProcessPartRemove);
        }

        //must not be static
        private void ProcessPartAttach(GameEvents.HostTargetAction<Part, Part> data)
        {
            var attachedPart = data.host;
            if (!attachedPart.Modules.Contains(Config.Instance.ModuleName))
            {
                return;
            }
            var module = attachedPart.Modules[Config.Instance.ModuleName] as ModuleActiveStrut;
            _modules.Add(module);
            ResetActiveStrutToDefault(module);
        }

        private void ProcessPartRemove(GameEvents.HostTargetAction<Part, Part> data)
        {
            var removedPart = data.target;
            if (removedPart.Modules.Contains(Config.Instance.ModuleName))
            {
                _modules.Remove(removedPart.Modules[Config.Instance.ModuleName] as ModuleActiveStrut);
            }
        }

        public static void ResetActiveStrutToDefault(ModuleActiveStrut moduleToReset)
        {
            moduleToReset.Target = null;
            moduleToReset.Targeter = null;
            moduleToReset.IsConnectionOrigin = false;
            moduleToReset.IsFreeAttached = false;
            moduleToReset.Mode = Mode.Unlinked;
            moduleToReset.IsHalfWayExtended = false;
            moduleToReset.Id = Guid.NewGuid().ToString();
            moduleToReset.LinkType = LinkType.None;
            moduleToReset.OldTargeter = null;
            moduleToReset.FreeAttachDistance = 0f;
            moduleToReset.IsFreeAttached = false;
            moduleToReset.IsLinked = false;
            moduleToReset.FreeAttachPoint = Vector3.zero;
            moduleToReset.FreeAttachTargetLocalVector = Vector3.zero;
        }

        public void Start()
        {
            GameEvents.onPartAttach.Add(this.ProcessPartAttach);
            GameEvents.onPartRemove.Add(this.ProcessPartRemove);
        }

        public void Update()
        {
            foreach (var moduleActiveStrut in _modules)
            {
                moduleActiveStrut.OnUpdate();
            }
        }
    }
}