/*
The MIT License (MIT)
Copyright (c) 2014 marce

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Linq;
using ActiveStruts.Addons;
using ActiveStruts.Util;
using UnityEngine;

namespace ActiveStruts.Modules
{
    public class ModuleActiveStrut : PartModule
    {
        private const ControlTypes EditorLockMask = ControlTypes.EDITOR_PAD_PICK_PLACE | ControlTypes.EDITOR_ICON_PICK;
        [KSPField(isPersistant = true)] public string FreeAttachTargetId = Guid.Empty.ToString();
        [KSPField(isPersistant = true)] public string Id = Guid.Empty.ToString();
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
        private float _jointBrokeForce;
        private bool _jointBroken;
        private LinkType _linkType;
        private Mode _mode = Mode.Undefined;
        private int _strutRealignCounter;
        private int _ticksForDelayedStart;
        private readonly object _freeAttachStrutUpdateLock = new object();
        private ModuleActiveStrutFreeAttachTarget _freeAttachTarget;

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

        public ModuleActiveStrutFreeAttachTarget FreeAttachTarget
        {
            get { return this._freeAttachTarget ?? (this._freeAttachTarget = Util.Util.FindFreeAttachTarget(new Guid(this.FreeAttachTargetId))); }
            set
            {
                this.FreeAttachTargetId = value != null ? value.ID.ToString() : Guid.Empty.ToString();
                _freeAttachTarget = value;
            }
        }

        public Guid ID
        {
            get
            {
                if (this.Id == null || new Guid(this.Id) == Guid.Empty)
                {
                    this.Id = Guid.NewGuid().ToString();
                }
                return new Guid(this.Id);
            }
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
            get { return this.TargetId == Guid.Empty.ToString() ? null : Util.Util.GetStrutById(new Guid(this.TargetId)); }
            set { this.TargetId = value != null ? value.ID.ToString() : Guid.Empty.ToString(); }
        }

        public ModuleActiveStrut Targeter
        {
            get { return this.TargeterId == Guid.Empty.ToString() ? null : Util.Util.GetStrutById(new Guid(this.TargeterId)); }
            set { this.TargeterId = value != null ? value.ID.ToString() : Guid.Empty.ToString(); }
        }

        [KSPEvent(name = "AbortLink", active = false, guiName = "Abort Link", guiActiveEditor = true, guiActiveUnfocused = true, unfocusedRange = 50)]
        public void AbortLink()
        {
            this.Mode = Mode.Unlinked;
            Util.Util.ResetAllFromTargeting();
            ActiveStrutsAddon.Mode = AddonMode.None;
            OSD.Info("Link aborted.");
            this.UpdateGui();
        }

        public void CreateJoint(Rigidbody originBody, Rigidbody targetBody, LinkType type, Vector3 anchorPosition)
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
            this._joint.projectionAngle = 0f;
            this._joint.projectionDistance = 0f;
            this._joint.anchor = anchorPosition;
            this.LinkType = type;
            if (!this.IsFreeAttached)
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
            if (this.IsFreeAttached)
            {
                distance -= Config.Instance.FreeAttachStrutExtension;
            }
            this.Strut.localScale = new Vector3(1, 1, distance);
        }

        public void DestroyJoint()
        {
            DestroyImmediate(this._joint);
            this._joint = null;
            this.LinkType = LinkType.None;
        }

        public void DestroyStrut()
        {
            this.Strut.localScale = Vector3.zero;
        }

        [KSPEvent(name = "FreeAttach", active = false, guiActiveEditor = true, guiName = "FreeAttach Link", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void FreeAttach()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                InputLockManager.SetControlLock(EditorLockMask, Config.Instance.EditorInputLockId);
            }
            OSD.Info(Config.Instance.FreeAttachHelpText);
            ActiveStrutsAddon.CurrentTargeter = this;
            ActiveStrutsAddon.Mode = AddonMode.FreeAttach;
        }

        [KSPEvent(name = "FreeAttachStraight", active = false, guiName = "Straight Up FreeAttach", guiActiveUnfocused = true, unfocusedRange = 50)]
        public void FreeAttachStraight()
        {
            var ray = new Ray(this.Origin.position, this.Origin.transform.right);
            RaycastHit info;
            var raycast = Physics.Raycast(ray, out info, Config.Instance.MaxDistance);
            if (raycast)
            {
                var hittedPart = info.PartFromHit();
                var valid = hittedPart != null;
                if (HighLogic.LoadedSceneIsFlight && valid)
                {
                    valid = hittedPart.vessel == this.vessel;
                }
                if (valid)
                {
                    this.PlaceFreeAttach(hittedPart, info.point);
                }
            }
            else
            {
                OSD.Warn("Nothing has been hit.");
            }
        }

        [KSPAction("FreeAttachStraightAction", KSPActionGroup.None, guiName = "Straight FreeAttach")]
        public void FreeAttachStraightAction(KSPActionParam param)
        {
            if (this.Mode == Mode.Unlinked && !this.IsTargetOnly)
            {
                this.FreeAttachStraight();
            }
        }

        [KSPEvent(name = "Link", active = false, guiName = "Link", guiActiveEditor = true, guiActiveUnfocused = true, unfocusedRange = 50)]
        public void Link()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                InputLockManager.SetControlLock(EditorLockMask, Config.Instance.EditorInputLockId);
            }
            this.Mode = Mode.Targeting;
            foreach (var possibleTarget in this.GetAllPossibleTargets())
            {
                possibleTarget.SetTargetedBy(this);
                possibleTarget.UpdateGui();
            }
            ActiveStrutsAddon.Mode = AddonMode.Link;
            ActiveStrutsAddon.CurrentTargeter = this;
            OSD.Info(Config.Instance.LinkHelpText, 5);
            this.UpdateGui();
        }

        public void OnJointBreak(float breakForce)
        {
            this._jointBroken = true;
            this._jointBrokeForce = breakForce;
        }

        public override void OnStart(StartState state)
        {
            Debug.Log("[AS] test if OnStart gets ever called");
            if (!this.IsTargetOnly)
            {
                this.Strut = this.part.FindModelTransform(this.StrutName);
                DestroyImmediate(this.Strut.collider);
                this.DestroyStrut();
            }
            this.Origin = this.part.transform;
            this._delayedStartFlag = true;
            this._ticksForDelayedStart = HighLogic.LoadedSceneIsEditor ? 0 : Config.Instance.StartDelay;
            this._strutRealignCounter = Config.Instance.StrutRealignInterval*(HighLogic.LoadedSceneIsEditor ? 6 : 0);
        }

        public override void OnUpdate()
        {
            if (this._delayedStartFlag)
            {
                this._delayedStart();
                return;
            }
            if (this._jointBroken)
            {
                this._jointBroken = false;
                var strength = this.LinkType.GetJointStrength();
                var diff = this._jointBrokeForce - strength;
                this.Unlink();
                OSD.Warn("Joint broken! Applied force was " + this._jointBrokeForce.ToString("R") + " while the joint could only take " + strength.ToString("R") + " (difference: " + diff.ToString("R") + ")", 5);
                return;
            }
            if (this.IsLinked)
            {
                if (this._strutRealignCounter > 0)
                {
                    this._strutRealignCounter--;
                }
                else
                {
                    this._strutRealignCounter = Config.Instance.StrutRealignInterval;
                    this._realignStrut();
                }
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
                    if (this.FreeAttachPart != null && (HighLogic.LoadedSceneIsEditor || this.FreeAttachPart.vessel == this.vessel))
                    {
                        return;
                    }
                    this.Unlink();
                    return;
                }
                if (this.IsConnectionOrigin)
                {
                    if (this.Target != null && (HighLogic.LoadedSceneIsEditor || this.Target.vessel == this.vessel))
                    {
                        return;
                    }
                    this.DestroyJoint();
                    this.DestroyStrut();
                    this.Mode = Mode.Unlinked;
                }
                else
                {
                    if (this.Targeter != null && (HighLogic.LoadedSceneIsEditor || this.Targeter.vessel == this.vessel))
                    {
                        return;
                    }
                    this.DestroyStrut();
                    this.Mode = Mode.Unlinked;
                }
                this.UpdateGui();
            }
        }

        public void PlaceFreeAttach(Part hittedPart, Vector3 hitPosition)
        {
            lock (_freeAttachStrutUpdateLock)
            {
                ActiveStrutsAddon.Mode = AddonMode.None;
                if (!hittedPart.Modules.Contains(Config.Instance.ModuleActiveStrutFreeAttachTarget))
                {
                    hittedPart.AddModule(Config.Instance.ModuleActiveStrutFreeAttachTarget);
                }
                var target = hittedPart.Modules[Config.Instance.ModuleActiveStrutFreeAttachTarget] as ModuleActiveStrutFreeAttachTarget;
                if (target != null)
                {
                    this.FreeAttachTarget = target;
                    ActiveStrutsEditorAddon.AddModuleActiveStrutFreeAttachTarget(target);
                }
                this.Mode = Mode.Linked;
                this.IsLinked = true;
                this.IsFreeAttached = true;
                this.IsConnectionOrigin = true;
                this.DestroyJoint();
                this.DestroyStrut();
                if (target != null)
                {
                    this.CreateJoint(this.part.rigidbody, target.PartRigidbody, LinkType.Weak, (hitPosition + this.Origin.position)/2);
                }
                this.CreateStrut(hitPosition);
                this.Target = null;
                this.Targeter = null;
                OSD.Success("FreeAttach Link established!");
            }
            this.UpdateGui();
        }

        private void Reconnect()
        {
            if (this.IsFreeAttached)
            {
                if (this.FreeAttachTarget != null)
                {
                    Debug.Log("[AS] should reconnect free attach strut");
                    var check = this.CheckFreeAttachPoint();
                    var rayRes = Util.Util.PerformRaycast(Origin.position, FreeAttachTarget.PartOrigin.position, Origin.right);
                    if (rayRes.HitCurrentVessel && rayRes.HittedPart != null && rayRes.DistanceFromOrigin <= Config.Instance.MaxDistance)
                    {
                        Debug.Log("[AS] linking free attach strut now...");
                        this.PlaceFreeAttach(rayRes.HittedPart, rayRes.Hit.point);
                        this.UpdateGui();
                        return;
                    }
                }
                Debug.Log("[AS] free attach target seems to be null");
                this.IsFreeAttached = false;
                this.Mode = Mode.Unlinked;
                this.IsConnectionOrigin = false;
                this.LinkType = LinkType.None;
                this.UpdateGui();
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
                    this.CreateJoint(this.part.rigidbody, this.Target.part.rigidbody, this.Target.IsTargetOnly ? LinkType.Normal : LinkType.Maximal, this.Target.transform.position);
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

        [KSPEvent(name = "SetAsTarget", active = false, guiName = "Set as Target", guiActiveEditor = true, guiActiveUnfocused = true, unfocusedRange = 50)]
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
            this.CreateJoint(this.part.rigidbody, target.part.rigidbody, target.IsTargetOnly ? LinkType.Normal : LinkType.Maximal, this.Target.transform.position);
            this.CreateStrut(target.Origin.position, target.IsTargetOnly ? 1 : 0.5f);
            this.IsConnectionOrigin = true;
            Util.Util.ResetAllFromTargeting();
            OSD.Success("Link established!");
            ActiveStrutsAddon.Mode = AddonMode.None;
            this.UpdateGui();
        }

        public void SetTargetedBy(ModuleActiveStrut targeter)
        {
            this.OldTargeter = this.Targeter;
            this.Targeter = targeter;
            this.Mode = Mode.Target;
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
                    if (this.IsPossibleTarget(this.Target))
                    {
                        this.Target.Targeter = this;
                        this.Target.SetAsTarget();
                    }
                    else
                    {
                        OSD.Warn("Can't relink at the moment, target may be obstructed.");
                    }
                }
                else if (this.Targeter != null)
                {
                    if (this.Targeter.IsPossibleTarget(this))
                    {
                        this.SetAsTarget();
                    }
                    else
                    {
                        OSD.Warn("Can't relink at the moment, targeter may be obstructed.");
                    }
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

        [KSPEvent(name = "Unlink", active = false, guiName = "Unlink", guiActiveEditor = true, guiActiveUnfocused = true, unfocusedRange = 50)]
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
            if (this.IsFreeAttached)
            {
                this.IsFreeAttached = false;
            }
            this.FreeAttachTarget = null;
            this.Mode = Mode.Unlinked;
            this.IsLinked = false;
            this.DestroyStrut();
            this.DestroyJoint();
            this.LinkType = LinkType.None;
            this.UpdateGui();
        }

        public void UpdateGui()
        {
            if (HighLogic.LoadedSceneIsFlight)
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
                            this.Events["AbortLink"].active = this.Events["AbortLink"].guiActive = false;
                            if (this.IsFreeAttached)
                            {
                                this.Events["ToggleLink"].active = this.Events["ToggleLink"].guiActive = false;
                                this.Events["Unlink"].active = this.Events["Unlink"].guiActive = true;
                            }
                            else
                            {
                                this.Events["ToggleLink"].active = this.Events["ToggleLink"].guiActive = true;
                                this.Events["Unlink"].active = this.Events["Unlink"].guiActive = false;
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
                this.Events["FreeAttachStraight"].active = this.Events["FreeAttachStraight"].guiActive = this.Events["FreeAttach"].active;
            }
            else if (HighLogic.LoadedSceneIsEditor)
            {
                this.Events["ToggleLink"].active = this.Events["ToggleLink"].guiActive = this.Events["ToggleLink"].guiActiveEditor = false;
                switch (this.Mode)
                {
                    case Mode.Linked:
                    {
                        if (!this.IsTargetOnly)
                        {
                            this.Events["Unlink"].active = this.Events["Unlink"].guiActive = this.Events["Unlink"].guiActiveEditor = true;
                        }
                        this.Events["Link"].active = this.Events["Link"].guiActive = this.Events["Link"].guiActiveEditor = false;
                        this.Events["SetAsTarget"].active = this.Events["SetAsTarget"].guiActive = this.Events["SetAsTarget"].guiActiveEditor = false;
                        this.Events["AbortLink"].active = this.Events["AbortLink"].guiActive = this.Events["AbortLink"].guiActiveEditor = false;
                        this.Events["FreeAttach"].active = this.Events["FreeAttach"].guiActive = this.Events["FreeAttach"].guiActiveEditor = false;
                    }
                        break;
                    case Mode.Unlinked:
                    {
                        this.Events["Unlink"].active = this.Events["Unlink"].guiActive = this.Events["Unlink"].guiActiveEditor = false;
                        this.Events["SetAsTarget"].active = this.Events["SetAsTarget"].guiActive = this.Events["SetAsTarget"].guiActiveEditor = false;
                        this.Events["AbortLink"].active = this.Events["AbortLink"].guiActive = this.Events["AbortLink"].guiActiveEditor = false;
                        if (!this.IsTargetOnly)
                        {
                            this.Events["Link"].active = this.Events["Link"].guiActive = this.Events["Link"].guiActiveEditor = true;
                            this.Events["FreeAttach"].active = this.Events["FreeAttach"].guiActive = this.Events["FreeAttach"].guiActiveEditor = true;
                        }
                    }
                        break;
                    case Mode.Target:
                    {
                        this.Events["Unlink"].active = this.Events["Unlink"].guiActive = this.Events["Unlink"].guiActiveEditor = false;
                        this.Events["Link"].active = this.Events["Link"].guiActive = this.Events["Link"].guiActiveEditor = false;
                        this.Events["SetAsTarget"].active = this.Events["SetAsTarget"].guiActive = this.Events["SetAsTarget"].guiActiveEditor = true;
                        this.Events["AbortLink"].active = this.Events["AbortLink"].guiActive = this.Events["AbortLink"].guiActiveEditor = false;
                        this.Events["FreeAttach"].active = this.Events["FreeAttach"].guiActive = this.Events["FreeAttach"].guiActiveEditor = false;
                    }
                        break;
                    case Mode.Targeting:
                    {
                        this.Events["Unlink"].active = this.Events["Unlink"].guiActive = this.Events["Unlink"].guiActiveEditor = false;
                        this.Events["Link"].active = this.Events["Link"].guiActive = this.Events["Link"].guiActiveEditor = false;
                        this.Events["SetAsTarget"].active = this.Events["SetAsTarget"].guiActive = this.Events["SetAsTarget"].guiActiveEditor = false;
                        this.Events["AbortLink"].active = this.Events["AbortLink"].guiActive = this.Events["AbortLink"].guiActiveEditor = true;
                        this.Events["FreeAttach"].active = this.Events["FreeAttach"].guiActive = this.Events["FreeAttach"].guiActiveEditor = false;
                    }
                        break;
                }
                this.Events["FreeAttachStraight"].active = this.Events["FreeAttachStraight"].guiActive = this.Events["FreeAttachStraight"].guiActiveEditor = this.Events["FreeAttach"].active;
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
            if (this.IsLinked && !HighLogic.LoadedSceneIsEditor)
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

        private void _realignStrut()
        {
            if (this.IsFreeAttached)
            {
                lock (_freeAttachStrutUpdateLock)
                {
                    var targetPos = Util.Util.PerformRaycast(this.Origin.position, this.FreeAttachTarget.PartOrigin.position, this.Origin.right).Hit.point;
                    this.DestroyStrut();
                    this.CreateStrut(targetPos);
                }
            }
            else if (!this.IsTargetOnly)
            {
                if (this.Target == null)
                {
                    return;
                }
                this.DestroyStrut();
                if (this.Target.IsTargetOnly)
                {
                    this.CreateStrut(this.Target.Origin.position);
                }
                else
                {
                    this.Target.DestroyStrut();
                    this.CreateStrut(this.Target.Origin.position, 0.5f);
                    this.Target.CreateStrut(this.Origin.position, 0.5f);
                }
            }
        }
    }
}