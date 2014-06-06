using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ActiveStruts
{
    

    public static class ASUtil
    {
        public static Tuple<bool, ModuleActiveStrutBase, ModuleActiveStrutBase> GetActiveStrut(this Vessel v, Guid targetId)
        {
            foreach (var p in from p in v.Parts
                              let targeterFlag = p.Modules.Contains(ModuleActiveStrutBase.TargeterModuleName)
                              let targetFlag = p.Modules.Contains(ModuleActiveStrutBase.TargetModuleName)
                              where targeterFlag || targetFlag
                              where
                                  (targeterFlag && ((p.Modules[ModuleActiveStrutBase.TargeterModuleName] as ModuleActiveStrutBase) != null && (p.Modules[ModuleActiveStrutBase.TargeterModuleName] as ModuleActiveStrutBase).ID == targetId)) ||
                                  (targetFlag && ((p.Modules[ModuleActiveStrutBase.TargetModuleName] as ModuleActiveStrutBase) != null && (p.Modules[ModuleActiveStrutBase.TargetModuleName] as ModuleActiveStrutBase).ID == targetId))
                              select p)
            {
                ModuleActiveStrutBase target = null, targeter = null;
                if (p.Modules.Contains(ModuleActiveStrutBase.TargetModuleName))
                {
                    target = p.Modules[ModuleActiveStrutBase.TargetModuleName] as ModuleActiveStrutBase;
                }
                if (p.Modules.Contains(ModuleActiveStrutBase.TargeterModuleName))
                {
                    targeter = p.Modules[ModuleActiveStrutBase.TargeterModuleName] as ModuleActiveStrutBase;
                }
                return Tuple.New(true, target, targeter);
            }
            return Tuple.New<bool, ModuleActiveStrutBase, ModuleActiveStrutBase>(false, null, null);
        }

        public static List<ModuleActiveStrutBase> GetAllActiveStrutsModules(Vessel vessel)
        {
            var partList = (from part in vessel.parts
                            where part.Modules.Contains(ModuleActiveStrutBase.TargetModuleName) || part.Modules.Contains(ModuleActiveStrutBase.TargeterModuleName)
                            select part);
            var moduleList = new List<ModuleActiveStrutBase>();
            foreach (var part in partList)
            {
                if (part.Modules.Contains(ModuleActiveStrutBase.TargetModuleName))
                {
                    moduleList.Add(part.Modules[ModuleActiveStrutBase.TargetModuleName] as ModuleActiveStrutBase);
                }
                if (part.Modules.Contains(ModuleActiveStrutBase.TargeterModuleName))
                {
                    moduleList.Add(part.Modules[ModuleActiveStrutBase.TargeterModuleName] as ModuleActiveStrutBase);
                }
            }
            return moduleList;
        }

        public static MousePositionData GetCurrentMousePositionData(Vector3? refOrigin, Vector3? refOriginUpVector)
        {
            var ray = HighLogic.LoadedSceneIsFlight ? FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition) : Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, ModuleActiveStrutBase.MaxDistance))
            {
                return null;
            }
            var hitGoFlag = hit.transform != null && hit.transform.gameObject != null;
            var rayUpVector = ray.direction;
            var mpd = new MousePositionData
                      {
                          OriginValid = refOrigin != null,
                          Ray = ray,
                          Hit = hit,
                          PartHitPosition = hitGoFlag ? hit.transform.position : Vector3.zero,
                          RayDistance = hit.distance,
                          ReferenceOrigin = refOrigin ?? Vector3.zero,
                          ExactHitPosition = hit.point,
                          DistanceFromReferenceOriginPart = Vector3.Distance(refOrigin ?? Vector3.zero, hitGoFlag ? hit.transform.position : Vector3.zero),
                          DistanceFromReferenceOriginExact = Vector3.Distance(refOrigin ?? Vector3.zero, hit.point),
                          HittedPart = hitGoFlag ? hit.transform.gameObject.GetComponent<Part>() : null,
                          //AngleFromOriginPart = Vector3.Angle(refOrigin ?? Vector3.zero, hitGoFlag ? hit.transform.position : Vector3.zero),
                          AngleFromOriginPart = Vector3.Angle(refOriginUpVector ?? Vector3.zero, hitGoFlag ? hit.transform.up : Vector3.zero),
                          //AngleFromOriginExact = Vector3.Angle(refOrigin ?? Vector3.zero, hit.point)
                          AngleFromOriginExact = Vector3.Angle(refOriginUpVector ?? Vector3.zero, rayUpVector)
                      };
            mpd.HitCurrentVessel = mpd.HittedPart != null && mpd.HittedPart.vessel == FlightGlobals.ActiveVessel;
            return mpd;
        }

        public static float[] GetRgbaFromColor(Color color)
        {
            var ret = new float[4];
            ret[0] = color.r;
            ret[1] = color.g;
            ret[2] = color.b;
            ret[3] = color.a;
            return ret;
        }

        public static void HideEventsOnAllTargeters(Guid exceptionId)
        {
            var targeters = GetAllActiveStrutsModules(FlightGlobals.ActiveVessel).Where(m => m is ModuleActiveStrutTargeter && m.ID != exceptionId).Select(m => m as ModuleActiveStrutTargeter).ToList();
            foreach (var targeter in targeters)
            {
                targeter.SuppressAllEvents(true);
                targeter.UpdateGui();
            }
        }

        public static Part PartFromHit(RaycastHit hit)
        {
            var go = hit.collider.gameObject;
            var p = Part.FromGO(go);
            while (p == null)
            {
                if (go.transform.parent != null && go.transform.parent.gameObject != null)
                {
                    go = go.transform.parent.gameObject;
                }
                else
                {
                    break;
                }
                p = Part.FromGO(go);
            }
            return p;
        }

        public static void RestoreEventsOnAllTargeters()
        {
            var targeters = GetAllActiveStrutsModules(FlightGlobals.ActiveVessel).OfType<ModuleActiveStrutTargeter>().ToList();
            foreach (var targeter in targeters)
            {
                targeter.SuppressAllEvents(false);
                targeter.UpdateGui();
            }
        }

        public class Tuple<T1, T2>
        {
            public T1 Item1 { get; private set; }
            public T2 Item2 { get; private set; }

            internal Tuple(T1 first, T2 second)
            {
                this.Item1 = first;
                this.Item2 = second;
            }
        }

        public class Tuple<T1, T2, T3> : Tuple<T1, T2>
        {
            public T3 Item3 { get; private set; }

            internal Tuple(T1 first, T2 second, T3 third) : base(first, second)
            {
                this.Item3 = third;
            }
        }

        public static class Tuple
        {
            public static Tuple<T1, T2> New<T1, T2>(T1 first, T2 second)
            {
                var tuple = new Tuple<T1, T2>(first, second);
                return tuple;
            }

            public static Tuple<T1, T2, T3> New<T1, T2, T3>(T1 first, T2 second, T3 third)
            {
                var tuple = new Tuple<T1, T2, T3>(first, second, third);
                return tuple;
            }
        }
    }
}