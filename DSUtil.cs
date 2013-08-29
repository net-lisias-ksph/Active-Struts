using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DockingStrut
{
    public enum DSMode
    {
        UNLINKED,
        LINKED,
        TARGETING,
        TARGET,
        INVALID
    }

    public static class DSUtil
    {
        public static Part PartFromHit(RaycastHit hit)
        {
            GameObject go = hit.collider.gameObject;
            Part p = null;

            while (p == null)
            {
                p = Part.FromGO(go);

                if (go.transform.parent != null && go.transform.parent.gameObject != null)
                    go = go.transform.parent.gameObject;
                else
                    break;
            }
            return p;
        }

        public static bool GetDS(this Vessel v, Guid TargetID, out ModuleDockingStrut Target) {
            
            foreach (Part p in v.Parts) {
                if (p.Modules.Contains("ModuleDockingStrut") && (p.Modules["ModuleDockingStrut"] as ModuleDockingStrut).ID == TargetID) {
                    Target = (p.Modules["ModuleDockingStrut"] as ModuleDockingStrut);
                    return true;
                }
            }
            Target = null;
            return false;
        }

        public static bool checkPossibleTarget(ModuleDockingStrut Origin, ModuleDockingStrut Target)
        {
            try
            {
                float distance = Vector3.Distance(Target.part.transform.position, Origin.part.transform.position);
                if (distance > Origin.MaxDistance)
                    return false;

                RaycastHit info = new RaycastHit();
                Vector3 start = Origin.rayCastOrigin;
                Vector3 dir = (Target.strutTarget - start).normalized;


                bool hit = Physics.Raycast(new Ray(start, dir), out info, Origin.MaxDistance + 1);

                Part tmpp = DSUtil.PartFromHit(info);
                if (hit && tmpp == Target.part)
                    hit = false;

                return !hit;
            }
            catch
            {
                return false;
            }
        }

        public static void setPossibleTarget(ModuleDockingStrut Origin, ModuleDockingStrut Target)
        {
            float distance = Vector3.Distance(Target.part.transform.position, Origin.part.transform.position);
            if (distance > Origin.MaxDistance)
            {
                Target.SetErrorMessage("Out of range by " + Math.Round(distance - Origin.MaxDistance, 2) + "m");
                return;
            }

            RaycastHit info = new RaycastHit();
            Vector3 start = Origin.rayCastOrigin;
            Vector3 dir = (Target.strutTarget - start).normalized;
            
            bool hit = Physics.Raycast(new Ray(start, dir), out info, Origin.MaxDistance + 1);

            Part tmpp = PartFromHit(info);
            if (hit && tmpp == Target.part)
                hit = false;

            if (hit)
            {
                Target.SetErrorMessage("Obstructed by " + tmpp.name);
                return;
            }

            Target.mode = DSMode.TARGET;
            Target.TargeterDS = Origin;
            foreach (BaseEvent e in Target.Events)
            {
                e.active = e.guiActive = false;
            }
        }


    }
}
