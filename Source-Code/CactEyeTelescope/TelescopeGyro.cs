using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CactEyeTelescope
{
    class TelescopeGyro : ModuleReactionWheel
    {
        [KSPField(isPersistant = false)]
        public float gyroScale = 0.1f;

        [KSPField(isPersistant = false)]
        public float gyroFineScale = 0.001f;

        [KSPField(isPersistant = false)]
        public float guiRate = 0.3f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Mode", guiActiveEditor = false)]
        public string torqueMode = "Normal";

        float pitchScale = 2.5f;
        float yawScale = 2.5f;
        float rollScale = 2.5f;

        [KSPEvent(guiActive = true, guiName = "Switch Mode", active = true)]
        public void cycleTorque()
        {
            if(torqueMode == "Normal")
            {
                redScale(null);
                return;
            }
            if(torqueMode == "Reduced")
            {
                fineScale(null);
                return;
            }
            if(torqueMode == "Fine")
            {
                normScale(null);
                return;
            }
        }

        [KSPAction("Normal Torque")]
        public void normScale(KSPActionParam param) //removed action param
        {
            torqueMode = "Normal";

            base.PitchTorque = pitchScale;
            base.YawTorque = yawScale;
            base.RollTorque = rollScale;

            ScreenMessages.PostScreenMessage("Torque Mode: " + torqueMode, 4, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPAction("Reduced Torque")]
        public void redScale(KSPActionParam param)
        {
            torqueMode = "Reduced";

            base.PitchTorque = pitchScale * gyroScale;
            base.YawTorque = yawScale * gyroScale;
            base.RollTorque = rollScale * gyroScale;

            ScreenMessages.PostScreenMessage("Torque Mode: " + torqueMode, 4, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPAction("Fine Torque")]
        public void fineScale(KSPActionParam param)
        {
            torqueMode = "Fine";

            base.PitchTorque = pitchScale * gyroFineScale;
            base.YawTorque = yawScale * gyroFineScale;
            base.RollTorque = rollScale * gyroFineScale;

            ScreenMessages.PostScreenMessage("Torque Mode: " + torqueMode, 4, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPAction("Switch Torque Mode")]
        public void switchMode(KSPActionParam param)
        {
            cycleTorque();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            pitchScale = base.PitchTorque;
            yawScale = base.YawTorque;
            rollScale = base.RollTorque;
        }

        public override string GetInfo()
        {
            var sb = new StringBuilder();

            if (pitchScale == yawScale && pitchScale == rollScale)
            {
                sb.AppendLine("Normal Torque: " + string.Format("{0:0.0##}", pitchScale));
                sb.AppendLine("Reduced Torque: " + string.Format("{0:0.0##}", pitchScale * gyroScale));
                sb.AppendLine("Fine Torque: " + string.Format("{0:0.0###}", pitchScale * gyroFineScale));
            }
            else
            {
                sb.AppendLine("Pitch Torque: " + string.Format("{0:0.0##}", pitchScale));
                sb.AppendLine("Yaw Torque: " + string.Format("{0:0.0##}", yawScale));
                sb.AppendLine("Roll Torque: " + string.Format("{0:0.0##}", rollScale));
                sb.AppendLine("Reduced Scale: " + string.Format("{0:0.0##}", gyroScale));
                sb.AppendLine("Fine Scale: " + string.Format("{0:0.0###}", gyroFineScale));
            }

            if (guiRate != -1)
            {
                sb.AppendLine();
                sb.AppendLine("<color=#99ff00ff>Requires:</color>");
                sb.AppendLine("- ElectricCharge: " + ((guiRate < 1) ? (string.Format("{0:0.0##}", guiRate * 60) + "/min.") : (string.Format("{0:0.0##}", guiRate) + "/sec.")));
            }

            return sb.ToString();
        }
    }
}
