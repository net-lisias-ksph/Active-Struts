using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ActiveStruts.Util
{
    public class Config
    {
        private class SettingsEntry
        {
            public object Value { get; set; }
            public object DefaultValue { get; private set; }

            public SettingsEntry(object defaultValue)
            {
                this.DefaultValue = defaultValue;
            }
        }

        public float ColorTransparency
        {
            get { return (float) _getValue<double>("ColorTransparency"); }
        }

        public float ConnectorDimension
        {
            get { return (float) _getValue<double>("ConnectorDimension"); }
        }

        public float FreeAttachDistanceTolerance
        {
            get { return (float) _getValue<double>("FreeAttachDistanceTolerance"); }
        }

        // ReSharper disable once InconsistentNaming
        private const string _freeAttachHelpText = "Click left on a valid position to establish a link. Press 'x' to abort.";

        public string FreeAttachHelpText
        {
            get { return _freeAttachHelpText; }
        }

        public float FreeAttachStrutExtension
        {
            get { return (float) _getValue<double>("FreeAttachStrutExtension"); }
        }

        // ReSharper disable once InconsistentNaming
        private const string _linkHelpText = "Click left on a possible target to establish a link. Press 'x' to abort. You can also right click -> 'Set as Target' on a valid target and right click -> 'Abort' on the targeter.";

        public string LinkHelpText
        {
            get { return _linkHelpText; }
        }

        public float MaxAngle
        {
            get { return (float) _getValue<double>("MaxAngle"); }
        }

        public float MaxDistance
        {
            get { return (float) _getValue<double>("MaxDistance"); }
        }

        public float MaximalJointStrength
        {
            get { return (float) _getValue<double>("MaximalJointStrength"); }
        }

        // ReSharper disable once InconsistentNaming
        private const string _moduleName = "ModuleActiveStrut";

        public string ModuleName
        {
            get { return _moduleName; }
        }

        public float NormalJointStrength
        {
            get { return (float) _getValue<double>("NormalJointStrength"); }
        }

        public int StartDelay
        {
            get { return _getValue<int>("StartDelay"); }
        }

        public int StrutRealignInterval
        {
            get { return _getValue<int>("StrutRealignInterval"); }
        }

        public float WeakJointStrength
        {
            get { return (float) _getValue<double>("WeakJointStrength"); }
        }

        // ReSharper disable once InconsistentNaming
        private const string _editorInputLockId = "[AS] temp editor lock";

        // ReSharper disable once InconsistentNaming
        private const string _invisibleCubeName = "AS_IC_1";

        public string InvisibleCubeName
        {
            get { return _invisibleCubeName; }
        }

        public string EditorInputLockId
        {
            get { return _editorInputLockId; }
        }

        private const string ConfigFilePath = "GameData/ActiveStruts/Plugin/ActiveStruts.cfg";
        private const string SettingsNodeName = "ACTIVE_STRUTS_SETTINGS";
        private static readonly Dictionary<string, SettingsEntry> Values = new Dictionary<string, SettingsEntry>
                                                                           {
                                                                               {"MaxDistance", new SettingsEntry(15)},
                                                                               {"MaxAngle", new SettingsEntry(95)},
                                                                               {"WeakJointStrength", new SettingsEntry(50)},
                                                                               {"NormalJointStrength", new SettingsEntry(500)},
                                                                               {"MaximalJointStrength", new SettingsEntry(2000)},
                                                                               {"ConnectorDimension", new SettingsEntry(0.5f)},
                                                                               {"ColorTransparency", new SettingsEntry(0.5f)},
                                                                               {"FreeAttachDistanceTolerance", new SettingsEntry(0.1f)},
                                                                               {"FreeAttachStrutExtension", new SettingsEntry(0.05f)},
                                                                               {"StartDelay", new SettingsEntry(60)},
                                                                               {"StrutRealignInterval", new SettingsEntry(5)}
                                                                           };

        private static Config _instance;

        public static Config Instance
        {
            get { return _instance ?? (_instance = new Config()); }
        }

        private static T _getValue<T>(string key)
        {
            if (!Values.ContainsKey(key))
            {
                throw new ArgumentException();
            }
            var val = Values[key];
            var ret = val.Value ?? val.DefaultValue;
            return (T) Convert.ChangeType(ret, typeof(T));
        }

        private static bool _configFileExists()
        {
            return File.Exists(ConfigFilePath);
        }

        private static void _initialSave()
        {
            ConfigNode node = new ConfigNode(), settings = new ConfigNode(SettingsNodeName);
            foreach (var settingsEntry in Values)
            {
                settings.AddValue(settingsEntry.Key, settingsEntry.Value.DefaultValue);
            }
            node.AddNode(settings);
            node.Save(ConfigFilePath);
        }

        private static void _load()
        {
            var node = ConfigNode.Load(ConfigFilePath);
            var settings = node.GetNode(SettingsNodeName);
            foreach (var settingsEntry in Values)
            {
                var val = settings.GetValue(settingsEntry.Key);
                if (val != null)
                {
                    settingsEntry.Value.Value = val;
                }
            }
        }

        private Config()
        {
            if (!_configFileExists())
            {
                _initialSave();
                Thread.Sleep(500);
            }
            _load();
        }
    }
}