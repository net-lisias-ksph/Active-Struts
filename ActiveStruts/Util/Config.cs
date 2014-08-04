using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ActiveStruts.Util
{
    public class Config
    {
        // ReSharper disable once InconsistentNaming
        private const string _freeAttachHelpText = "Left-Click on a valid position to establish a link. Press 'x' to abort.";
        // ReSharper disable once InconsistentNaming
        private const string _linkHelpText = "Left-Click on a possible target to establish a link. Press 'x' to abort or use the 'Abort Link' button.";
        // ReSharper disable once InconsistentNaming
        private const string _moduleName = "ModuleActiveStrut";
        // ReSharper disable once InconsistentNaming
        private const string _editorInputLockId = "[AS] editor lock";
        public const float UnfocusedRange = 3f;
        public const int TargetHighlightDuration = 3;
        // ReSharper disable once InconsistentNaming
        private const string _moduleActiveStrutFreeAttachTarget = "ModuleActiveStrutFreeAttachTarget";
        private const string ConfigFilePath = "GameData/ActiveStruts/Plugin/ActiveStruts.cfg";
        private const string SettingsNodeName = "ACTIVE_STRUTS_SETTINGS";
        public const int IdResetCheckInterval = 120;
        private static readonly Dictionary<string, SettingsEntry> Values = new Dictionary<string, SettingsEntry>
                                                                           {
                                                                               {"MaxDistance", new SettingsEntry(15)},
                                                                               {"MaxAngle", new SettingsEntry(95)},
                                                                               {"WeakJointStrength", new SettingsEntry(1)},
                                                                               {"NormalJointStrength", new SettingsEntry(5)},
                                                                               {"MaximalJointStrength", new SettingsEntry(50)},
                                                                               {"ConnectorDimension", new SettingsEntry(0.5f)},
                                                                               {"ColorTransparency", new SettingsEntry(0.3f)},
                                                                               {"FreeAttachDistanceTolerance", new SettingsEntry(0.1f)},
                                                                               {"FreeAttachStrutExtension", new SettingsEntry(0.05f)},
                                                                               {"StartDelay", new SettingsEntry(60)},
                                                                               {"StrutRealignInterval", new SettingsEntry(5)},
                                                                               {"SoundAttachFile", new SettingsEntry("ActiveStruts/Sounds/AS_Attach")},
                                                                               {"SoundDetachFile", new SettingsEntry("ActiveStruts/Sounds/AS_Detach")},
                                                                               {"SoundBreakFile", new SettingsEntry("ActiveStruts/Sounds/AS_Break")},
                                                                               {"GlobalJointEnforcement", new SettingsEntry(false)},
                                                                               {"GlobalJointWeakness", new SettingsEntry(false)},
                                                                               {"StrutRealignDistanceTolerance", new SettingsEntry(0.02f)},
                                                                               {"EnableDocking", new SettingsEntry(false)},
                                                                               {"ShowHelpTexts", new SettingsEntry(true)}
                                                                           };

        private static Config _instance;

        public float ColorTransparency
        {
            get { return (float) _getValue<double>("ColorTransparency"); }
        }

        public float ConnectorDimension
        {
            get { return (float) _getValue<double>("ConnectorDimension"); }
        }

        public bool DockingEnabled
        {
            get { return _getValue<bool>("EnableDocking"); }
        }

        public string EditorInputLockId
        {
            get { return _editorInputLockId; }
        }

        public float FreeAttachDistanceTolerance
        {
            get { return (float) _getValue<double>("FreeAttachDistanceTolerance"); }
        }

        // ReSharper disable once InconsistentNaming

        public string FreeAttachHelpText
        {
            get { return _freeAttachHelpText; }
        }

        public float FreeAttachStrutExtension
        {
            get { return (float) _getValue<double>("FreeAttachStrutExtension"); }
        }

        public bool GlobalJointEnforcement
        {
            get { return _getValue<bool>("GlobalJointEnforcement"); }
        }

        public bool GlobalJointWeakness
        {
            get { return _getValue<bool>("GlobalJointWeakness"); }
        }

        public static Config Instance
        {
            get { return _instance ?? (_instance = new Config()); }
        }

        // ReSharper disable once InconsistentNaming

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

        public string ModuleActiveStrutFreeAttachTarget
        {
            get { return _moduleActiveStrutFreeAttachTarget; }
        }

        // ReSharper disable once InconsistentNaming

        public string ModuleName
        {
            get { return _moduleName; }
        }

        public float NormalJointStrength
        {
            get { return (float) _getValue<double>("NormalJointStrength"); }
        }

        public bool ShowHelpTexts
        {
            get { return _getValue<bool>("ShowHelpTexts"); }
        }

        public string SoundAttachFileUrl
        {
            get { return _getValue<string>("SoundAttachFile"); }
        }

        public string SoundBreakFileUrl
        {
            get { return _getValue<string>("SoundBreakFile"); }
        }

        public string SoundDetachFileUrl
        {
            get { return _getValue<string>("SoundDetachFile"); }
        }

        public int StartDelay
        {
            get { return _getValue<int>("StartDelay"); }
        }

        public float StrutRealignDistanceTolerance
        {
            get { return (float) _getValue<double>("StrutRealignDistanceTolerance"); }
        }

        public int StrutRealignInterval
        {
            get { return _getValue<int>("StrutRealignInterval"); }
        }

        public float WeakJointStrength
        {
            get { return (float) _getValue<double>("WeakJointStrength"); }
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

        private static bool _configFileExists()
        {
            return File.Exists(ConfigFilePath);
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

        private class SettingsEntry
        {
            public object DefaultValue { get; private set; }
            public object Value { get; set; }

            public SettingsEntry(object defaultValue)
            {
                this.DefaultValue = defaultValue;
            }
        }
    }
}