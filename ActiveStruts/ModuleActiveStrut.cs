using System;
using System.Linq;
using UnityEngine;

namespace ActiveStruts
{
    public class ModuleActiveStrut : PartModule
    {
        [KSPField(isPersistant = true)] public float FreeAttachDistance = 0.0f;
        [KSPField(isPersistant = true)] public string FreeAttachPointString = "0.0 0.0 0.0";
        [KSPField(isPersistant = true, guiActive = true)] public string Id = Guid.Empty.ToString();
        [KSPField(isPersistant = true)] public bool IsConnectionOrigin = false;
        [KSPField(isPersistant = true)] public bool IsFreeAttached = false;
        [KSPField(isPersistant = true)] public bool IsHalfWayExtended = false;
        [KSPField(isPersistant = true)] public bool IsLinked = false;
        [KSPField(isPersistant = true)] public bool IsTargetOnly = false;
        public ModuleActiveStrut OldTargeter;
        public Transform Origin;
        [KSPField(isPersistant = false, guiActive = true)] public string State = "n.a.";
        [KSPField(guiActive = true)] public string Strength = LinkType.None.ToString();
        public Transform Strut;
        [KSPField(isPersistant = true)] public string StrutName = "strut";
        [KSPField(isPersistant = true)] public string TargetId = Guid.NewGuid().ToString();
        [KSPField(isPersistant = true)] public string TargeterId = Guid.NewGuid().ToString();
        private bool _delayedStartFlag;
        private Part _freeAttachPart;
        private ConfigurableJoint _joint;
        private LinkType _linkType;
        private Mode _mode = Mode.Undefined;
        private int _ticksForDelayedStart;

        private Part FreeAttachPart
        {
            get
            {
                if (this._freeAttachPart != null)
                {
                    return this._freeAttachPart;
                }
                var rayRes = this.CheckFreeAttachPoint();
                if (rayRes.HitResult)
                {
                    this._freeAttachPart = rayRes.TargetPart;
                }
                return this._freeAttachPart;
            }
        }

        public Vector3 FreeAttachPoint
        {
            get
            {
                var coords = this.FreeAttachPointString.Split(' ').Select(float.Parse).ToArray();
                return new Vector3(coords[0], coords[1], coords[2]);
            }
            set { this.FreeAttachPointString = string.Format("{0} {1} {2}", value.x, value.y, value.z); }
        }

        public Guid ID
        {
            get { return new Guid(this.Id); }
        }

        public bool IsConnectionFree
        {
            get { return this.IsTargetOnly || !this.IsLinked || (this.IsLinked && this.Mode == Mode.Unlinked); }
        }

        public LinkType LinkType
        {
            get { return this._linkType; }
            set
            {
                this._linkType = value;
                this.Strength = value.ToString();
            }
        }

        public Mode Mode
        {
            get { return this._mode; }
            set
            {
                this._mode = value;
                this.State = value.ToString();
            }
        }

        public ModuleActiveStrut Target
        {
            get { return this.TargetId == Guid.Empty.ToString() ? null : this.part.vessel.GetStrutById(new Guid(this.TargetId)); }
            set { this.TargetId = value != null ? value.ID.ToString() : Guid.Empty.ToString(); }
        }

        public ModuleActiveStrut Targeter
        {
            get { return this.TargeterId == Guid.Empty.ToString() ? null : this.part.vessel.GetStrutById(new Guid(this.TargeterId)); }
            set { this.TargeterId = value != null ? value.ID.ToString() : Guid.Empty.ToString(); }
        }

        [KSPEvent(name = "AbortLink", active = false, guiName = "Abort Link", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void AbortLink()
        {
            this.Mode = Mode.Unlinked;
            Util.ResetAllFromTargeting();
            ActiveStrutsAddon.Mode = AddonMode.None;
            OSD.Info("Link aborted.");
            this.UpdateGui();
        }

        public void CreateJoint(Rigidbody originBody, Rigidbody targetBody, LinkType type)
        {
            this._joint = originBody.gameObject.AddComponent<ConfigurableJoint>();
            this._joint.connectedBody = targetBody;
            this._joint.breakForce = this._joint.breakTorque = type.GetJointStrength();
            this._joint.xMotion = ConfigurableJointMotion.Locked;
            this._joint.yMotion = ConfigurableJointMotion.Locked;
            this._joint.zMotion = ConfigurableJointMotion.Locked;
            this._joint.angularXMotion = ConfigurableJointMotion.Locked;
            this._joint.angularYMotion = ConfigurableJointMotion.Locked;
            this._joint.angularZMotion = ConfigurableJointMotion.Locked;
            this.LinkType = type;
            if (!IsFreeAttached)
            {
                this.Target.LinkType = type;
            }
        }

        public void CreateStrut(Vector3 target, float distancePercent = 1)
        {
            var strut = this.Strut;
            strut.LookAt(target);
            strut.localScale = new Vector3(1, 1, 1);
            var distance = -1*Vector3.Distance(Vector3.zero, this.Strut.InverseTransformPoint(target))*distancePercent;
            this.Strut.localScale = new Vector3(1, 1, distance);
        }

        public void DestroyJoint()
        {
            Destroy(this._joint);
            this._joint = null;
            this.LinkType = LinkType.None;
        }

        public void DestroyStrut()
        {
            this.Strut.localScale = Vector3.zero;
        }

        [KSPEvent(name = "FreeAttach", active = false, guiName = "FreeAttach Link", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void FreeAttach()
        {
            OSD.Info(Config.FreeAttachHelpText);
            ActiveStrutsAddon.CurrentTargeter = this;
            ActiveStrutsAddon.Mode = AddonMode.FreeAttach;
        }

        [KSPEvent(name = "Link", active = false, guiName = "Link", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void Link()
        {
            this.Mode = Mode.Targeting;
            foreach (var possibleTarget in this.GetAllPossibleTargets())
            {
                possibleTarget.SetTargetedBy(this);
                possibleTarget.UpdateGui();
            }
            ActiveStrutsAddon.Mode = AddonMode.Link;
            ActiveStrutsAddon.CurrentTargeter = this;
            OSD.Info(Config.LinkHelpText, 5);
            this.UpdateGui();
        }

        public override void OnStart(StartState state)
        {
            if (!this.IsTargetOnly)
            {
                this.Strut = this.part.FindModelTransform(this.StrutName);
                this.DestroyStrut();
            }
            this.Origin = this.part.transform;
            if (state == StartState.Editor)
            {
                this._delayedStartFlag = false;
                return;
            }
            this._delayedStartFlag = true;
            this._ticksForDelayedStart = 100;
        }

        public override void OnUpdate()
        {
            if (this._delayedStartFlag)
            {
                this._delayedStart();
                return;
            }
            if (this.Mode == Mode.Unlinked || this.Mode == Mode.Target || this.Mode == Mode.Targeting)
            {
                return;
            }
            if (this.IsTargetOnly)
            {
                if (!this.AnyTargetersConnected())
                {
                    this.Mode = Mode.Unlinked;
                    this.UpdateGui();
                    return;
                }
            }
            if (this.Mode == Mode.Linked)
            {
                if (this.IsFreeAttached)
                {
                    if (this.FreeAttachPart != null && this.FreeAttachPart.vessel == this.vessel)
                    {
                        return;
                    }
                    this.Unlink();
                    return;
                }
                if (this.IsConnectionOrigin)
                {
                    if (this.Target != null && this.Target.vessel == this.vessel)
                    {
                        return;
                    }
                    this.DestroyJoint();
                    this.DestroyStrut();
                    this.Mode = Mode.Unlinked;
                }
                else
                {
                    if (this.Targeter != null && this.Targeter.vessel == this.vessel)
                    {
                        return;
                    }
                    this.DestroyStrut();
                    this.Mode = Mode.Unlinked;
                }
                this.UpdateGui();
            }
        }

        public void PlaceFreeAttach(Part hittedPart, Vector3 hitPosition, float distance)
        {
            this.Mode = Mode.Linked;
            this.IsLinked = true;
            this.IsFreeAttached = true;
            this.IsConnectionOrigin = true;
            this.CreateJoint(this.part.rigidbody, hittedPart.rigidbody, LinkType.Weak);
            this.CreateStrut(hitPosition);
            this.FreeAttachPoint = hitPosition;
            this.FreeAttachDistance = distance;
            this.Target = null;
            this.Targeter = null;
            ActiveStrutsAddon.Mode = AddonMode.None;
            OSD.Success("FreeAttach Link established!");
            this.UpdateGui();
        }

        private void Reconnect()
        {
            if (this.IsFreeAttached)
            {
                var rayRes = this.CheckFreeAttachPoint();
                if (!rayRes.HitResult)
                {
                    this.IsFreeAttached = false;
                    return;
                }
                this.PlaceFreeAttach(rayRes.TargetPart, this.FreeAttachPoint, this.FreeAttachDistance);
                return;
            }
            if (this.IsConnectionOrigin)
            {
                if (this.Target != null && this.IsPossibleTarget(this.Target))
                {
                    if (!this.Target.IsTargetOnly)
                    {
                        this.CreateStrut(this.Target.Origin.position, 0.5f);
                    }
                    else
                    {
                        this.CreateStrut(this.Target.Origin.position);
                    }
                    this.CreateJoint(this.part.rigidbody, this.Target.part.rigidbody, LinkType.Maximal);
                    this.Mode = Mode.Linked;
                    this.IsLinked = true;
                }
            }
            else
            {
                if (this.IsPossibleTarget(this.Targeter))
                {
                    this.CreateStrut(this.Targeter.Origin.position, 0.5f);
                    this.Mode = Mode.Linked;
                    this.IsLinked = true;
                }
            }
            this.UpdateGui();
        }

        [KSPEvent(name = "SetAsTarget", active = false, guiName = "Set as Target", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void SetAsTarget()
        {
            this.IsLinked = true;
            this.part.SetHighlightDefault();
            this.Mode = Mode.Linked;
            this.IsConnectionOrigin = false;
            if (!this.IsTargetOnly)
            {
                this.CreateStrut(this.Targeter.Origin.position, 0.5f);
            }
            this.Targeter.SetTarget(this);
            this.UpdateGui();
        }

        public void SetTarget(ModuleActiveStrut target)
        {
            this.Target = target;
            this.Mode = Mode.Linked;
            this.IsLinked = true;
            this.CreateJoint(this.part.rigidbody, target.part.rigidbody, target.IsTargetOnly ? LinkType.Normal : LinkType.Maximal);
            this.CreateStrut(target.Origin.position, target.IsTargetOnly ? 1 : 0.5f);
            this.IsConnectionOrigin = true;
            Util.ResetAllFromTargeting();
            OSD.Success("Link established!");
            ActiveStrutsAddon.Mode = AddonMode.None;
            this.UpdateGui();
        }

        public void SetTargetedBy(ModuleActiveStrut targeter)
        {
            this.OldTargeter = this.Targeter ?? targeter;
            this.Targeter = targeter;
            this.Mode = Mode.Target;
            this.part.SetHighlightColor(Color.green);
            this.part.SetHighlight(true);
        }

        [KSPEvent(name = "ToggleLink", active = false, guiName = "Toggle Link", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void ToggleLink()
        {
            if (this.Mode == Mode.Linked)
            {
                if (this.IsConnectionOrigin)
                {
                    this.Unlink();
                }
                else
                {
                    if (this.Targeter != null)
                    {
                        this.Targeter.Unlink();
                    }
                }
            }
            else if (this.Mode == Mode.Unlinked && ((this.Target != null && this.Target.IsConnectionFree) || (this.Targeter != null && this.Targeter.IsConnectionFree)))
            {
                if (this.Target != null)
                {
                    this.Target.Targeter = this;
                    this.Target.SetAsTarget();
                }
                else if (this.Targeter != null)
                {
                    this.SetAsTarget();
                }
            }
            this.UpdateGui();
        }

        [KSPAction("ToggleLinkAction", KSPActionGroup.None, guiName = "Toggle Link")]
        public void ToggleLinkAction(KSPActionParam param)
        {
            if (this.Mode == Mode.Linked || (this.Mode == Mode.Unlinked && ((this.Target != null && this.Target.IsConnectionFree) || (this.Targeter != null && this.Targeter.IsConnectionFree))))
            {
                this.ToggleLink();
            }
        }

        [KSPEvent(name = "Unlink", active = false, guiName = "Unlink", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void Unlink()
        {
            if (!this.IsTargetOnly && this.Target != null)
            {
                if (this.IsConnectionOrigin && !this.IsFreeAttached)
                {
                    this.Target.Unlink();
                    OSD.Success("Unlinked!");
                }
                if (this.IsFreeAttached)
                {
                    this.IsFreeAttached = false;
                }
                this.Mode = Mode.Unlinked;
                this.IsLinked = false;
                this.DestroyJoint();
                this.DestroyStrut();
                this.LinkType = LinkType.None;
                this.IsConnectionOrigin = false;
                this.UpdateGui();
                return;
            }
            if (this.IsTargetOnly)
            {
                if (!this.AnyTargetersConnected())
                {
                    this.Mode = Mode.Unlinked;
                    this.IsLinked = false;
                }
                this.UpdateGui();
                return;
            }
            this.Mode = Mode.Unlinked;
            this.IsLinked = false;
            this.DestroyStrut();
            this.LinkType = LinkType.None;
            this.UpdateGui();
        }

        public void UpdateGui()
        {
            switch (this.Mode)
            {
                case Mode.Linked:
                {
                    this.Events["Link"].active = this.Events["Link"].guiActive = false;
                    this.Events["SetAsTarget"].active = this.Events["SetAsTarget"].guiActive = false;
                    this.Events["FreeAttach"].active = this.Events["FreeAttach"].guiActive = false;
                    if (!this.IsTargetOnly)
                    {
                        this.Events["Unlink"].active = this.Events["Unlink"].guiActive = true;
                        this.Events["AbortLink"].active = this.Events["AbortLink"].guiActive = false;
                        if (this.IsFreeAttached)
                        {
                            this.Events["ToggleLink"].active = this.Events["ToggleLink"].guiActive = false;
                        }
                        else
                        {
                            this.Events["ToggleLink"].active = this.Events["ToggleLink"].guiActive = true;
                        }
                    }
                    else
                    {
                        this.Events["Unlink"].active = this.Events["Unlink"].guiActive = false;
                        this.Events["ToggleLink"].active = this.Events["ToggleLink"].guiActive = false;
                    }
                }
                    break;
                case Mode.Unlinked:
                {
                    this.Events["SetAsTarget"].active = this.Events["SetAsTarget"].guiActive = false;
                    this.Events["Unlink"].active = this.Events["Unlink"].guiActive = false;
                    if (this.IsTargetOnly)
                    {
                        this.Events["Link"].active = this.Events["Link"].guiActive = false;
                    }
                    else
                    {
                        this.Events["Link"].active = this.Events["Link"].guiActive = true;
                        this.Events["FreeAttach"].active = this.Events["FreeAttach"].guiActive = true;
                        this.Events["AbortLink"].active = this.Events["AbortLink"].guiActive = false;
                        if ((this.Target != null && this.Target.IsConnectionFree) || (this.Targeter != null && this.Targeter.IsConnectionFree))
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
                case Mode.Target:
                {
                    this.Events["SetAsTarget"].active = this.Events["SetAsTarget"].guiActive = true;
                    if (!this.IsTargetOnly)
                    {
                        this.Events["Link"].active = this.Events["Link"].guiActive = false;
                    }
                    this.Events["ToggleLink"].active = this.Events["ToggleLink"].guiActive = false;
                    this.Events["FreeAttach"].active = this.Events["FreeAttach"].guiActive = false;
                }
                    break;
                case Mode.Targeting:
                {
                    this.Events["Link"].active = this.Events["Link"].guiActive = false;
                    this.Events["AbortLink"].active = this.Events["AbortLink"].guiActive = true;
                    this.Events["ToggleLink"].active = this.Events["ToggleLink"].guiActive = false;
                    this.Events["FreeAttach"].active = this.Events["FreeAttach"].guiActive = false;
                }
                    break;
            }
        }

        private void _delayedStart()
        {
            if (this._ticksForDelayedStart > 0)
            {
                this._ticksForDelayedStart--;
                return;
            }
            this._delayedStartFlag = false;
            if (this.Id == Guid.Empty.ToString())
            {
                this.Id = Guid.NewGuid().ToString();
            }
            if (this.IsLinked)
            {
                if (this.IsTargetOnly)
                {
                    this.Mode = Mode.Linked;
                }
                else
                {
                    this.Reconnect();
                }
            }
            else
            {
                this.Mode = Mode.Unlinked;
            }
            this.UpdateGui();
        }
    }
}