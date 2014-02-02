using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CactEyeTelescope
{
    class TelescopeExperiment : ModuleScienceExperiment
    {
        [KSPField(isPersistant = false)]
        public string techType = "low";

        [KSPField(isPersistant = false)]
        public string requiredBay = "tele.bay";

        [KSPField(isPersistant = false)]
        public string requiredMount = "tele.mount";

        [KSPField(isPersistant = false)]
        public int experimentType = 0; //0 = default, 1 = planetary cam, 2 = transit cam, 3 = solar cam

        [KSPField(isPersistant = false)]
        public bool checkBuild = true; //set false to not use the bay/mount/optics structure and just use this part as the camera, up being where its pointed

        [KSPField(isPersistant = false)]
        public string specialExperimentName = ""; // defined when experimentType is 1 or 2, allows for experimentID to be changed based on what planet is being observed

        [KSPField(isPersistant = false)]
        public string specialExperimentTitle = ""; // when doing advanced experiments, uses this title for experiment readouts. Replaces #BODY# with targetted celestial body.

        [KSPField(isPersistant = false)]
        public int valueScale = 16; //Base value to calculate science on. Ignores baseValue defined in the actual experiment definition cfg.

        [KSPField(isPersistant = false)]
        public float dataScalar = 0.5f; //basically dataScale, but set per part

        //[KSPField(isPersistant = false, guiActive = true, guiName = "Status")] --- (if I want to display this in-game for debugging)
        private string status = "";

        Part bayPart = null;
        Part mountPart = null;
        Part procPart = null;
        TelescopeProcessor procModule = null;

        //Experiment vars needed
        double targetAngleTo = 0;
        double targetSize = 0;
        double orbitalAngle = 0;
        //double sunSize = 0; //solar telescope stuff, largely unimplemented

        private bool readyToGo = true;

        List<string> supportedPlanets = new List<string>(new string[] { "Sun", "Kerbin", "Mun", "Minmus", "Moho", "Eve", "Duna", "Ike", "Jool", "Laythe", "Vall", "Bop", "Tylo", "Gilly", "Pol", "Dres", "Eeloo" });

        //Science gain scales for planets. Corresponds to FlightGlobals.Bodies[index]
        float[] getScienceScale = new float[] { 0, 1, 1, 1, 3, 3, 3, 2, 3, 7, 5, 7, 4, 5, 7, 5, 7 };

        private bool probe()
        {
            bayPart = part.parent;
            if(!bayPart.name.Contains(requiredBay))
            {
                status = "Experiment needs to be placed on bay!";
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

            if (procModule.techType != techType)
            {
                status = "Incorrect processor! Tech type is " + procModule.techType + ", requires " + techType;
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

            status = "";
            return true;
        }

        new public void DeployExperiment()
        {
            if (readyToGo)
            {
                //print("not ready to go");
                readyToGo = false;
                Events["DeployExperiment"].active = false;
                StartCoroutine("EndCooldown"); //Deploys the experiment twice for some reason, starts a short 'cooldown' period so it doesn't do that.
                if (part.vessel.altitude < FlightGlobals.getMainBody().scienceValues.spaceAltitudeThreshold)
                {
                    status = "Telescope only operational in high orbit (" + (FlightGlobals.getMainBody().scienceValues.spaceAltitudeThreshold / 1000) + "km for current planet)";
                    ScreenMessages.PostScreenMessage(status, 6, ScreenMessageStyle.UPPER_CENTER);
                    base.ResetExperiment();
                    return;
                }
                if (experimentType != 0)
                {
                    if (checkBuild && !probe())
                    {
                        ScreenMessages.PostScreenMessage(status, 6, ScreenMessageStyle.UPPER_CENTER);
                        base.ResetExperiment();
                        return;
                    }
                    else if (!setTargetParameters())
                    {
                        ScreenMessages.PostScreenMessage(status, 6, ScreenMessageStyle.UPPER_CENTER);
                        base.ResetExperiment();
                        return;
                    }
                    else
                    {
                        //print("Running");
                        advExperiment(); //runs advanced experiment if telescope is built correctly and a target is selected
                    }
                }
                else if (checkBuild)
                {
                    if (!probe())
                    {
                        ScreenMessages.PostScreenMessage(status, 6, ScreenMessageStyle.UPPER_CENTER);
                        base.ResetExperiment();
                        return;
                    }
                    else
                        base.DeployExperiment(); //runs basic experiment only if telescope is built correctly
                }
                else
                    base.DeployExperiment(); //if you use experimentType 0 and checkBuild = false, it's really just a standard experiment
            }
        }

        [KSPAction("DeployAction")]
        new public void DeployAction(KSPActionParam param)
        {
            if(readyToGo)
                DeployExperiment();
        }

        public void advExperiment()
        {
            //print("Running adv. experiment");
            if (experimentType == 1)
            {
                if(targetAngleTo >= 5)
                {
                    status = "Target not in view!";
                    ScreenMessages.PostScreenMessage(status, 6, ScreenMessageStyle.UPPER_CENTER);
                    base.ResetExperiment();
                    return;
                }
                if (supportedPlanets.Contains(FlightGlobals.fetch.VesselTarget.GetName()))
                {
                    base.experiment.id = specialExperimentName + FlightGlobals.fetch.VesselTarget.GetName();
                    base.experiment.experimentTitle = specialExperimentTitle.Replace("#BODY#", FlightGlobals.fetch.VesselTarget.GetName());
                }
                else //this is (very) basic support for planet mods like Alternis Kerbal
                {
                    base.experiment.id = specialExperimentName + "Base";
                    base.experiment.experimentTitle = specialExperimentTitle.Replace("#BODY#", FlightGlobals.fetch.VesselTarget.GetName());
                }
                double detractor = 1;
                detractor *= 1 - Math.Sqrt(Math.Max(0, targetAngleTo - targetSize) / 5); //how centered the planet is, where directly intersecting target is 0
                detractor *= (1 - (.25 * (orbitalAngle / 180))); //how optimal the phase angle is
                base.experiment.baseValue = getScienceScale[FlightGlobals.Bodies.FindIndex(index => index.GetName() == FlightGlobals.fetch.VesselTarget.GetName())] * valueScale * (float)detractor;
                base.experiment.scienceCap = getScienceScale[FlightGlobals.Bodies.FindIndex(index => index.GetName() == FlightGlobals.fetch.VesselTarget.GetName())] * valueScale;
                base.experiment.dataScale = dataScalar * (valueScale / base.experiment.baseValue);
                //print("Deploying adv. experiment: " + base.experiment.id + ", baseValue: " + base.experiment.baseValue);
                base.DeployExperiment();
            }
            //Future solar telescope crap will go here
        }

        public bool setTargetParameters() //Only run after probe() returns true or checkBuild = false
        {
            var target = FlightGlobals.fetch.VesselTarget;
            if (target == null)
            {
                status = "You must select a target!";
                return false;
            }
            if (target.GetType().Name == "CelestialBody")
            {
                //double meanDist = 0;
                //double targetInclination = 0;
                //double sunAngleTo = 0;
                CelestialBody targetBody = FlightGlobals.Bodies.Find(index => index.GetName() == target.GetName());
                Vector3d targetLocation = targetBody.position;
                double targetDist = Vector3d.Distance(targetLocation, FlightGlobals.ship_position);

                //meanDist = Math.Abs(targetBody.orbit.semiMajorAxis - FlightGlobals.Bodies[1].orbit.semiMajorAxis); //average distance of target from Kerbin

                Vector3d targetAngle = FlightGlobals.fetch.vesselTargetDirection;
                Vector3d vesselAngle = part.transform.up;
                if(checkBuild)
                    vesselAngle = procPart.transform.up; //use the processor direction instead of this part if checkBuild

                targetAngleTo = Vector3d.Angle(targetAngle, vesselAngle); //relative angle between where processor is pointing and angle to target - 0 degrees ideal
                targetSize = Math.Atan2(targetBody.Radius, targetDist); //angular size of planet
                targetSize *= (180 / Math.PI);

                //targetInclination = targetBody.orbit.inclination; //inclination ended up being unused
                //if (targetInclination > 90)
                    //targetInclination = Math.Abs(targetInclination - 180); //inclination should be between 0 and 90, in case a planet is retrograde for some reason? idk

                CelestialBody sun = FlightGlobals.Bodies[0];
                if (targetBody.referenceBody == FlightGlobals.getMainBody() || FlightGlobals.getMainBody().HasParent(targetBody.referenceBody))
                    sun = targetBody.referenceBody; // If the target is a moon around the telescope's host planet, use that as the 'sun' for calculations, also if scope is orbiting one moon while looking at another
                double sunTargetDist = Vector3d.Distance(targetLocation, sun.position);
                double sunDist = Vector3d.Distance(FlightGlobals.ship_position, sun.position);

                //---  arccos(c^2 - a^2 - b^2) / (-2*a*b) = angleC    [law of cosines]
                orbitalAngle = Math.Acos(((targetDist * targetDist) - (sunTargetDist * sunTargetDist) - (sunDist * sunDist)) / (-2 * sunTargetDist * sunDist)); //phase angle from scope to target
                orbitalAngle *= (180 / Math.PI);

                //sunSize = Math.Atan2(sun.Radius, sunDist);
                //sunSize *= (180 / Math.PI);
                //sunAngleTo = Vector3d.Dot(vesselAngle, (FlightGlobals.Bodies[0].position - FlightGlobals.ship_position).normalized);

                double mainBodyAngleTo = Vector3d.Dot(targetAngle, (FlightGlobals.getMainBody().position - FlightGlobals.ship_position).normalized);
                double mainBodySize = Math.Atan2(FlightGlobals.getMainBody().Radius, Vector3d.Distance(FlightGlobals.getMainBody().position, FlightGlobals.ship_position));
                mainBodySize *= (4 / Math.PI);

                //print("mb: " + mainBodyAngleTo + " / " + mainBodySize);

                if(mainBodyAngleTo > mainBodySize)
                {
                    status = "Target covered by " + FlightGlobals.getMainBody().GetName() + "!";
                    return false;
                }
                return true;
            }
            else
            {
                status = "Target not a celestial body!";
                return false;
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (Events["DeployExperiment"].active != readyToGo)
                Events["DeployExperiment"].active = readyToGo;
        }
        private System.Collections.IEnumerator EndCooldown()
        {
            yield return new WaitForSeconds(1f);
            //print("Ready to go");
            readyToGo = true;
            Events["DeployExperiment"].active = true;
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

            if (experimentType == 0 || experimentType == 3)
                sb.AppendLine("Experiment Type: Basic");
            else if (experimentType == 1)
                sb.AppendLine("Experiment Type: Planetary");
            else if (experimentType == 2)
                sb.AppendLine("Experiment Type: Transit"); //obviously not implemented yet

            sb.AppendLine();

            sb.AppendLine("<color=#ffa500ff>Requires high orbit to function!</color>");

            return sb.ToString();
        }
    }
}
