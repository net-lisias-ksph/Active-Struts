using System;
using System.Linq;
using UnityEngine;

namespace ActiveStruts
{
    public class StateManager
    {
        private ASMode _partnerMode;
        private ASMode _ownMode;
        private bool _hasPartnerLastSet;

        public string DisplayMode
        {
            get
            {
                if (_partnerMode == ASMode.Linked || _ownMode == ASMode.Linked)
                {
                    return ASMode.Linked.ToString();
                }
                return this._hasPartnerLastSet ? this._partnerMode.ToString() : this._ownMode.ToString();
            }
        }

        public void SetOwnMode(ASMode mode)
        {
            this._ownMode = mode;
            this._hasPartnerLastSet = false;
        }

        public void SetPartnerMode(ASMode mode)
        {
            this._partnerMode = mode;
            this._hasPartnerLastSet = true;
        }
    }

    public class ModuleActiveStrutTargeter : ModuleActiveStrutBase
    {
        [KSPField(isPersistant = true)] public bool IsLinked = false;
        [KSPField] public string RayCastOriginName = "strut";
        protected Transform Strut;
        [KSPField] public string StrutName = "strut";
        protected float StrutX, StrutY;
        [KSPField(isPersistant = true, guiActive = false)] public string TargetId = Guid.Empty.ToString();
        private bool _checkForReDocking;
        private bool _checkTarget;
        private ConfigurableJoint _joint;
        private bool _jointCreated;
        private Transform _raycastOrigin;
        public readonly StateManager StateManager = new StateManager();

        [KSPField] private int _ticksToCheckForLinkAtStart = 100;

        private bool IsPartnerTargeter
        {
            get { return this.Partner != null && this.Partner is ModuleActiveStrutTargeter; }
        }

        protected override bool Linked
        {
            get { return this.Mode == ASMode.Linked && this.Target != null && this.Target.ID != Guid.Empty; }
        }

        public Vector3 RayCastOrigin
        {
            get { return this._raycastOrigin.position; }
        }

        public ModuleActiveStrutTarget Target { get; set; }

        public Guid TargetID
        {
            get { return new Guid(this.TargetId); }
            set { this.TargetId = value.ToString(); }
        }

        [KSPEvent(name = "Abort", active = false, guiName = "Abort link", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void Abort()
        {
            ConnectorManager.Deactivate();
            this.Mode = ASMode.Unlinked;
            foreach (var moduleDockingStrut in ASUtil.GetAllDockingStrutModules(this.vessel))
            {
                moduleDockingStrut.RevertGui();
            }
            this.Events["Abort"].active = this.Events["Abort"].guiActive = false;
        }

        private void ActivateLineRender()
        {
            ConnectorManager.Activate(this, this.part.Rigidbody.position);
        }

        [KSPEvent(name = "Link", active = false, guiName = "Link", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void Link()
        {
            if (this.HasPartner && this.Partner.ConnectionInUse())
            {
                OSD.Warn("Targeter strut can only have one active connection at the same time");
                return;
            }
            this.ActivateLineRender();
            foreach (
                var possibleTarget in
                    this.vessel.parts.Where(p => p != this.part && p.Modules.Contains(TargetModuleName))
                        .Select(p => p.Modules[TargetModuleName] as ModuleActiveStrutTarget)
                        .Where(possibleTarget => possibleTarget.Mode == ASMode.Unlinked && !possibleTarget.ConnectionInUse()))
            {
                this._setPossibleTarget(possibleTarget);
            }
            this.Mode = ASMode.Targeting;
            this.Events["Link"].active = this.Events["Link"].guiActive = false;
        }

        public override void OnStart(StartState state)
        {
            this.Strut = this.part.FindModelTransform(this.StrutName);
            this.StrutX = this.Strut.localScale.x;
            this.StrutY = this.Strut.localScale.y;
            this.Strut.localScale = Vector3.zero;
            if (state == StartState.Editor)
            {
                return;
            }
            if (this.HasPartner)
            {
                this.Partner = this.part.Modules[TargetModuleName] as ModuleActiveStrutBase;
            }
            this._raycastOrigin = this.part.FindModelTransform(this.RayCastOriginName);
            if (this.ID == Guid.Empty)
            {
                this.ID = Guid.NewGuid();
            }
            if (this.TargetID != Guid.Empty && this.Mode == ASMode.Linked)
            {
                this.SetTargetAtLoad();
            }
            else
            {
                this.Mode = ASMode.Unlinked;
            }
            this.Initialized = true;
            this.Events["Unlink"].guiName = "Unlink Target";
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (this.Mode == ASMode.Unlinked)
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

        public void SetTarget(ModuleActiveStrutTarget target)
        {
            if (!this._checkPossibleTarget(target))
            {
                this.Mode = ASMode.Unlinked;
                return;
            }
            foreach (var e in this.Events)
            {
                e.active = e.guiActive = false;
            }
            this.Target = target;
            this.Mode = ASMode.Linked;
            this._setStrutEnd(target.part.transform.position);
            OSD.Success("Link established");
        }

        private void SetTargetAtLoad()
        {
            Debug.Log("setting target at load with ID " + this.TargetID);
            var searchResult = this.vessel.GetDockingStrut(this.TargetID);
            if (searchResult.Item1 && searchResult.Item2 is ModuleActiveStrutTarget)
            {
                this.Target = searchResult.Item2 as ModuleActiveStrutTarget;
                this._checkTarget = true;
                Debug.Log("target set");
            }
            else
            {
                foreach (var e in this.Events)
                {
                    e.active = e.guiActive = false;
                }
                this.Mode = ASMode.Unlinked;
            }
        }

        [KSPEvent(name = "Unlink", active = false, guiName = "Unlink", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void Unlink()
        {
            this.Target.Mode = ASMode.Unlinked;
            this.UnlinkSelf();
        }

        [KSPAction("UnlinkAction", KSPActionGroup.None, guiName = "Unlink")]
        public void UnlinkAction(KSPActionParam param)
        {
            if (this.Mode == ASMode.Linked)
            {
                this.Unlink();
            }
        }

        protected void UnlinkSelf()
        {
            OSD.Success("Unlinked");
            this.Mode = ASMode.Unlinked;
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
                case ASMode.Linked:
                {
                    this.Events["Unlink"].active = this.Events["Unlink"].guiActive = true;
                }
                    break;
                case ASMode.Targeting:
                {
                    this.Events["Abort"].active = this.Events["Abort"].guiActive = true;
                }
                    break;
                case ASMode.Unlinked:
                {
                    if (this.HasPartner && this.Partner.ConnectionInUse())
                    {
                        this.Events["Link"].active = this.Events["Link"].guiActive = false;
                    }
                    else
                    {
                        this.Events["Link"].active = this.Events["Link"].guiActive = true;
                    }
                }
                    break;
            }
        }

        public void UpdateLink()
        {
            try
            {
                if (this.Linked)
                {
                    if (this.Target.vessel != this.vessel)
                    {
                        this.Mode = ASMode.Unlinked;
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
                            this.Target = target as ModuleActiveStrutTarget;
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

        public void ShareState(ASMode mode)
        {
            this.StateManager.SetPartnerMode(mode);
        }

        private ASUtil.Tuple<bool, Single> _checkDistance(PartModule target)
        {
            var distance = Vector3.Distance(target.part.transform.position, this.part.transform.position);
            return ASUtil.Tuple.New(distance <= MaxDistance, distance);
        }

        private bool _checkPossibleTarget(ModuleActiveStrutTarget target = null)
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

        private void _setPossibleTarget(ModuleActiveStrutTarget target)
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
            this.Strut.localScale = new Vector3(this.StrutX, this.StrutY, -1*Vector3.Distance(Vector3.zero, this.Strut.InverseTransformPoint(position)));
        }

        private ASUtil.Tuple<bool, Part> _tryPartHit(ModuleActiveStrutTarget target)
        {
            RaycastHit info;
            var start = this.RayCastOrigin;
            var dir = (target.part.transform.position - start).normalized;
            var hit = Physics.Raycast(new Ray(start, dir), out info, MaxDistance + 1);
            var hittedPart = ASUtil.PartFromHit(info);
            return ASUtil.Tuple.New(hit, hittedPart);
        }
    }
}