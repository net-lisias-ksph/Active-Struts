namespace ActiveStruts
{
    public class Config
    {
        public const string ModuleName = "ModuleActiveStrut";
        public const float MaxDistance = 15;
        public const float MaxAngle = 95;
        public const float WeakJointStrength = 50;
        public const float NormalJointStrength = 2000;
        public const float MaximalJointStrength = 40000;
        public const string LinkHelpText = "Click left on a possible target to establish a link. Press 'x' to abort. You can also right click -> 'Set as Target' on a valid target and right click -> 'Abort' on the targeter.";
        public const string FreeAttachHelpText = "Click left on a valid position to establish a link. Click right to abort.";
        public const float ConnectorDimension = 0.025f;
        public const float ColorTransparency = 0.5f;
    }
}