using KSP.IO;

namespace ActiveStruts
{
    public class Config
    {
        public static string ModuleName = "ModuleActiveStrut";
        public static float MaxDistance = 15;
        public static float MaxAngle = 95;
        public static float WeakJointStrength = 50;
        public static float NormalJointStrength = 2000;
        public static float MaximalJointStrength = 40000;
        public static string LinkHelpText = "Click left on a possible target to establish a link. Press 'x' to abort. You can also right click -> 'Set as Target' on a valid target and right click -> 'Abort' on the targeter.";
        public static string FreeAttachHelpText = "Click left on a valid position to establish a link. Click right to abort.";
        public static float ConnectorDimension = 0.5f;
        public static float ColorTransparency = 0.5f;
        public static float FreeAttachDistanceTolerance = 0.1f;
        public static float FreeAttachStrutExtension = 0.05f;
        public static int StartDelay = 60;
        public static int StrutRealignInterval = 10;

        public static void Load()
        {
            var cfg = PluginConfiguration.CreateForType<ModuleActiveStrut>();
            cfg.load();
            ModuleName = cfg.GetValue<string>("ModuleName");
            MaxDistance = cfg.GetValue<float>("MaxDistance");
            MaxAngle = cfg.GetValue<float>("MaxAngle");
            WeakJointStrength = cfg.GetValue<float>("WeakJointStrength");
            NormalJointStrength = cfg.GetValue<float>("NormalJointStrength");
            MaximalJointStrength = cfg.GetValue<float>("MaximalJointStrength");
            LinkHelpText = cfg.GetValue<string>("LinkHelpText");
            FreeAttachHelpText = cfg.GetValue<string>("FreeAttachHelpText");
            ConnectorDimension = cfg.GetValue<float>("ConectorDimension");
            ColorTransparency = cfg.GetValue<float>("ColorTransparency");
            FreeAttachDistanceTolerance = cfg.GetValue<float>("FreeAttachDistanceTolerance");
            FreeAttachStrutExtension = cfg.GetValue<float>("FreeAttachStrutExtension");
            StartDelay = cfg.GetValue<int>("StartDelay");
            StrutRealignInterval = cfg.GetValue<int>("StrutRealignInterval");
        }

        public static void Save()
        {
            var cfg = PluginConfiguration.CreateForType<ModuleActiveStrut>();
            cfg.SetValue("ModuleName", ModuleName);
            cfg.SetValue("MaxDistance", MaxDistance);
            cfg.SetValue("MaxAngle", MaxAngle);
            cfg.SetValue("WeakJointStrength", WeakJointStrength);
            cfg.SetValue("NormalJointStrength", NormalJointStrength);
            cfg.SetValue("MaximalJointStrength", MaximalJointStrength);
            cfg.SetValue("LinkHelptext", LinkHelpText);
            cfg.SetValue("FreeAttachHelpText", FreeAttachHelpText);
            cfg.SetValue("ConnectorDimension", ConnectorDimension);
            cfg.SetValue("ColorTransparency", ColorTransparency);
            cfg.SetValue("FreeAttachDistanceTolerance", FreeAttachDistanceTolerance);
            cfg.SetValue("FreeAttachStrutExtension", FreeAttachStrutExtension);
            cfg.SetValue("StartDelay", StartDelay);
            cfg.SetValue("StrutRealignInterval", StrutRealignInterval);
            cfg.save();
        }
    }
}