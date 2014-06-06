using UnityEngine;

namespace ActiveStruts
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ActiveStrutsAddon : MonoBehaviour
    {
        private static GameObject _connector;
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
            var module = data.Modules[Config.ModuleName] as ModuleActiveStrut;
            if (module == null)
            {
                return;
            }
            if (module.IsConnectionOrigin && module.Target != null && module.Target.part.vessel == data.vessel)
            {
                module.Target.part.SetHighlightDefault();
            }
            else if (!module.IsConnectionOrigin && module.Targeter != null && module.Targeter.vessel == data.vessel)
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
            var module = data.Modules[Config.ModuleName] as ModuleActiveStrut;
            if (module == null)
            {
                return;
            }
            if (module.IsConnectionOrigin && module.Target != null && module.Target.part.vessel == data.vessel)
            {
                module.Target.part.SetHighlightColor(Color.cyan);
                module.Target.part.SetHighlight(true);
            }
            else if (!module.IsConnectionOrigin && module.Targeter != null && module.Targeter.vessel == data.vessel)
            {
                module.Targeter.part.SetHighlightColor(Color.cyan);
                module.Targeter.part.SetHighlight(true);
            }
        }

        private static bool IsValidPosition(RaycastResult raycast)
        {
            var valid = raycast.HitResult && raycast.HitCurrentVessel && raycast.DistanceFromOrigin <= Config.MaxDistance && raycast.RayAngle <= Config.MaxAngle;
            switch (Mode)
            {
                case AddonMode.Link:
                {
                    var moduleActiveStrut = raycast.HittedPart.Modules[Config.ModuleName] as ModuleActiveStrut;
                    if (moduleActiveStrut != null)
                    {
                        valid = valid && raycast.HittedPart != null && raycast.HittedPart.Modules.Contains(Config.ModuleName) && moduleActiveStrut.IsConnectionFree;
                    }
                }
                    break;
                case AddonMode.FreeAttach:
                {
                    valid = valid && raycast.HittedPart != null && !raycast.HittedPart.Modules.Contains(Config.ModuleName);
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
            GameEvents.onPartActionUICreate.Add(this.ActionMenuCreated);
            GameEvents.onPartActionUIDismiss.Add(this.ActionMenuClosed);
            _connector = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _connector.name = "ASConn";
            DestroyImmediate(_connector.collider);
            _connector.transform.localScale = new Vector3(Config.ConnectorDimension, Config.ConnectorDimension, Config.ConnectorDimension);
            var mr = _connector.GetComponent<MeshRenderer>();
            mr.name = "ASConn";
            mr.material = new Material(Shader.Find("Diffuse")); // {mainTexture = GameDatabase.Instance.GetTexture(_path + "IR_Robotic.tga", false)};
            mr.material.color = Util.MakeColorTransparent(Color.green);
            _connector.SetActive(false);
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight || Mode == AddonMode.None || CurrentTargeter == null)
            {
                _connector.SetActive(false);
                return;
            }
            var mp = Util.GetMouseWorldPosition();
            _pointToMousePosition(mp);
            var raycast = Util.PerformRaycast(CurrentTargeter.Origin.position, mp, CurrentTargeter.Origin.right);
            if (!raycast.HitResult || !raycast.HitCurrentVessel)
            {
                if (Mode == AddonMode.Link && Input.GetKeyDown(KeyCode.Mouse0))
                {
                    CurrentTargeter.AbortLink();
                }
                _connector.SetActive(false);
                return;
            }
            var validPos = _determineColor(mp, raycast);
            _processUserInput(mp, raycast, validPos);
        }

        private static bool _checkForModule(Part part)
        {
            return part.Modules.Contains(Config.ModuleName);
        }

        private static bool _determineColor(Vector3 mp, RaycastResult raycast)
        {
            var validPosition = IsValidPosition(raycast);
            var mr = _connector.GetComponent<MeshRenderer>();
            mr.material.color = Util.MakeColorTransparent(validPosition ? Color.green : Color.red);
            return validPosition;
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
            switch (Mode)
            {
                case AddonMode.Link:
                {
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        if (validPos)
                        {
                            var moduleActiveStrut = raycast.HittedPart.Modules[Config.ModuleName] as ModuleActiveStrut;
                            if (moduleActiveStrut != null)
                            {
                                moduleActiveStrut.SetAsTarget();
                            }
                        }
                    }
                    else if (Input.GetKeyDown(KeyCode.X))
                    {
                        CurrentTargeter.AbortLink();
                    }
                }
                    break;
                case AddonMode.FreeAttach:
                {
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        if (validPos)
                        {
                            CurrentTargeter.PlaceFreeAttach(raycast.HittedPart, mp, raycast.DistanceFromOrigin);
                        }
                    }
                    else if (Input.GetKeyDown(KeyCode.Mouse1))
                    {
                        Mode = AddonMode.None;
                    }
                }
                    break;
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