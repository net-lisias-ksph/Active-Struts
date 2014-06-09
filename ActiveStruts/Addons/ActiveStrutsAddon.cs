using System;
using System.Collections.Generic;
using System.Linq;
using ActiveStruts.Modules;
using ActiveStruts.Util;
using UnityEngine;

namespace ActiveStruts.Addons
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class ActiveStrutsAddon : MonoBehaviour
    {
        private const int TargetHighlightRemoveInterval = 180;
        private static GameObject _connector;
        private bool _resetAllHighlighting;
        private int _targetHighlightRemoveCounter;
        private List<Part> _targetHighlightedParts;
        public static ModuleActiveStrut CurrentTargeter { get; set; }
        public static AddonMode Mode { get; set; }
        public static Vector3 Origin { get; set; }

        //must not be static
        private void ActionMenuClosed(Part data)
        {
            if (!_checkForModule(data))
            {
                return;
            }
            var module = data.Modules[Config.Instance.ModuleName] as ModuleActiveStrut;
            if (module == null)
            {
                return;
            }
            if (module.IsConnectionOrigin && module.Target != null && (HighLogic.LoadedSceneIsEditor || module.Target.part.vessel == data.vessel))
            {
                module.Target.part.SetHighlightDefault();
            }
            else if (module.Target != null && (!module.IsConnectionOrigin && module.Targeter != null && (HighLogic.LoadedSceneIsEditor || module.Target.part.vessel == data.vessel)))
            {
                module.Targeter.part.SetHighlightDefault();
            }
        }

        //must not be static
        private void ActionMenuCreated(Part data)
        {
            if (!_checkForModule(data))
            {
                return;
            }
            var module = data.Modules[Config.Instance.ModuleName] as ModuleActiveStrut;
            if (module == null)
            {
                return;
            }
            if (module.IsConnectionOrigin && module.Target != null && (HighLogic.LoadedSceneIsEditor || module.Target.part.vessel == data.vessel))
            {
                module.Target.part.SetHighlightColor(Color.cyan);
                module.Target.part.SetHighlight(true);
                this._targetHighlightedParts.Add(module.Target.part);
            }
            else if (module.Targeter != null && !module.IsConnectionOrigin && (HighLogic.LoadedSceneIsEditor || module.Targeter.part.vessel == data.vessel))
            {
                if (module.IsTargetOnly)
                {
                    return;
                }
                module.Targeter.part.SetHighlightColor(Color.cyan);
                module.Targeter.part.SetHighlight(true);
                this._targetHighlightedParts.Add(module.Targeter.part);
            }
        }

        public void Awake()
        {
            if (!(HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight))
            {
                return;
            }
            this._targetHighlightRemoveCounter = TargetHighlightRemoveInterval;
            this._targetHighlightedParts = new List<Part>();
            GameEvents.onPartActionUICreate.Add(this.ActionMenuCreated);
            GameEvents.onPartActionUIDismiss.Add(this.ActionMenuClosed);
        }

        private static bool IsValidPosition(RaycastResult raycast)
        {
            var valid = raycast.HitResult && raycast.HittedPart != null && raycast.HitCurrentVessel && raycast.DistanceFromOrigin <= Config.Instance.MaxDistance && raycast.RayAngle <= Config.Instance.MaxAngle;
            switch (Mode)
            {
                case AddonMode.Link:
                {
                    if (raycast.HittedPart != null && raycast.HittedPart.Modules.Contains(Config.Instance.ModuleName))
                    {
                        var moduleActiveStrut = raycast.HittedPart.Modules[Config.Instance.ModuleName] as ModuleActiveStrut;
                        if (moduleActiveStrut != null)
                        {
                            valid = valid && raycast.HittedPart != null && raycast.HittedPart.Modules.Contains(Config.Instance.ModuleName) && moduleActiveStrut.IsConnectionFree;
                        }
                    }
                }
                    break;
                case AddonMode.FreeAttach:
                {
                    valid = valid && !raycast.HittedPart.Modules.Contains(Config.Instance.ModuleName) && raycast.HittedPart.Modules.Contains(Config.Instance.ModuleActiveStrutFreeAttachTarget);
                    if (valid)
                    {
                        var res = Util.Util.PerformRaycast(CurrentTargeter.Origin.position, raycast.HittedPart.transform.position, CurrentTargeter.Origin.right);
                        valid = res.HitResult && res.HittedPart != null && res.HittedPart == raycast.HittedPart && res.DistanceFromOrigin <= Config.Instance.MaxDistance && res.RayAngle <= Config.Instance.MaxAngle;
                        raycast.HitResult = res.HitResult;
                        raycast.HittedPart = res.HittedPart;
                        raycast.HitCurrentVessel = res.HitCurrentVessel;
                        raycast.DistanceFromOrigin = res.DistanceFromOrigin;
                        raycast.RayAngle = res.RayAngle;
                        raycast.Hit = res.Hit;
                        raycast.Ray = res.Ray;
                    }
                }
                    break;
            }
            return valid;
        }

        public void OnDestroy()
        {
            GameEvents.onPartActionUICreate.Remove(this.ActionMenuCreated);
            GameEvents.onPartActionUIDismiss.Remove(this.ActionMenuClosed);
        }

        // ReSharper disable once InconsistentNaming
        public void OnGUI()
        {
            OSD.Update();
        }

        public void Start()
        {
            if (!(HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight))
            {
                return;
            }
            _connector = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _connector.name = "ASConn";
            DestroyImmediate(_connector.collider);
            _connector.transform.localScale = new Vector3(Config.Instance.ConnectorDimension, Config.Instance.ConnectorDimension, Config.Instance.ConnectorDimension);
            var mr = _connector.GetComponent<MeshRenderer>();
            mr.name = "ASConn";
            mr.material = new Material(Shader.Find("Transparent/Diffuse")) {color = Util.Util.MakeColorTransparent(Color.green)};
            _connector.SetActive(false);
        }

        public void Update()
        {
            if (!(HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight))
            {
                return;
            }
            if (HighLogic.LoadedSceneIsEditor)
            {
                try
                {
                    foreach (var activeStrut in Util.Util.GetAllActiveStruts())
                    {
                        activeStrut.OnUpdate();
                    }
                }
                catch (NullReferenceException)
                {
                    //happens onload, ignore for the moment...
                }
            }
            if (this._targetHighlightRemoveCounter > 0)
            {
                this._targetHighlightRemoveCounter--;
            }
            else
            {
                this._targetHighlightRemoveCounter = TargetHighlightRemoveInterval;
                var resetList = new List<Part>();
                if (this._targetHighlightedParts != null)
                {
                    resetList = this._targetHighlightedParts.Where(targetHighlightedPart => targetHighlightedPart != null).ToList();
                }
                foreach (var targetHighlightedPart in resetList)
                {
                    targetHighlightedPart.SetHighlightDefault();
                }
                if (this._targetHighlightedParts != null)
                {
                    this._targetHighlightedParts.Clear();
                }
            }
            if (Mode == AddonMode.None || CurrentTargeter == null)
            {
                if (this._resetAllHighlighting)
                {
                    this._resetAllHighlighting = false;
                    foreach (var moduleActiveStrut in Util.Util.GetAllActiveStruts())
                    {
                        moduleActiveStrut.part.SetHighlightDefault();
                    }
                }
                _connector.SetActive(false);
                return;
            }
            this._resetAllHighlighting = true;
            if (Mode == AddonMode.Link)
            {
                _highlightCurrentTargets();
            }
            var mp = Util.Util.GetMouseWorldPosition();
            _pointToMousePosition(mp);
            var raycast = Util.Util.PerformRaycast(CurrentTargeter.Origin.position, mp, CurrentTargeter.Origin.right);
            if (!raycast.HitResult || !raycast.HitCurrentVessel)
            {
                var handled = false;
                if (Mode == AddonMode.Link && Input.GetKeyDown(KeyCode.Mouse0))
                {
                    CurrentTargeter.AbortLink();
                    CurrentTargeter.UpdateGui();
                    handled = true;
                }
                if (Mode == AddonMode.FreeAttach && Input.GetKeyDown(KeyCode.X))
                {
                    Mode = AddonMode.None;
                    CurrentTargeter.UpdateGui();
                    handled = true;
                }
                _connector.SetActive(false);
                if (HighLogic.LoadedSceneIsEditor && handled)
                {
                    Input.ResetInputAxes();
                    InputLockManager.RemoveControlLock(Config.Instance.EditorInputLockId);
                }
                return;
            }
            var validPos = _determineColor(mp, raycast);
            _processUserInput(mp, raycast, validPos);
        }

        private static bool _checkForModule(Part part)
        {
            return part.Modules.Contains(Config.Instance.ModuleName);
        }

        private static bool _determineColor(Vector3 mp, RaycastResult raycast)
        {
            var validPosition = IsValidPosition(raycast);
            var mr = _connector.GetComponent<MeshRenderer>();
            mr.material.color = Util.Util.MakeColorTransparent(validPosition ? Color.green : Color.red);
            return validPosition;
        }

        private static void _highlightCurrentTargets()
        {
            var targets = Util.Util.GetAllActiveStruts().Where(m => m.Mode == Util.Mode.Target).Select(m => m.part).ToList();
            foreach (var part in targets)
            {
                part.SetHighlightColor(Color.green);
                part.SetHighlight(true);
            }
        }

        private static void _pointToMousePosition(Vector3 mp)
        {
            _connector.SetActive(true);
            var trans = _connector.transform;
            trans.position = CurrentTargeter.Origin.position;
            trans.LookAt(mp);
            trans.localScale = new Vector3(trans.position.x, trans.position.y, 1);
            var dist = Vector3.Distance(Vector3.zero, trans.InverseTransformPoint(mp))/2.0f;
            trans.localScale = new Vector3(0.05f, dist, 0.05f);
            trans.Rotate(new Vector3(0, 0, 1), 90f);
            trans.Rotate(new Vector3(1, 0, 0), 90f);
            trans.Translate(new Vector3(0f, dist, 0f));
        }

        private static void _processUserInput(Vector3 mp, RaycastResult raycast, bool validPos)
        {
            var handled = false;
            switch (Mode)
            {
                case AddonMode.Link:
                {
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        if (validPos && raycast.HittedPart.Modules.Contains(Config.Instance.ModuleName))
                        {
                            var moduleActiveStrut = raycast.HittedPart.Modules[Config.Instance.ModuleName] as ModuleActiveStrut;
                            if (moduleActiveStrut != null)
                            {
                                moduleActiveStrut.SetAsTarget();
                                handled = true;
                            }
                        }
                    }
                    else if (Input.GetKeyDown(KeyCode.X))
                    {
                        CurrentTargeter.AbortLink();
                        handled = true;
                    }
                }
                    break;
                case AddonMode.FreeAttach:
                {
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        if (validPos)
                        {
                            CurrentTargeter.PlaceFreeAttach(raycast.HittedPart, raycast.Hit.point);
                            handled = true;
                        }
                    }
                    else if (Input.GetKeyDown(KeyCode.X))
                    {
                        Mode = AddonMode.None;
                        handled = true;
                    }
                }
                    break;
            }
            if (HighLogic.LoadedSceneIsEditor && handled)
            {
                Input.ResetInputAxes();
                InputLockManager.RemoveControlLock(Config.Instance.EditorInputLockId);
            }
        }
    }

    public enum AddonMode
    {
        FreeAttach,
        Link,
        None
    }
}