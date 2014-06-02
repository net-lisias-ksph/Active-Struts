using System;
using System.Linq;
using UnityEngine;

namespace DockingStrut
{
    public class ModuleDockingStrutTargeter : ModuleDockingStrutBase
    {
        [KSPField] public string RayCastOriginName = "strut";
        [KSPField(isPersistant = true, guiActive = true)] public string TargetId = Guid.Empty.ToString();
        private bool _checkForReDocking;
        private bool _checkTarget;
        private ConfigurableJoint _joint;
        private bool _jointCreated;
        private Transform _raycastOrigin;

        [KSPField] private int _ticksToCheckForLinkAtStart = 100;

        protected override bool Linked
        {
            get { return this.Mode == DSMode.Linked && this.Target != null && this.Target.ID != Guid.Empty; }
        }

        public Vector3 RayCastOrigin
        {
            get { return this._raycastOrigin.position; }
        }

        public ModuleDockingStrutTarget Target { get; set; }

        public Guid TargetID
        {
            get { return new Guid(this.TargetId); }
            set { this.TargetId = value.ToString(); }
        }

        [KSPEvent(name = "Abort", active = false, guiName = "Abort link", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void Abort()
        {
            this.Mode = DSMode.Unlinked;
            foreach (var moduleDockingStrut in DSUtil.GetAllDockingStrutModules(this.vessel))
            {
                moduleDockingStrut.RevertGui();
            }
            this.Events["Abort"].active = this.Events["Abort"].guiActive = false;
        }

        [KSPEvent(name = "Link", active = false, guiName = "Link", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void Link()
        {
            foreach (
                var possibleTarget in
                    this.vessel.parts.Where(p => p != this.part && p.Modules.Contains(TargetModuleName))
                        .Select(p => p.Modules[TargetModuleName] as ModuleDockingStrutTarget)
                        .Where(possibleTarget => possibleTarget == null || possibleTarget.Mode == DSMode.Unlinked))
            {
                this._setPossibleTarget(possibleTarget);
            }
            this.Mode = DSMode.Targeting;
            this.Events["Link"].active = this.Events["Link"].guiActive = false;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            this.Strut.localScale = Vector3.zero;
            if (state == StartState.Editor)
            {
                return;
            }
            this._raycastOrigin = this.part.FindModelTransform(this.RayCastOriginName);
            if (this.ID == Guid.Empty)
            {
                this.ID = Guid.NewGuid();
            }
            if (this.TargetID != Guid.Empty && this.Mode == DSMode.Linked)
            {
                this.SetTargetAtLoad();
            }
            else
            {
                this.Mode = DSMode.Unlinked;
            }
            this.Initialized = true;
            this.Events["Unlink"].guiName = "Unlink Target";
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (this.Mode == DSMode.Unlinked)
            {
                return;
            }
            if (!this._checkTarget)
            {
                return;
            }
            if (this._checkPossibleTarget())
            {
                this._checkTarget = false;
                this.SetTarget(this.Target);
            }
            else if (this._ticksToCheckForLinkAtStart-- < 0)
            {
                this._checkTarget = false;
            }
        }

        public void SetTarget(ModuleDockingStrutTarget target)
        {
            if (!this._checkPossibleTarget(target))
            {
                this.Mode = DSMode.Unlinked;
                return;
            }
            foreach (var e in this.Events)
            {
                e.active = e.guiActive = false;
            }
            this.Target = target;
            this.TargetID = target.ID;
            this.Mode = DSMode.Linked;
            this._setStrutEnd(target.StrutTarget);
            OSD.Success("Linked " + this.ID + " and " + this.TargetID);
        }

        private void SetTargetAtLoad()
        {
            Debug.Log("setting target at load with ID " + this.TargetID);
            var searchResult = this.vessel.GetDockingStrut(this.TargetID);
            if (searchResult.Item1 && searchResult.Item2 is ModuleDockingStrutTarget)
            {
                this.Target = searchResult.Item2 as ModuleDockingStrutTarget;
                this._checkTarget = true;
                Debug.Log("target set");
            }
            else
            {
                foreach (var e in this.Events)
                {
                    e.active = e.guiActive = false;
                }
                this.Mode = DSMode.Unlinked;
            }
        }

        public override void UnlinkPartner(bool secondary = false)
        {
            if (secondary)
            {
                this.UnlinkSelf();
                return;
            }
            this.Target.UnlinkPartner(true);
        }

        protected override void UnlinkSelf()
        {
            OSD.Success("Unlinked " + this.ID + " and " + this.TargetID);
            this.Mode = DSMode.Unlinked;
            this.Events["Unlink"].active = this.Events["Unlink"].guiActive = false;
            this.Target = null;
            this.TargetID = Guid.Empty;
            this.UpdateLink();
            this.UpdateGui();
        }

        internal override void UpdateGui()
        {
            switch (this.Mode)
            {
                case DSMode.Linked:
                {
                    this.Events["Unlink"].active = this.Events["Unlink"].guiActive = true;
                }
                    break;
                case DSMode.Targeting:
                {
                    this.Events["Abort"].active = this.Events["Abort"].guiActive = true;
                }
                    break;
                case DSMode.Unlinked:
                {
                    this.Events["Link"].active = this.Events["Link"].guiActive = true;
                }
                    break;
            }
        }

        protected override void UpdateLink()
        {
            try
            {
                if (this.Linked)
                {
                    if (this.Target.vessel != this.vessel)
                    {
                        this.Mode = DSMode.Unlinked;
                        this._checkForReDocking = true;
                        this.Events["Unlink"].guiActive = false;
                    }
                    else
                    {
                        this.TargetID = this.Target.ID;
                    }
                }
                if (this._checkForReDocking)
                {
                    if (this.Linked)
                    {
                        this._checkForReDocking = false;
                    }
                    else if (this.TargetID != Guid.Empty)
                    {
                        var target = this.vessel.GetDockingStrut(this.TargetID).Item2;
                        if (target != null)
                        {
                            this.Target = target as ModuleDockingStrutTarget;
                            this.SetTarget(this.Target);
                            this.Target.SetTargetedBy(this);
                            this._checkForReDocking = false;
                        }
                    }
                }
                if (this._jointCreated == this.Linked || this.part.rigidbody.isKinematic || (this.Target != null && this.Target.part.rigidbody.isKinematic))
                {
                    return;
                }
                if (this.Linked)
                {
                    if (this.Target == null)
                    {
                        OSD.Warn(this.ID + " cant't find its target");
                        return;
                    }
                    this._joint = this.part.rigidbody.gameObject.AddComponent<ConfigurableJoint>();
                    this._joint.connectedBody = this.Target.part.rigidbody;
                    this._joint.breakForce = this._joint.breakTorque = float.PositiveInfinity;
                    this._joint.xMotion = ConfigurableJointMotion.Locked;
                    this._joint.yMotion = ConfigurableJointMotion.Locked;
                    this._joint.zMotion = ConfigurableJointMotion.Locked;
                    this._joint.angularXMotion = ConfigurableJointMotion.Locked;
                    this._joint.angularYMotion = ConfigurableJointMotion.Locked;
                    this._joint.angularZMotion = ConfigurableJointMotion.Locked;
                }
                else
                {
                    Destroy(this._joint);
                    this._joint = null;
                    this.Strut.localScale = Vector3.zero;
                }
                this._jointCreated = this.Linked;
            }
            catch
            {
                OSD.Error("Sorry, something unexpected happened!");
            }
        }

        private DSUtil.Tuple<bool, Single> _checkDistance(PartModule target)
        {
            var distance = Vector3.Distance(target.part.transform.position, this.part.transform.position);
            return DSUtil.Tuple.New(distance <= MaxDistance, distance);
        }

        private bool _checkPossibleTarget(ModuleDockingStrutTarget target = null)
        {
            var tempTarget = target ?? this.Target;
            try
            {
                var distanceTestResult = this._checkDistance(tempTarget);
                if (!distanceTestResult.Item1)
                {
                    OSD.Error("Out of range by " + Math.Round(distanceTestResult.Item2 - MaxDistance, 2) + "m");
                    return false;
                }
                var raycastHitResult = this._tryPartHit(tempTarget);
                var otherHit = raycastHitResult.Item1 && raycastHitResult.Item2 != tempTarget.part;
                if (otherHit)
                {
                    OSD.Error("Obstructed by " + raycastHitResult.Item2.name);
                    return false;
                }
                if (raycastHitResult.Item1)
                {
                    return raycastHitResult.Item1 && raycastHitResult.Item2 == tempTarget.part;
                }
                OSD.Error("No target found.");
                return false;
            }
            catch
            {
                return false;
            }
        }

        private void _setPossibleTarget(ModuleDockingStrutTarget target)
        {
            var distanceTestResult = this._checkDistance(target);
            if (!distanceTestResult.Item1)
            {
                target.SetErrorMessage("Out of range by " + Math.Round(distanceTestResult.Item2 - MaxDistance, 2) + "m");
                return;
            }
            var hitResult = this._tryPartHit(target);
            var hit = hitResult.Item1 && hitResult.Item2 != target.part;
            if (hit)
            {
                target.SetErrorMessage("Obstructed by " + hitResult.Item2.name);
                return;
            }
            target.BackupOldTargeter();
            target.SetTargetedBy(this);
            foreach (var e in target.Events)
            {
                e.active = e.guiActive = false;
            }
        }

        private void _setStrutEnd(Vector3 position)
        {
            this.Strut.LookAt(position);
            this.Strut.localScale = new Vector3(this.StrutX, this.StrutY, 1);
            this.Strut.localScale = new Vector3(this.StrutX, this.StrutY, Vector3.Distance(Vector3.zero, this.Strut.InverseTransformPoint(position)));
        }

        private DSUtil.Tuple<bool, Part> _tryPartHit(ModuleDockingStrutTarget target)
        {
            RaycastHit info;
            var start = this.RayCastOrigin;
            var dir = (target.StrutTarget - start).normalized;
            var hit = Physics.Raycast(new Ray(start, dir), out info, MaxDistance + 1);
            var hittedPart = DSUtil.PartFromHit(info);
            return DSUtil.Tuple.New(hit, hittedPart);
        }
    }
}