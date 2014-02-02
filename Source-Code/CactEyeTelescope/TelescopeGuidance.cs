//Using blizzy78's Toolbar API

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Toolbar;

namespace CactEyeTelescope
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ResetTelescopeGuidance : MonoBehaviour
    {
        public void Awake()
        {
            TelescopeGuidance.numInit = 0;
        }
    }
    public class TelescopeGuidance : PartModule
    {
        protected Rect windowPos = new Rect(Screen.width / 2, Screen.height / 2, 10f, 10f);

        Part bayPart = null;
        Part mountPart = null;
        Part procPart = null;
        TelescopeProcessor procModule = null;

        [KSPField(isPersistant = false)]
        public bool checkBuild = true;

        [KSPField(isPersistant = false)]
        public string requiredBay = "tele.bay";

        [KSPField(isPersistant = false)]
        public string requiredMount = "tele.mount";

        [KSPField(isPersistant = false, guiActive = false, guiName = "Deactivated", guiActiveEditor = false)]
        public string warningMessage = "Other Guidance Module Detected";

        private string status = "";

        private double targetDist = 0;
        private double targetAngleTo = 0;
        private double targetSize = 0;
        private double targetProximity = 0;

        private bool activated = false;

        private bool addDist = false;

        private IButton buttonGuidance;

        public static int numInit = 0;

        private void mainGUI(int windowID)
        {
            GUILayout.BeginVertical();

            if (checkBuild)
            {
                if (probe())
                {
                    status = getStatus();
                }
            }
            else
            {
                status = getStatus();
            }

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            GUILayout.Label("1.000 = Pointing directly at target");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            GUILayout.Label("" + status);
            GUILayout.EndHorizontal();

            if(addDist)
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                GUILayout.Label("Distance: " + string.Format("{0:0.0}", targetDist) + "m");
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        protected void drawGUI()
        {
            if (activated && HighLogic.LoadedSceneIsFlight)
                windowPos = GUILayout.Window(-5234628, windowPos, mainGUI, "Fine Guidance Sensor", GUILayout.Width(250), GUILayout.Height(50));
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            if (state == StartState.Editor) { return; }

            //print("tg numInit: " + numInit + " => " + (numInit + 1));

            numInit += 1;

            if (numInit == 1)
            {
                RenderingManager.AddToPostDrawQueue(3, new Callback(drawGUI));
                toolbarButton();
                Events["toggleEvent"].active = true;
            }
            else
            {
                Events["toggleEvent"].active = false;
                Fields["warningMessage"].guiActive = true;
            }
        }

        private string getStatus()
        {
            var target = FlightGlobals.fetch.VesselTarget;
            if (target == null)
            {
                addDist = false;

                return "You must select a target!";
            }
            if (target.GetType().Name == "CelestialBody")
            {
                addDist = false;

                CelestialBody targetBody = FlightGlobals.Bodies.Find(index => index.GetName() == target.GetName());
                Vector3d targetLocation = targetBody.position;
                targetDist = Vector3d.Distance(targetLocation, FlightGlobals.ship_position);
                Vector3d targetAngle = FlightGlobals.fetch.vesselTargetDirection;
                Vector3d vesselAngle = part.transform.up;
                if (checkBuild)
                    vesselAngle = procPart.transform.up;
                targetAngleTo = Vector3d.Angle(targetAngle, vesselAngle); //relative angle between where processor is pointing and angle to target - 0 degrees ideal
                targetSize = Math.Atan2(targetBody.Radius, targetDist); //angular size of planet
                targetSize *= (180 / Math.PI);
                targetProximity = Math.Sqrt(Math.Max(0, targetAngleTo - targetSize) / 5);
                if (targetProximity > 1)
                    return "Point within 5° of target to begin proximity reading!";
                else
                    return "Proximity: " + string.Format("{0:0.000}", 1 - targetProximity);
            }
            else if (target.GetType().Name == "Vessel")
            {
                addDist = true;

                Vessel targetShip = FlightGlobals.Vessels.Find(index => index.GetName() == target.GetName());
                targetDist = (Vector3d.Distance(FlightGlobals.ship_position, targetShip.GetWorldPos3D()));
                Vector3d targetAngle = FlightGlobals.fetch.vesselTargetDirection;
                Vector3d vesselAngle = part.transform.up;
                if (checkBuild)
                    vesselAngle = procPart.transform.up;
                targetAngleTo = Vector3d.Angle(targetAngle, vesselAngle);
                targetProximity = Math.Sqrt(Math.Max(0, targetAngleTo) / 5);
                if (targetProximity > 1)
                    return "Point within 5° of target to begin proximity reading!";
                else
                    return "Proximity: " + string.Format("{0:0.000}", 1 - targetProximity);
            }
            else
            {
                addDist = false;
                return "You must target a celestial body or vessel!"; //can you even target anything else? whatever
            }
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Display", active = true)]
        public void toggleEvent()
        {
            activated = !activated;
            if(activated)
                buttonGuidance.TexturePath = "CactEye/Icons/toolbar";
            else
                buttonGuidance.TexturePath = "CactEye/Icons/toolbar_disabled";
        }

        [KSPAction("Toggle Display")]
        public void toggleAction(KSPActionParam param)
        {
            toggleEvent();
        }

        
        private void toolbarButton()
        {
            buttonGuidance = ToolbarManager.Instance.add("test", "buttonGuidance");
            buttonGuidance.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);
            buttonGuidance.TexturePath = "CactEye/Icons/toolbar_disabled";
            buttonGuidance.ToolTip = "CactEye Fine Guidance Sensor";
            buttonGuidance.OnClick += (e) => toggleEvent();
        }
        
        private void OnDestroy()
        {
            buttonGuidance.Destroy();
        }
        

        private bool probe()
        {
            bayPart = part.parent;
            if (!bayPart.name.Contains(requiredBay))
            {
                status = "Guidance sensor needs to be placed on bay!";
                return false;
            }

            mountPart = bayPart.parent;
            if (bayPart.FindChildPart(requiredMount) != null)
                mountPart = bayPart.FindChildPart(requiredMount);
            else if (mountPart == null)
            {
                status = "Could not find processor!";
                return false;
            }
            else if (!mountPart.name.Contains(requiredMount))
            {
                status = "Could not find processor!";
                return false;
            }

            procPart = mountPart.children.Last();

            if (procPart == null)
            {
                status = "Could not find processor!";
                return false;
            }

            procModule = procPart.GetComponent<TelescopeProcessor>();

            if (procModule == null)
            {
                status = "Could not find processor!";
                return false;
            }

            if (!procModule.partFunctional)
            {
                status = "Processor not functional!";
                return false;
            }

            if (!procModule.partActive)
            {
                status = "Processor not activated!";
                return false;
            }

            return true;
        }
    }
}
