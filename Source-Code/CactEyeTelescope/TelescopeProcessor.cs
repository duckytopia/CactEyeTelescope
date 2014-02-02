using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MuMech;

namespace CactEyeTelescope
{
    public class TelescopeProcessor : PartModule
    {
        public bool partActive = false;
        public bool partFunctional = false;
        public bool hasCharge = true;
        private int currentPartCount = 0;

        private double deltaTime = 0;

        [KSPField(isPersistant = false)]
        public string techType = "low";

        [KSPField(isPersistant = false)]
        public string mountName = "tele.mount";

        [KSPField(isPersistant = false)]
        public float mountDistance = 0.0f;

        [KSPField(isPersistant = false)]
        public string bayName = "tele.bay";

        [KSPField(isPersistant = false)]
        public float bayDistance = 0.0f;

        [KSPField(isPersistant = false)]
        public string opticsName = "tele.body";

        [KSPField(isPersistant = false)]
        public float opticsDistance = 3.4f;

        [KSPField(isPersistant = false)]
        public float consumeRate = 2.0f; //in e/s

        [KSPField(isPersistant = false, guiActive = true, guiName = "Status", guiActiveEditor = false)]
        public string status = "Off";

        ModuleAnimateGeneric opticsAnimate = null;
        double opticsState = 0;

        //Checks construction of telescope to make sure it's all good and stuff.
        ///*
        private bool checkSetup()
        {
           MuMechModuleHullCameraZoom moduleHC = part.GetComponent<MuMechModuleHullCameraZoom>();
            if(moduleHC == null)
            {
                status = "Install Hullcam VDS!";
                return false;
            }

            //print("running checkSetup");
            Part mount = part.parent;
            if (mount == null)
            {
                status = "Processor not mounted";
                moduleHC.Events["ActivateCamera"].active = false;
                moduleHC.Events["EnableCamera"].active = false;
                return false;
            }
            //If the processor is parented to any other part
            if (!mount.name.Contains(mountName))
            {
                status = "Processor not mounted";
                moduleHC.Events["ActivateCamera"].active = false;
                moduleHC.Events["EnableCamera"].active = false;
                return false;
            }
            //Is the mount the specified distance away? Within ~0.1
            else if (Math.Round(Vector3d.Distance(part.orgPos, mount.orgPos), 1) != mountDistance)
            {
                status = "Incorrectly mounted";
                moduleHC.Events["ActivateCamera"].active = false;
                moduleHC.Events["EnableCamera"].active = false;
                return false;
            }
            else
            {
                //print("mount check passed");
                //Now we'll look at the bay connected to the mount
                Part bay = mount.parent;
                //If it exists, prioritize the bay that exists as a child instead of a parent
                if (mount.children.Find(n => n.name.Contains(bayName)) != null)
                    bay = mount.children.Find(n => n.name.Contains(bayName));
                //If there is no childed bay and the mount is the root part
                else if (bay == null)
                {
                    status = "No service bay detected";
                    moduleHC.Events["ActivateCamera"].active = false;
                    moduleHC.Events["EnableCamera"].active = false;
                    return false;
                }
                //If it fails the other two checks and the parented object is not a bay
                else if (!bay.name.Contains(bayName))
                {
                    status = "No service bay detected";
                    moduleHC.Events["ActivateCamera"].active = false;
                    moduleHC.Events["EnableCamera"].active = false;
                    return false;
                }
                //Checks if bay is at the specified distance away
                if (Math.Round(Vector3d.Distance(bay.orgPos, mount.orgPos), 1) != bayDistance)
                {
                    status = "Incorrect bay config";
                    moduleHC.Events["ActivateCamera"].active = false;
                    moduleHC.Events["EnableCamera"].active = false;
                    return false;
                }
                else
                {
                    //print("bay check passed");
                    //Now we'll look at the optical tube
                    Part optics = bay.parent;
                    if (bay.children.Find(n => n.name.Contains(opticsName)) != null)
                        optics = bay.children.Find(n => n.name.Contains(opticsName));
                    else if (optics == null)
                    {
                        status = "No optics detected";
                        moduleHC.Events["ActivateCamera"].active = false;
                        moduleHC.Events["EnableCamera"].active = false;
                        return false;
                    }
                    else if (!optics.name.Contains(opticsName))
                    {
                        status = "No optics detected";
                        moduleHC.Events["ActivateCamera"].active = false;
                        moduleHC.Events["EnableCamera"].active = false;
                        return false;
                    }
                    if (Vector3d.Distance(bay.orgPos, optics.orgPos) < opticsDistance - 0.5 || Vector3d.Distance(bay.orgPos, optics.orgPos) > opticsDistance + 0.5)
                    {
                        status = "Optics out of line!";
                        moduleHC.Events["ActivateCamera"].active = false;
                        moduleHC.Events["EnableCamera"].active = false;
                        return false;
                    }
                    if (techType != "solar")
                    {
                        opticsAnimate = optics.GetComponent<ModuleAnimateGeneric>();
                        if (opticsAnimate.animTime < 0.5)
                        {
                            status = "Aperture closed!";
                            moduleHC.Events["ActivateCamera"].active = false;
                            moduleHC.Events["EnableCamera"].active = false;
                            return false;
                        }
                    }
                    status = "Functioning...";
                    moduleHC.Events["ActivateCamera"].active = true;
                    moduleHC.Events["EnableCamera"].active = false;
                    return true;
                }
            }
        }

        private void checkSelf() //recommended before wreckSelf()
        {
            //print("Checking self, part count " + currentPartCount);
            if (!checkSetup())
            {
                if (!partFunctional)
                    ScreenMessages.PostScreenMessage("Warning: " + status, 6, ScreenMessageStyle.UPPER_CENTER);
                partFunctional = false;
            }
            else if (hasCharge)
            {
                partFunctional = true;
            }
        }

        [KSPEvent(guiActive = true, guiName = "Activate", active = true)]
        public void activate()
        {
            partActive = true;
            Events["activate"].active = !partActive;
            Events["deactivate"].active = partActive;
            checkSelf();
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate", active = true)]
        public void deactivate()
        {
            partActive = false;
            Events["activate"].active = !partActive;
            Events["deactivate"].active = partActive;
            MuMechModuleHullCameraZoom moduleHC = part.GetComponent<MuMechModuleHullCameraZoom>();
            if (moduleHC != null)
            {
                moduleHC.Events["ActivateCamera"].active = false;
                moduleHC.Events["EnableCamera"].active = false;
            }
            status = "Off";
        }

        [KSPAction("Activate")]
        public void activate(KSPActionParam param)
        {
            activate();
        }

        [KSPAction("Deactivate")]
        public void deactivate(KSPActionParam param)
        {
            deactivate();
        }

        [KSPAction("Toggle")]
        public void toggle(KSPActionParam param)
        {
            if (partActive)
                deactivate();
            else
                activate();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (partActive && part.vessel.isActiveVessel)
            {
                if (part.vessel.Parts.Count != currentPartCount)
                    checkSelf();
                currentPartCount = part.vessel.Parts.Count;
            }

            if(opticsAnimate != null && techType != "solar")
            {
                if (Math.Round(opticsState) != Math.Round(opticsAnimate.animTime)) //recalculate functionality if optics are opened/closed
                {
                    opticsState = Math.Round(opticsAnimate.animTime);
                    if (partActive)
                        checkSelf();
                }
                if (opticsAnimate.animTime > 0 )
                {
                    double mainBodyAngleTo = Vector3d.Dot(part.transform.up, (FlightGlobals.getMainBody().position - FlightGlobals.ship_position).normalized);
                    double mainBodySize = Math.Atan2(FlightGlobals.getMainBody().Radius, Vector3d.Distance(FlightGlobals.getMainBody().position, FlightGlobals.ship_position));
                    mainBodySize *= (4 / Math.PI);
                    Vector3d heading = (FlightGlobals.Bodies[0].position - FlightGlobals.ship_position).normalized;
                    if (Vector3d.Dot(part.transform.up, heading) > 0.95 && mainBodyAngleTo < mainBodySize)
                    {
                        ScreenMessages.PostScreenMessage("Telescope pointed directly at sun, processor fried!", 6, ScreenMessageStyle.UPPER_CENTER);
                        part.explode(); //officially the best function ever
                    }
                }
            }

            if (partActive && deltaTime != 0)
            {
                double consumeAmount = (consumeRate * (Planetarium.GetUniversalTime() - deltaTime));
                if (part.RequestResource("ElectricCharge", consumeAmount) < consumeAmount * 0.95) //separated from other if statement because it actually eats up ElectricCharge when it's called
                {
                    if (partActive)
                    {
                        deactivate();
                        status = "Insufficient ElectricCharge";
                        ScreenMessages.PostScreenMessage("Processor Shutting Down (" + status + ")", 6, ScreenMessageStyle.UPPER_CENTER);
                    }
                }
            }

            deltaTime = Planetarium.GetUniversalTime();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (state == StartState.Editor)
                return;
            
            Events["activate"].active = !partActive;
            Events["deactivate"].active = partActive;
            //checkSetup() seems to freeze KAS for whatever reason when placed in OnStart, let's just slap the first check in a subroutine instead.
            StartCoroutine("InitialCheck");
        }

        private System.Collections.IEnumerator InitialCheck()
        {
            yield return new WaitForSeconds(0.1f);
            checkSetup();
            deactivate();
        }

        public override string GetInfo()
        {
            var sb = new StringBuilder();
            string techName = "";

            if (techType == "low")
                techName = "Low Tech";
            else if (techType == "mid")
                techName = "Mid Tech";
            else if (techType == "high")
                techName = "High Tech";
            else if (techType == "solar") //not implemented yet
                techName = "Solar";
            else
                techName = techType;

            sb.AppendLine("Tech Type: " + techName);

            MuMechModuleHullCameraZoom moduleHC = part.GetComponent<MuMechModuleHullCameraZoom>();

            if (moduleHC != null)
                sb.AppendLine("Magnification: " + string.Format("{0:n0}", (60 / moduleHC.cameraFoVMin)) + "x");
            else
                sb.AppendLine("ERROR: MISSING CAMERA MODULE");

            sb.AppendLine();

            sb.AppendLine("<color=#99ff00ff>Requires:</color>");
            sb.AppendLine("- ElectricCharge: " + string.Format("{0:0.0##}", consumeRate) + "/sec");

            sb.AppendLine();

            if (techType != "solar")
            {
                sb.AppendLine("<color=#ffa500ff>Do not point directly at sun while activated!</color>");
            }
            else
            {
                sb.AppendLine("<color=#ffa500ff>This part is broken! Solar processors may be added in future releases.</color>");
            }
            
            return sb.ToString();
        }
    }
}
