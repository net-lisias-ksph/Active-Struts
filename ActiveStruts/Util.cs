using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ActiveStruts
{
    public class RaycastResult
    {
        public float DistanceFromOrigin { get; set; }
        public RaycastHit Hit { get; set; }
        public bool HitCurrentVessel { get; set; }
        public bool HitResult { get; set; }
        public Part HittedPart { get; set; }
        public Ray Ray { get; set; }
        public float RayAngle { get; set; }
    }

    public class FreeAttachTargetCheck
    {
        public bool HitResult { get; set; }
        public Part TargetPart { get; set; }
    }

    public static class Util
    {
        public static bool AnyTargetersConnected(this ModuleActiveStrut target)
        {
            return FlightGlobals.ActiveVessel.GetAllActiveStruts().Any(m => !m.IsTargetOnly && m.Mode == Mode.Linked && m.Target != null && m.Target == target);
        }

        public static FreeAttachTargetCheck CheckFreeAttachPoint(this ModuleActiveStrut origin)
        {
            var raycast = PerformRaycast(origin.Origin.position, origin.FreeAttachPoint, origin.Origin.right);
            if (raycast.HitResult)
            {
                var distOk = DistanceInToleranceRange(origin.FreeAttachDistance, raycast.DistanceFromOrigin);
                return new FreeAttachTargetCheck
                       {
                           TargetPart = raycast.HittedPart,
                           HitResult = distOk && raycast.HitCurrentVessel
                       };
            }
            return new FreeAttachTargetCheck
                   {
                       TargetPart = null,
                       HitResult = false
                   };
        }

        public static bool DistanceInToleranceRange(float savedDistance, float currentDistance)
        {
            return currentDistance >= savedDistance - Config.FreeAttachDistanceTolerance && currentDistance <= savedDistance + Config.FreeAttachDistanceTolerance && currentDistance <= Config.MaxDistance;
        }

        public static List<ModuleActiveStrut> GetAllActiveStruts(this Vessel vessel)
        {
            return vessel.Parts.Where(p => p.Modules.Contains(Config.ModuleName)).Select(p => p.Modules[Config.ModuleName] as ModuleActiveStrut).ToList();
        }

        public static List<ModuleActiveStrut> GetAllPossibleTargets(this ModuleActiveStrut origin)
        {
            return origin.part.vessel.GetAllActiveStruts().Where(m => m.ID != origin.ID && origin.IsPossibleTarget(m)).Select(m => m).ToList();
        }

        public static float GetJointStrength(this LinkType type)
        {
            switch (type)
            {
                case LinkType.None:
                {
                    return 0;
                }
                case LinkType.Normal:
                {
                    return Config.NormalJointStrength;
                }
                case LinkType.Maximal:
                {
                    return Config.MaximalJointStrength;
                }
                case LinkType.Weak:
                {
                    return Config.WeakJointStrength;
                }
            }
            return 0;
        }

        public static Vector3 GetMouseWorldPosition()
        {
            var ray = HighLogic.LoadedSceneIsFlight ? FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition) : Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            return Physics.Raycast(ray, out hit, Config.MaxDistance) ? hit.point : Vector3.zero;
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

        public static ModuleActiveStrut GetStrutById(this Vessel vessel, Guid id)
        {
            return vessel.GetAllActiveStruts().Find(m => m.ID == id);
        }

        public static bool IsPossibleFreeAttachTarget(this ModuleActiveStrut origin, Vector3 mousePosition)
        {
            var raycast = PerformRaycast(origin.Origin.position, mousePosition, origin.Origin.right);
            return raycast.HitResult && raycast.HitCurrentVessel && raycast.DistanceFromOrigin <= Config.MaxDistance && raycast.RayAngle <= Config.MaxAngle;
        }

        public static bool IsPossibleTarget(this ModuleActiveStrut origin, ModuleActiveStrut possibleTarget)
        {
            if (!possibleTarget.IsConnectionFree)
            {
                return false;
            }
            Debug.Log("[AS] now raycasting " + possibleTarget.ID);
            var raycast = PerformRaycast(origin.Origin.position, possibleTarget.Origin.position, origin.Origin.right);
            Debug.Log("[AS] now checking: " + string.Format("hitresult={0} hittedpart={1} distance={2} hitcurrentvessel={3}", raycast.HitResult, raycast.HittedPart.name, raycast.DistanceFromOrigin, raycast.HitCurrentVessel));
            return raycast.HitResult && raycast.HittedPart == possibleTarget.part && raycast.DistanceFromOrigin <= Config.MaxDistance && raycast.RayAngle <= Config.MaxAngle && raycast.HitCurrentVessel;
        }

        public static Color MakeColorTransparent(Color color)
        {
            var rgba = GetRgbaFromColor(color);
            return new Color(rgba[0], rgba[1], rgba[2], Config.ColorTransparency);
        }

        public static Part PartFromHit(this RaycastHit hit)
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

        public static RaycastResult PerformRaycast(Vector3 origin, Vector3 target, Vector3 originUp)
        {
            Debug.Log("[AS] raycast: from " + origin.ToDebugString() + " to " + target.ToDebugString() + " with originup " + originUp.ToDebugString());
            RaycastHit info;
            var dir = (target - origin).normalized;
            var ray = new Ray(origin, dir);
            var hit = Physics.Raycast(ray, out info, Config.MaxDistance + 1);
            var hittedPart = hit ? PartFromHit(info) : null;
            var angle = Vector3.Angle(dir, originUp);
            Debug.Log("[AS] raycast result: " + hit + " with angle " + angle);
            return new RaycastResult
                   {
                       DistanceFromOrigin = info.distance,
                       Hit = info,
                       HittedPart = hittedPart,
                       HitResult = hit,
                       Ray = ray,
                       RayAngle = angle,
                       HitCurrentVessel = hittedPart != null && hittedPart.vessel == FlightGlobals.ActiveVessel
                   };
        }

        public static void ResetAllFromTargeting()
        {
            foreach (var moduleActiveStrut in FlightGlobals.ActiveVessel.GetAllActiveStruts().Where(m => m.Mode == Mode.Target))
            {
                moduleActiveStrut.Mode = Mode.Unlinked;
                moduleActiveStrut.part.SetHighlightDefault();
                moduleActiveStrut.UpdateGui();
                moduleActiveStrut.Targeter = moduleActiveStrut.OldTargeter;
            }
        }

        public static string ToDebugString(this Vector3 vector)
        {
            return string.Format("[x:{0}, y:{1}, z:{2}]", vector.x, vector.y, vector.z);
        }
    }
}