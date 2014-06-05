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
        private static bool _valid;
        private static bool _listenForLeftClick;
        public static bool Active { get; set; }

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
            ASUtil.RestoreEventsOnAllTargeters();
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
}