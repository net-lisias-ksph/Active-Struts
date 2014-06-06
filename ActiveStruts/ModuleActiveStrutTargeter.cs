using System;
using System.Linq;
using UnityEngine;

namespace ActiveStruts
{
    public class StateManager
    {
        private bool _hasPartnerLastSet;
        private ASMode _ownMode;
        private ASMode _partnerMode;

        public string DisplayMode
        {
            get
            {
                if (this._partnerMode == ASMode.Linked || this._ownMode == ASMode.Linked)
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
        public readonly StateManager StateManager = new StateManager();
        [KSPField(isPersistant = true)] public float FreeAttachDistance = 0;
        [KSPField(isPersistant = true)] public float FreeAttachPosX = 0;
        [KSPField(isPersistant = true)] public float FreeAttachPosY = 0;
        [KSPField(isPersistant = true)] public float FreeAttachPosZ = 0;
        [KSPField(isPersistant = true)] public bool HalfWayLink = false;
        [KSPField(isPersistant = true)] public string HalfWayPartnerId;
        [KSPField(isPersistant = true)] public bool IsFreeAttached = false;
        [KSPField(isPersistant = true)] public bool IsLinked = false;
        [KSPField] public string RayCastOriginName = "strut";
        [KSPField(isPersistant = true, guiActive = true)] public LinkType Strength = LinkType.None;
        protected Transform Strut;
        [KSPField] public string StrutName = "strut";
        protected float StrutX, StrutY;
        public bool TargetHighlighterOverride = false;
        [KSPField(isPersistant = true, guiActive = false)] public string TargetId = Guid.Empty.ToString();
        private bool _checkForReDocking;
        private bool _checkTarget;
        private Part _freeAttachPart;
        private ModuleActiveStrutTargeter _halfWayPartner;
        private ConfigurableJoint _joint;
        private bool _jointCreated;
        private Transform _raycastOrigin;
        private bool _suppressEvents;
        private ModuleActiveStrutTarget _target;

        [KSPField] private int _ticksToCheckForLinkAtStart = 100;

        public ModuleActiveStrutTargeter HalfWayPartner
        {
            get
            {
                if (!this.HalfWayLink)
                {
                    return null;
                }
                return this._halfWayPartner ??
                       (this._halfWayPartner = ASUtil.GetAllActiveStrutsModules(this.vessel).Where(m => m is ModuleActiveStrutTargeter && m.ID == new Guid(this.HalfWayPartnerId)).Select(m => m as ModuleActiveStrutTargeter).FirstOrDefault());
            }
            set
            {
                if (value == null)
                {
                    this.HalfWayLink = false;
                    this._halfWayPartner = null;
                    this.HalfWayPartnerId = Guid.Empty.ToString();
                }
                else
                {
                    this.HalfWayLink = true;
                    this.HalfWayPartnerId = value.ID.ToString();
                }
            }
        }

        private bool HasTargetPartner
        {
            get { return this.Target != null && this.Target.HasPartner; }
        }

        private bool IsPartnerTargeter
        {
            get { return this.Partner != null && this.Partner is ModuleActiveStrutTargeter; }
        }

        protected override bool Linked
        {
            get { return this.Mode == ASMode.Linked && ((this.Target != null && this.Target.ID != Guid.Empty) || this.IsFreeAttached); }
        }

        public Vector3 RayCastOrigin
        {
            get { return this._raycastOrigin.position; }
        }

        public ModuleActiveStrutTarget Target
        {
            get
            {
                if (this._target != null)
                {
                    return this._target;
                }
                if (this.TargetID != Guid.Empty)
                {
                    this._target = ASUtil.GetAllActiveStrutsModules(this.vessel).Where(m => m is ModuleActiveStrutTarget && m.ID == this.TargetID).Select(m => m as ModuleActiveStrutTarget).FirstOrDefault();
                }
                return this._target;
            }
            set
            {
                this.TargetID = value.ID;
                this._target = value;
            }
        }

        public Guid TargetID
        {
            get { return new Guid(this.TargetId); }
            set { this.TargetId = value.ToString(); }
        }

        [KSPEvent(name = "Abort", active = false, guiName = "Abort link", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void Abort()
        {
            ActiveStrutsConnectorManagerAddon.Deactivate();
            ASUtil.RestoreEventsOnAllTargeters();
            this.Mode = ASMode.Unlinked;
            foreach (var moduleDockingStrut in ASUtil.GetAllActiveStrutsModules(this.vessel))
            {
                moduleDockingStrut.RevertGui();
            }
            this.Events["Abort"].active = this.Events["Abort"].guiActive = false;
        }

        public void AbortAttachRequest()
        {
            ActiveStrutsConnectorManagerAddon.Deactivate();
            OSD.Info("Free attachment aborted.");
            this.IsFreeAttached = false;
            this.IsLinked = false;
            this.Strength = LinkType.None;
            this.Mode = ASMode.Unlinked;
            this.UpdateGui();
        }

        private void ActivateLineRender(bool listenForLeftClick = false)
        {
            ActiveStrutsConnectorManagerAddon.Activate(this, this.part.Rigidbody.position, listenForLeftClick);
        }

        public void ClearStrut()
        {
            this.Strut.localScale = Vector3.zero;
        }

        private void ExtendHalf(Vector3 target, ModuleActiveStrutTargeter halfWayPartner)
        {
            this._setStrutEnd(target, true);
            this.HalfWayPartner = halfWayPartner;
            this.UpdateGui();
        }

        [KSPEvent(name = "FreeAttach", active = false, guiName = "Free Attachment", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void FreeAttach()
        {
            this.ActivateLineRender(true);
            OSD.Info("Free Attachment active. Left click to attach, right click to abort.");
            //TODO
        }

        public void FreeAttachRequest(MousePositionData mpd)
        {
            if (mpd == null || !mpd.OriginValid || !mpd.HitCurrentVessel)
            {
                return;
            }
            if (mpd.DistanceFromReferenceOriginExact > MaxDistance)
            {
                OSD.Warn("Distance too big!");
                return;
            }
            if (mpd.AngleFromOriginExact > 180)
            {
                OSD.Warn("Angle exceeds 180 degree!");
                return;
            }
            this._createFreeAttachment(mpd);
            ActiveStrutsConnectorManagerAddon.Deactivate();
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
                        .Where(possibleTarget => !possibleTarget.HasPartner || !possibleTarget.ConnectionInUse()))
            {
                this._setPossibleTarget(possibleTarget);
            }
            this.Mode = ASMode.Targeting;
            this.Events["Link"].active = this.Events["Link"].guiActive = false;
            ASUtil.HideEventsOnAllTargeters(this.ID);
        }

        public void MuteStrength()
        {
            this.Fields["Strength"].guiActive = false;
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
                this.Strength = LinkType.None;
            }
            this.Initialized = true;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (this.Mode == ASMode.Unlinked)
            {
                return;
            }
            if (!this._checkTarget || this.IsFreeAttached)
            {
                return;
            }
            if (this._checkPossibleTarget())
            {
                this._checkTarget = false;
                this.SetTarget(this.Target, ASUtil.Tuple.New<bool, ModuleActiveStrutTargeter>(false, null));
            }
            else if (this._ticksToCheckForLinkAtStart-- < 0)
            {
                this._checkTarget = false;
            }
        }

        public void SetTarget(ModuleActiveStrutTarget target, ASUtil.Tuple<bool, ModuleActiveStrutTargeter> halfWayData)
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
            this._setStrutEnd(target.part.transform.position, halfWayData.Item1);
            if (halfWayData.Item1)
            {
                halfWayData.Item2.ExtendHalf(this.RayCastOrigin, this);
            }
            ASUtil.RestoreEventsOnAllTargeters();
            OSD.Success("Link established");
        }

        private void SetTargetAtLoad()
        {
            if (this.IsFreeAttached)
            {
                Debug.Log("setting free attached target at load");
                this._checkTarget = this._tryHitFreeAttachmentPoint();
            }
            else
            {
                Debug.Log("setting target at load with ID " + this.TargetID);
                var searchResult = this.vessel.GetActiveStrut(this.TargetID);
                if (searchResult.Item1 && searchResult.Item2 is ModuleActiveStrutTarget)
                {
                    this.Target = searchResult.Item2 as ModuleActiveStrutTarget;
                    this._checkTarget = true;
                    Debug.Log("target set");
                }
            }
            if (this._checkTarget)
            {
                return;
            }
            foreach (var e in this.Events)
            {
                e.active = e.guiActive = false;
            }
            this.Mode = ASMode.Unlinked;
        }

        public void SetTargetHighlighterOverride(bool overrideFlag)
        {
            this.TargetHighlighterOverride = overrideFlag;
        }

        public void ShareState(ASMode mode)
        {
            this.StateManager.SetPartnerMode(mode);
            this.UpdateGui();
        }

        public void ShowStrength()
        {
            this.Fields["Strength"].guiActive = true;
        }

        public void SuppressAllEvents(bool flag)
        {
            this._suppressEvents = flag;
        }

        [KSPEvent(name = "ToggleLink", active = false, guiName = "Toggle Link")]
        public void ToggleLink()
        {
            if (this.Linked)
            {
                this.Unlink();
                return;
            }
            if (this.HalfWayLink)
            {
                if (this.HalfWayPartner != null && this.HalfWayPartner.TargetID == this.ID)
                {
                    this.HalfWayPartner.ToggleLink();
                }
                return;
            }
            if (this.Target == null)
            {
                return;
            }
            if (!this._checkPossibleTarget())
            {
                OSD.Warn("Can't reconnect to target.");
                return;
            }
            this.Target.SetTargetedBy(this);
            this.Target.SetAsTarget();
            //this.SetTarget(this.Target, this.Target.HasPartner ? ASUtil.Tuple.New(true, this.Target.GetPartner()) : ASUtil.Tuple.New<bool, ModuleActiveStrutTargeter>(false, null));
        }

        [KSPAction("ToggleLinkAction", KSPActionGroup.None, guiName = "Toggle Link")]
        public void ToggleLinkAction(KSPActionParam param)
        {
            this.ToggleLink();
        }

        [KSPEvent(name = "Unlink", active = false, guiName = "Unlink", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void Unlink()
        {
            if (this.IsFreeAttached)
            {
                this.IsFreeAttached = false;
                this.Strength = LinkType.None;
            }
            else
            {
                if (this.HalfWayLink)
                {
                    this.HalfWayPartner.Unlink();
                }
                else
                {
                    this.Target.Unlink();
                    //this.Target = null;
                    //this.TargetID = Guid.Empty;
                }
            }
            this.Mode = ASMode.Unlinked;
            this.Events["Unlink"].active = this.Events["Unlink"].guiActive = false;
            this.UpdateLink();
            this.UpdateGui();
            OSD.Success("Unlinked");
        }

        [KSPAction("UnlinkAction", KSPActionGroup.None, guiName = "Unlink")]
        public void UnlinkAction(KSPActionParam param)
        {
            if (this.Mode == ASMode.Linked || this.HalfWayLink)
            {
                this.Unlink();
            }
        }

        internal override void UpdateGui()
        {
            if (this._suppressEvents)
            {
                this.Events["Unlink"].active = this.Events["Unlink"].guiActive = false;
                this.Events["Link"].active = this.Events["Link"].guiActive = false;
                this.Events["FreeAttach"].active = this.Events["FreeAttach"].guiActive = false;
                this.Events["ToggleLink"].active = this.Events["ToggleLink"].guiActive = false;
                this.Events["Abort"].active = this.Events["Abort"].guiActive = false;
                return;
            }
            switch (this.Mode)
            {
                case ASMode.Linked:
                {
                    this.Events["Unlink"].active = this.Events["Unlink"].guiActive = true;
                    this.Events["Link"].active = this.Events["Link"].guiActive = false;
                    this.Events["FreeAttach"].active = this.Events["FreeAttach"].guiActive = false;
                    this.Events["ToggleLink"].active = this.Events["ToggleLink"].guiActive = true;
                }
                    break;
                case ASMode.Targeting:
                {
                    this.Events["Abort"].active = this.Events["Abort"].guiActive = true;
                    this.Events["FreeAttach"].active = this.Events["FreeAttach"].guiActive = false;
                    this.Events["ToggleLink"].active = this.Events["ToggleLink"].guiActive = false;
                }
                    break;
                case ASMode.Unlinked:
                {
                    if (this.HasPartner && this.Partner.ConnectionInUse())
                    {
                        this.Events["Link"].active = this.Events["Link"].guiActive = false;
                        this.Events["FreeAttach"].active = this.Events["FreeAttach"].guiActive = false;
                        this.Events["ToggleLink"].active = this.Events["ToggleLink"].guiActive = false;
                    }
                    else
                    {
                        this.Events["Link"].active = this.Events["Link"].guiActive = true;
                        this.Events["FreeAttach"].active = this.Events["FreeAttach"].guiActive = true;
                        if (TargetID != Guid.Empty && (!Target.HasPartner || !Target.ConnectionInUse()))
                        {
                            this.Events["ToggleLink"].active = this.Events["ToggleLink"].guiActive = true;
                        }
                        else
                        {
                            this.Events["ToggleLink"].active = this.Events["ToggleLink"].guiActive = false;
                        }
                    }
                    if (this.HalfWayLink)
                    {
                        this.Events["Unlink"].active = this.Events["Unlink"].guiActive = true;
                        this.Events["Link"].active = this.Events["Link"].guiActive = false;
                        if (this.HalfWayPartner != null && this.HalfWayPartner.TargetID == this.ID)
                        {
                            this.Events["ToggleLink"].active = this.Events["ToggleLink"].guiActive = true;
                        }
                        else
                        {
                            this.Events["ToggleLink"].active = this.Events["ToggleLink"].guiActive = false;
                        }
                    }
                }
                    break;
            }
            this.State = StateManager.DisplayMode;
        }

        public void UpdateLink()
        {
            try
            {
                if (this.Linked)
                {
                    if (this.IsFreeAttached)
                    {
                        if (this._freeAttachPart.vessel != this.vessel)
                        {
                            this.Mode = ASMode.Unlinked;
                            this.Events["Unlink"].guiActive = false;
                            this.IsFreeAttached = false;
                        }
                        this._checkForReDocking = false;
                    }
                    else if (this.Target.vessel != this.vessel)
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
                        var target = this.vessel.GetActiveStrut(this.TargetID).Item2;
                        if (target != null)
                        {
                            this.Target = target as ModuleActiveStrutTarget;
                            this.SetTarget(this.Target, ASUtil.Tuple.New<bool, ModuleActiveStrutTargeter>(false, null));
                            //this.Target.SetTargetedBy(this, Vector3.Distance(this.Target.part.transform.position, this.part.transform.position));
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
                    if (this.IsFreeAttached ? this._freeAttachPart == null : this.Target == null)
                    {
                        OSD.Warn(this.ID + " cant't find its target");
                        return;
                    }
                    this._joint = this.part.rigidbody.gameObject.AddComponent<ConfigurableJoint>();
                    if (this.IsFreeAttached)
                    {
                        this._joint.connectedBody = this._freeAttachPart.rigidbody;
                    }
                    else
                    {
                        var moduleActiveStrutTarget = this.Target;
                        if (moduleActiveStrutTarget != null)
                        {
                            this._joint.connectedBody = moduleActiveStrutTarget.part.rigidbody;
                        }
                    }
                    this._joint.breakForce = this._joint.breakTorque = this._determineJointStrength();
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
                    this.Strength = LinkType.None;
                }
                this._jointCreated = this.Linked;
            }
            catch (Exception exception)
            {
                OSD.Error("Sorry, something unexpected happened!");
                Debug.Log("[AS][EX] " + exception.Message + " " + exception.StackTrace);
            }
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

        private void _createFreeAttachment(MousePositionData mpd)
        {
            this.IsFreeAttached = true;
            this.FreeAttachDistance = mpd.DistanceFromReferenceOriginExact;
            this.FreeAttachPosX = mpd.ExactHitPosition.x;
            this.FreeAttachPosY = mpd.ExactHitPosition.y;
            this.FreeAttachPosZ = mpd.ExactHitPosition.z;
            this.IsLinked = true;
            this.Mode = ASMode.Linked;
            this._freeAttachPart = mpd.HittedPart;
            this._setStrutEnd(mpd.ExactHitPosition);
            this.UpdateLink();
            this.UpdateGui();
        }

        private float _determineJointStrength()
        {
            if (!this.Linked)
            {
                this.Strength = LinkType.None;
                return 0;
            }
            if (this.IsFreeAttached)
            {
                this.Strength = LinkType.Weak;
                return WeakStrength;
            }
            if (this.HasTargetPartner)
            {
                this.Strength = LinkType.Maximal;
                return MaximalStrength;
            }
            this.Strength = LinkType.Normal;
            return NormalStrength;
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
            //target.SetTargetedBy(this, distanceTestResult.Item2);
            target.SetTargetedBy(this);
            foreach (var e in target.Events)
            {
                e.active = e.guiActive = false;
            }
        }

        private void _setStrutEnd(Vector3 position, bool halfWay = false)
        {
            this.Strut.LookAt(position);
            this.Strut.localScale = new Vector3(this.StrutX, this.StrutY, 1);
            var distance = -1*Vector3.Distance(Vector3.zero, this.Strut.InverseTransformPoint(position))*(halfWay ? 0.5f : 1.0f);
            this.Strut.localScale = new Vector3(this.StrutX, this.StrutY, distance);
        }

        private bool _tryHitFreeAttachmentPoint()
        {
            const float distanceTolerance = 0.05f;
            if (!this.IsFreeAttached)
            {
                return false;
            }
            RaycastHit info;
            var start = this.RayCastOrigin;
            var dir = (new Vector3(this.FreeAttachPosX, this.FreeAttachPosY, this.FreeAttachPosZ) - start).normalized;
            var hit = Physics.Raycast(new Ray(start, dir), out info, this.FreeAttachDistance + distanceTolerance);
            if (!hit)
            {
                return false;
            }
            var hittedPart = ASUtil.PartFromHit(info);
            this._freeAttachPart = hittedPart;
            var hitDistFlag = this.FreeAttachDistance + distanceTolerance >= info.distance && this.FreeAttachDistance - distanceTolerance <= info.distance;
            return hittedPart.vessel == this.vessel && hitDistFlag;
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