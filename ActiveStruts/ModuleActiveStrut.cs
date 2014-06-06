using System;
using System.Linq;
using UnityEngine;

namespace ActiveStruts
{
    public class ModuleActiveStrut : PartModule
    {
        [KSPField(isPersistant = true)] public float FreeFormAttachmentDistance = 0.0f;
        [KSPField(isPersistant = true)] public string FreeFormAttachmentPoint = "0.0 0.0 0.0";
        [KSPField(isPersistant = true, guiActive = true)] public string Id = Guid.Empty.ToString();
        [KSPField(isPersistant = true)] public bool IsConnectionOrigin = false;
        [KSPField(isPersistant = true)] public bool IsFreeFormAttached = false;
        [KSPField(isPersistant = true)] public bool IsHalfWayExtended = false;
        [KSPField(isPersistant = true)] public bool IsLinked = false;
        [KSPField(isPersistant = true)] public bool IsTargetOnly = false;
        //[KSPField(isPersistant = true)] public string RayCastOriginName = "rayCastOrigin";
        [KSPField(isPersistant = false, guiActive = true)] public string State = "n.a.";
        [KSPField(guiActive = true)] public string Strength = LinkType.None.ToString();
        [KSPField(isPersistant = true)] public string StrutName = "strut";
        [KSPField(isPersistant = true)] public string TargetId = Guid.NewGuid().ToString();
        [KSPField(isPersistant = true)] public string TargeterId = Guid.NewGuid().ToString();
        private ConfigurableJoint _joint;
        private LinkType _linkType;
        private Mode _mode = Mode.Undefined;

        private Vector3 FreeAttachPoint
        {
            get
            {
                var coords = this.FreeFormAttachmentPoint.Split(' ').Select(float.Parse).ToArray();
                return new Vector3(coords[0], coords[1], coords[2]);
            }
            set { this.FreeFormAttachmentPoint = string.Format("{0} {1} {2}", value.x, value.y, value.z); }
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

        public Transform Origin;
        public Transform Strut;

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
                return;
            }
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
                    Reconnect();
                }
            }
            else
            {
                this.Mode = Mode.Unlinked;
            }
            this.UpdateGui();
        }

        private void Reconnect()
        {
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
                }
            }
            else
            {
                if (this.IsPossibleTarget(this.Targeter))
                {
                    this.CreateStrut(this.Targeter.Origin.position, 0.5f);
                    this.Mode = Mode.Linked;
                }
            }
        }

        public override void OnUpdate()
        {
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

        public ModuleActiveStrut Targeter
        {
            get { return this.TargeterId == Guid.Empty.ToString() ? null : this.part.vessel.GetStrutById(new Guid(this.TargeterId)); }
            set { this.TargeterId = value.ID.ToString(); }
        }

        public ModuleActiveStrut Target
        {
            get { return this.TargetId == Guid.Empty.ToString() ? null : this.part.vessel.GetStrutById(new Guid(this.TargetId)); }
            set { this.TargetId = value.ID.ToString(); }
        }

        [KSPEvent(name = "Link", active = false, guiName = "Link", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void Link()
        {
            this.Mode = Mode.Targeting;
            foreach (var possibleTarget in this.GetAllPossibleTargets())
            {
                possibleTarget.SetTargetedBy(this);
                possibleTarget.UpdateGui();
                Debug.Log("[AS] setting " + possibleTarget.ID + " as target");
            }
            ActiveStrutsAddon.Mode = AddonMode.Link;
            ActiveStrutsAddon.CurrentTargeter = this;
            OSD.Info(Config.LinkHelpText, 5);
            this.UpdateGui();
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

        [KSPEvent(name = "AbortLink", active = false, guiName = "Abort Link", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void AbortLink()
        {
            this.Mode = Mode.Unlinked;
            Util.ResetAllFromTargeting();
            ActiveStrutsAddon.Mode = AddonMode.None;
            OSD.Info("Link aborted.");
            this.UpdateGui();
        }

        [KSPEvent(name = "SetAsTarget", active = false, guiName = "Set as Target", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void SetAsTarget()
        {
            this.Targeter.SetTarget(this);
            this.IsLinked = true;
            this.part.SetHighlightDefault();
            this.Mode = Mode.Linked;
            this.IsConnectionOrigin = false;
            if (!this.IsTargetOnly)
            {
                this.CreateStrut(this.Targeter.Origin.position, 0.5f);
            }
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

        [KSPEvent(name = "Unlink", active = false, guiName = "Unlink", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void Unlink()
        {
            if (!this.IsTargetOnly && this.Target != null)
            {
                if (this.IsConnectionOrigin)
                {
                    this.Target.Unlink();
                    OSD.Success("Unlinked!");
                }
                this.Mode = Mode.Unlinked;
                this.IsLinked = false;
                this.DestroyJoint();
                this.DestroyStrut();
                this.UpdateGui();
                this.IsConnectionOrigin = false;
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

        public void UpdateGui()
        {
            switch (Mode)
            {
                case Mode.Linked:
                {
                    this.Events["Link"].active = this.Events["Link"].guiActive = false;
                    this.Events["SetAsTarget"].active = this.Events["SetAsTarget"].guiActive = false;
                    if (!this.IsTargetOnly)
                    {
                        this.Events["Unlink"].active = this.Events["Unlink"].guiActive = true;
                        this.Events["AbortLink"].active = this.Events["AbortLink"].guiActive = false;
                        this.Events["ToggleLink"].active = this.Events["ToggleLink"].guiActive = true;
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
                    if (this.IsTargetOnly)
                    {
                        this.Events["Link"].active = this.Events["Link"].guiActive = false;
                    }
                    else
                    {
                        this.Events["Link"].active = this.Events["Link"].guiActive = true;
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
                }
                    break;
                case Mode.Targeting:
                {
                    this.Events["Link"].active = this.Events["Link"].guiActive = false;
                    this.Events["AbortLink"].active = this.Events["AbortLink"].guiActive = true;
                    this.Events["ToggleLink"].active = this.Events["ToggleLink"].guiActive = false;
                }
                    break;
            }
        }

        public void SetTargetedBy(ModuleActiveStrut targeter)
        {
            this.Targeter = targeter;
            this.Mode = Mode.Target;
            this.part.SetHighlightColor(Color.green);
            this.part.SetHighlight(true);
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
            this.Target.LinkType = type;
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
    }
}