using System;
using System.Collections.Generic;
using System.Linq;
using ActiveStruts.Modules;
using ActiveStruts.Util;
using UnityEngine;

namespace ActiveStruts.Addons
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class ActiveStrutsEditorAddon : MonoBehaviour
    {
        private static List<ModuleActiveStrut> _modules;
        private static List<ModuleActiveStrutFreeAttachTarget> _moduleActiveStrutFreeAttachTargets;

        public void Awake()
        {
            _modules = new List<ModuleActiveStrut>();
            _moduleActiveStrutFreeAttachTargets = new List<ModuleActiveStrutFreeAttachTarget>();
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
            if (attachedPart.Modules.Contains(Config.Instance.ModuleName))
            {
                var module = attachedPart.Modules[Config.Instance.ModuleName] as ModuleActiveStrut;
                _modules.Add(module);
                ResetActiveStrutToDefault(module);
            }
            if (attachedPart.Modules.Contains(Config.Instance.ModuleActiveStrutFreeAttachTarget))
            {
                var module = attachedPart.Modules[Config.Instance.ModuleActiveStrutFreeAttachTarget] as ModuleActiveStrutFreeAttachTarget;
                AddModuleActiveStrutFreeAttachTarget(module);
                ResetFreeAttachTargetToDefault(module);
            }
        }

        private static void ResetFreeAttachTargetToDefault(ModuleActiveStrutFreeAttachTarget module)
        {
            module.ID = Guid.NewGuid();
        }

        public static void AddModuleActiveStrutFreeAttachTarget(ModuleActiveStrutFreeAttachTarget module)
        {
            if (_moduleActiveStrutFreeAttachTargets.All(m => m.ID != module.ID))
            {
                _moduleActiveStrutFreeAttachTargets.Add(module);
            }
        }

        //must not be static
        private void ProcessPartRemove(GameEvents.HostTargetAction<Part, Part> data)
        {
            var removedPart = data.target;
            if (removedPart.Modules.Contains(Config.Instance.ModuleName))
            {
                _modules.Remove(removedPart.Modules[Config.Instance.ModuleName] as ModuleActiveStrut);
            }
            if (removedPart.Modules.Contains(Config.Instance.ModuleActiveStrutFreeAttachTarget))
            {
                _moduleActiveStrutFreeAttachTargets.Remove(removedPart.Modules[Config.Instance.ModuleActiveStrutFreeAttachTarget] as ModuleActiveStrutFreeAttachTarget);
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
            moduleToReset.FreeAttachTarget = null;
            moduleToReset.IsFreeAttached = false;
            moduleToReset.IsLinked = false;
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

        public static List<ModuleActiveStrutFreeAttachTarget> GetAllFreeAttachTargets()
        {
            return _moduleActiveStrutFreeAttachTargets;
        }
    }
}