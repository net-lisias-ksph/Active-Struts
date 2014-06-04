using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ActiveStruts
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ConnectorManager : MonoBehaviour
    {
        private const float ConnectorDimension = 0.05f;
        private const string _path = "ActiveStruts/IR_LoneStrut/";
        private static GameObject _connector;
        private static ModuleActiveStrutTargeter _targeter;
        private static Vector3? _origin;
        private static Vector3 _target;
        private static bool _initialized;
        public static bool Active { get; set; }
        private static bool _valid;
        private static bool _listenForLeftClick;

        public static void Activate(ModuleActiveStrutTargeter origin, Vector3 originVector, bool listenForLeftClick)
        {
            _targeter = origin;
            _origin = originVector;
            Active = true;
            _connector.SetActive(true);
            _listenForLeftClick = listenForLeftClick;
        }

        public static void Deactivate()
        {
            Active = false;
            _connector.SetActive(false);
            _listenForLeftClick = false;
        }

        public void Start()
        {
            _connector = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _connector.name = "ASConn";
            DestroyImmediate(_connector.collider);
            _connector.transform.localScale = new Vector3(ConnectorDimension, ConnectorDimension, ConnectorDimension);
            //Debug.Log("trying to set mesh stuff");
            var mr = _connector.GetComponent<MeshRenderer>();
            mr.name = "ASConn";
            mr.material = new Material(Shader.Find("Diffuse")) {mainTexture = GameDatabase.Instance.GetTexture(_path + "IR_Robotic.tga", false)};
            var greenRgb = ASUtil.GetRgbaFromColor(Color.green);
            mr.material.color = new Color(greenRgb[0], greenRgb[1], greenRgb[2], 0.5f);
            //TODO meshrenderer
            _initialized = true;
            //Debug.Log("conn init");
            _connector.SetActive(false);
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight || !Active || !_initialized)
            {
                return;
            }
            //Debug.Log("would paint conn between [" + _origin.x + ", " + _origin.y + ", " + _origin.z + "] and [" + _target.x + ", " + _target.y + ", " + _target.z + "]");
            //_target = FlightCamera.fetch.mainCamera.ScreenToWorldPoint(Input.mousePosition);
            //var ray = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
            //RaycastHit hit;
            //Physics.Raycast(ray, out hit, 557059);
            var mpd = ASUtil.GetCurrentMousePositionData(_origin, _targeter.transform.up);
            if (mpd == null || !mpd.OriginValid)
            {
                _connector.SetActive(false);
                _valid = false;
                _updateColor();
                return;
            }
            Debug.Log("[AS] hitcurrvess: " + mpd.HitCurrentVessel + "; distfromorigin: " + mpd.DistanceFromReferenceOriginExact + "; anglefromorigin: " + mpd.AngleFromOriginExact);
            _valid = mpd.HitCurrentVessel && mpd.DistanceFromReferenceOriginExact <= ModuleActiveStrutBase.MaxDistance && mpd.AngleFromOriginExact <= 90;
            _connector.SetActive(true);
            _target = mpd.ExactHitPosition;
            var trans = _connector.transform;
            trans.position = _origin ?? Vector3.zero;
            trans.LookAt(_target);
            var usableOrigin = _origin ?? Vector3.zero;
            trans.localScale = new Vector3(usableOrigin.x, usableOrigin.y, 1);
            //trans.localScale = new Vector3(_origin.x, _origin.y, Vector3.Distance(Vector3.zero, trans.InverseTransformPoint(_target)));
            //trans.localScale = new Vector3(1, 1, Vector3.Distance(Vector3.zero, trans.InverseTransformPoint(_target)));
            var dist = Vector3.Distance(Vector3.zero, trans.InverseTransformPoint(_target))/2.0f;
            trans.localScale = new Vector3(0.05f, dist, 0.05f);
            trans.Rotate(new Vector3(0, 0, 1), 90f);
            trans.Rotate(new Vector3(1, 0, 0), 90f);
            trans.Translate(new Vector3(0f, dist, 0f));
            _updateColor();
            if (_listenForLeftClick && Input.GetKeyDown(KeyCode.Mouse0) && mpd.HitCurrentVessel)
            {
                _targeter.FreeAttachRequest(mpd);
            }
            if (_listenForLeftClick && Input.GetKeyDown(KeyCode.Mouse1))
            {
                _targeter.AbortAttachRequest();
            }
        }

        private static Color _colorToTransparentColor(Color color)
        {
            var rgba = ASUtil.GetRgbaFromColor(color);
            return new Color(rgba[0], rgba[1], rgba[2], 0.5f);
        }

        private static void _updateColor()
        {
            var mat = (_connector.GetComponent<MeshRenderer>()).material;
            mat.color = _valid ? _colorToTransparentColor(Color.green) : _colorToTransparentColor(Color.red);
        }
    }

    public class MousePositionData
    {
        public float AngleFromOriginExact { get; set; }
        public float AngleFromOriginPart { get; set; }
        public float DistanceFromReferenceOriginExact { get; set; }
        public float DistanceFromReferenceOriginPart { get; set; }
        public Vector3 ExactHitPosition { get; set; }
        public RaycastHit Hit { get; set; }
        public bool HitCurrentVessel { get; set; }
        public Part HittedPart { get; set; }
        public bool OriginValid { get; set; }
        public Vector3 PartHitPosition { get; set; }
        public Ray Ray { get; set; }
        public float RayDistance { get; set; }
        public Vector3 ReferenceOrigin { get; set; }
    }

    public static class ASUtil
    {
        public static List<ModuleActiveStrutBase> GetAllDockingStrutModules(Vessel vessel)
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

        public static float[] GetRgbaFromColor(Color color)
        {
            var ret = new float[4];
            ret[0] = color.r;
            ret[1] = color.g;
            ret[2] = color.b;
            ret[3] = color.a;
            return ret;
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

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class OSDInfo : MonoBehaviour
    {
        // ReSharper disable once InconsistentNaming
        public void OnGUI()
        {
            OSD.Update();
        }
    }

    /*  Copyright (C) 2013 FW Industries
        Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
        The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
    */

    public static class OSD
    {
        private const string Prefix = "[ActiveStruts] ";

        private static readonly List<Message> Msgs = new List<Message>();

        public static void AddMessage(String text, Color color, float shownFor = 3.7f)
        {
            var msg = new Message {Text = Prefix + text, Color = color, HideAt = Time.time + shownFor};
            Msgs.Add(msg);
        }

        private static float CalcHeight()
        {
            var style = CreateStyle(Color.white);
            return Msgs.Aggregate(.0f, (a, m) => a + style.CalcSize(new GUIContent(m.Text)).y);
        }

        private static GUIStyle CreateStyle(Color color)
        {
            var style = new GUIStyle {stretchWidth = true, alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold, normal = {textColor = color}};
            return style;
        }

        public static void Error(String text)
        {
            AddMessage(text, XKCDColors.LightRed);
        }

        public static void Info(String text)
        {
            AddMessage(text, XKCDColors.OffWhite);
        }

        public static void Success(String text)
        {
            AddMessage(text, XKCDColors.Cerulean);
        }

        public static void Update()
        {
            if (Msgs.Count == 0)
            {
                return;
            }
            Msgs.RemoveAll(m => Time.time >= m.HideAt);
            var h = CalcHeight();
            GUILayout.BeginArea(new Rect(0, Screen.height*0.1f, Screen.width, h), CreateStyle(Color.white));
            Msgs.ForEach(m => GUILayout.Label(m.Text, CreateStyle(m.Color)));
            GUILayout.EndArea();
        }

        public static void Warn(String text)
        {
            AddMessage(text, XKCDColors.Yellow);
        }

        private class Message
        {
            public Color Color { get; set; }
            public float HideAt { get; set; }
            public String Text { get; set; }
        }
    }
}