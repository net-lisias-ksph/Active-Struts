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
        private static bool _modulesFirstLoadFlag;
        private static bool _moduleTargetFirstLoadFlag;
        private static Dictionary<string, ModuleActiveStrut> _originalModules;
        private static Dictionary<string, ModuleActiveStrutFreeAttachTarget> _originalAttachTargets;

        public static void AddModuleActiveStrut(ModuleActiveStrut module)
        {
            var original = !_originalModules.ContainsKey(module.Id) || _originalModules[module.Id] == module;
            if (!original)
            {
                ResetActiveStrutToDefault(module);
                _originalModules.Add(module.Id, module);
            }
            _modules.Add(module);
        }

        public static void AddModuleActiveStrutFreeAttachTarget(ModuleActiveStrutFreeAttachTarget module)
        {
            var original = !_originalAttachTargets.ContainsKey(module.Id) || _originalAttachTargets[module.Id] == module;
            if (!original)
            {
                ResetFreeAttachTargetToDefault(module);
                _originalAttachTargets.Add(module.Id, module);
            }
            _moduleActiveStrutFreeAttachTargets.Add(module);
        }

        public void Awake()
        {
            _modules = new List<ModuleActiveStrut>();
            _moduleActiveStrutFreeAttachTargets = new List<ModuleActiveStrutFreeAttachTarget>();
            _moduleTargetFirstLoadFlag = true;
            _modulesFirstLoadFlag = true;
            _originalModules = new Dictionary<string, ModuleActiveStrut>();
            _originalAttachTargets = new Dictionary<string, ModuleActiveStrutFreeAttachTarget>();
            //GameEvents.onPartAttach.Add(this.ProcessPartAttach);
            //GameEvents.onPartRemove.Add(this.ProcessPartRemove);
        }

        public static List<ModuleActiveStrut> GetAllActiveStruts()
        {
            if (_modulesFirstLoadFlag)
            {
                var partList = Util.Util.ListEditorParts(true);
                foreach (var module in partList.Where(p => p.Modules.Contains(Config.Instance.ModuleName)).Select(p => p.Modules[Config.Instance.ModuleName] as ModuleActiveStrut).ToList())
                {
                    AddModuleActiveStrut(module);
                }
                _modulesFirstLoadFlag = false;
            }
            return _modules;
        }

        public static List<ModuleActiveStrutFreeAttachTarget> GetAllFreeAttachTargets()
        {
            if (_moduleTargetFirstLoadFlag)
            {
                var partList = Util.Util.ListEditorParts(true);
                foreach (
                    var module in
                        partList.Where(p => p.Modules.Contains(Config.Instance.ModuleActiveStrutFreeAttachTarget)).Select(p => p.Modules[Config.Instance.ModuleActiveStrutFreeAttachTarget] as ModuleActiveStrutFreeAttachTarget).ToList())
                {
                    AddModuleActiveStrutFreeAttachTarget(module);
                }
                _moduleTargetFirstLoadFlag = false;
            }
            return _moduleActiveStrutFreeAttachTargets;
        }

        public static void RemoveModuleActiveStrut(ModuleActiveStrut module)
        {
            var original = !_originalModules.ContainsKey(module.Id) || _originalModules[module.Id] == module;
            if (original)
            {
                _originalModules.Remove(module.Id);
                _modules.Remove(module);
            }
        }

        public void OnDestroy()
        {
            InputLockManager.RemoveControlLock(Config.Instance.EditorInputLockId);
        }

        ////must not be static
        //private void ProcessPartAttach(GameEvents.HostTargetAction<Part, Part> data)
        //{
        //    var attachedPart = data.host;
        //    if (attachedPart.Modules.Contains(Config.Instance.ModuleName))
        //    {
        //        var module = attachedPart.Modules[Config.Instance.ModuleName] as ModuleActiveStrut;
        //        _modules.Add(module);
        //        ResetActiveStrutToDefault(module);
        //    }
        //    if (attachedPart.Modules.Contains(Config.Instance.ModuleActiveStrutFreeAttachTarget))
        //    {
        //        var module = attachedPart.Modules[Config.Instance.ModuleActiveStrutFreeAttachTarget] as ModuleActiveStrutFreeAttachTarget;
        //        AddModuleActiveStrutFreeAttachTarget(module);
        //        ResetFreeAttachTargetToDefault(module);
        //    }
        //}

        public static void RemoveModuleActiveStrutFreeAttachTarget(ModuleActiveStrutFreeAttachTarget module)
        {
            var original = !_originalAttachTargets.ContainsKey(module.Id) || _originalAttachTargets[module.Id] == module;
            if (original)
            {
                _originalAttachTargets.Remove(module.Id);
                _moduleActiveStrutFreeAttachTargets.Remove(module);
            }
        }

        ////must not be static
        //private void ProcessPartRemove(GameEvents.HostTargetAction<Part, Part> data)
        //{
        //    var removedPart = data.target;
        //    if (removedPart.Modules.Contains(Config.Instance.ModuleName))
        //    {
        //        _modules.Remove(removedPart.Modules[Config.Instance.ModuleName] as ModuleActiveStrut);
        //    }
        //    if (removedPart.Modules.Contains(Config.Instance.ModuleActiveStrutFreeAttachTarget))
        //    {
        //        _moduleActiveStrutFreeAttachTargets.Remove(removedPart.Modules[Config.Instance.ModuleActiveStrutFreeAttachTarget] as ModuleActiveStrutFreeAttachTarget);
        //    }
        //}

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

        private static void ResetFreeAttachTargetToDefault(ModuleActiveStrutFreeAttachTarget module)
        {
            module.ID = Guid.NewGuid();
        }

        public void Start()
        {
            //_populateLists();
        }

        //private void _populateLists()
        //{
        //}

        public void Update()
        {
            //Debug.Log("[AS] editor addon has " + GetAllActiveStruts().Count + " active struts and " + GetAllFreeAttachTargets().Count + " free attach targets.");
            foreach (var moduleActiveStrut in GetAllActiveStruts())
            {
                moduleActiveStrut.OnUpdate();
            }
        }
    }
}