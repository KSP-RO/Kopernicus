/**
 * Kopernicus Planetary System Modifier
 * Copyright (C) 2014 Bryce C Schroeder (bryce.schroeder@gmail.com), Nathaniel R. Lewis (linux.robotdude@gmail.com)
 * 
 * http://www.ferazelhosting.net/~bryce/contact.html
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
 * MA 02110-1301  USA
 * 
 * This library is intended to be used as a plugin for Kerbal Space Program
 * which is copyright 2011-2014 Squad. Your usage of Kerbal Space Program
 * itself is governed by the terms of its EULA, not the license above.
 * 
 * https://kerbalspaceprogram.com
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;

namespace Kopernicus 
{
	// Hook the PSystemSpawn (creation of the planetary system) event in the KSP initialization lifecycle
	[KSPAddon(KSPAddon.Startup.PSystemSpawn, false)]
	public class Injector : MonoBehaviour 
	{
        public Templates templates = null;
        
		/**
		 * Awake() is the first function called in the lifecycle of a Unity3D MonoBehaviour.  In the case of KSP,
		 * it happens to be called right before the game's PSystem is instantiated from PSystemManager.Instance.systemPrefab
		 *
		 * TL,DR - Custom planet injection happens here
		 * 
		 **/
		public void Awake()
		{
			// We're ALIVE
			Logger.Default.SetAsActive ();
			Logger.Default.Log("Injector.Awake(): Begin");

			// Yo garbage collector - we have work to do man
			DontDestroyOnLoad(this);

			// If the planetary manager does not work, well, error out
			if (PSystemManager.Instance == null) 
			{
				// Log the error
				Logger.Default.Log("Injector.Awake(): If PSystemManager.Instance is null, there is nothing to do");
				return;
			}

			// Get the current time
			DateTime start = DateTime.Now;
            
            // Grab templates
            templates = new Templates();


			// THIS IS WHERE THE MAGIC HAPPENS - OVERWRITE THE SYSTEM PREFAB SO KSP ACCEPTS OUR CUSTOM SOLAR SYSTEM AS IF IT WERE FROM SQUAD
			PSystemManager.Instance.systemPrefab = (new Configuration.Loader()).Generate();

			// SEARCH FOR THE ARCHIVES CONTROLLER PREFAB AND OVERWRITE IT WITH THE CUSTOM SYSTEM
			RDArchivesController archivesController = AssetBase.RnDTechTree.GetRDScreenPrefab ().GetComponentsInChildren<RDArchivesController> (true).First ();
			archivesController.systemPrefab = PSystemManager.Instance.systemPrefab;

            // CBT check
            Logger.Default.Log("++++++++++++++ CBT check");
            Utility.CBTCheck(PSystemManager.Instance.systemPrefab.rootBody);

            // reparent space center instance
            Logger.Default.Log("Space Center: is it null? " + (SpaceCenter.Instance == null).ToString());
            if (SpaceCenter.Instance != null)
            {
                SpaceCenter sc = SpaceCenter.Instance;
                PQS homePQS = null;
                bool flag = false;
                if (FlightGlobals.Bodies != null)
                    if (FlightGlobals.Bodies.Count > 0)
                        flag = true;
                if (flag)
                {
                    Logger.Default.Log("Getting from FG");
                    foreach (CelestialBody b in FlightGlobals.Bodies)
                        if (b.bodyName == Templates.instance.homeName)
                        {
                            homePQS = b.pqsController;
                            break;
                        }
                }
                else
                {
                    PSystemBody homeBody = Utility.FindBody(PSystemManager.Instance.systemPrefab.rootBody, Templates.instance.homeName);
                    homePQS = homeBody.pqsVersion;
                }
                if (homePQS != null)
                {
                    Logger.Default.Log("Reparented Space Center to new home pqs");
                    sc.transform.parent.SetParent(homePQS.transform);
                }
            }

            // Set home stuff
            // done on Post-Spawn now - ApplyHome(Templates.instance.homeNode, Templates.instance.homePQS, Templates.instance.homeBody);
            string home = Templates.instance.homeName;
            PSystemSetup.Instance.pqsToActivate = home;
            PSystemSetup.SpaceCenterFacility[] spaceCenterFacilityArray = PSystemSetup.Instance.GetSpaceCenterFacilities();
            foreach (PSystemSetup.SpaceCenterFacility scf in spaceCenterFacilityArray)
            {
                scf.pqsName = home;
            }

			// Add a handler so that we can do post spawn fixups.  
			PSystemManager.Instance.OnPSystemReady.Add(PostSpawnFixups);

			// Done executing the awake function
			TimeSpan duration = (DateTime.Now - start);
			Logger.Default.Log("Injector.Awake(): Completed in: " + duration.TotalMilliseconds + " ms");
			Logger.Default.Flush ();
		}

		// Post spawn fixups (ewwwww........)
		public void PostSpawnFixups ()
		{
            Debug.Log("[Kopernicus]: Post-Spawn");
			// Fix the flight globals index of each body
			int counter = 0;
			foreach (CelestialBody body in FlightGlobals.Bodies) 
			{
				body.flightGlobalsIndex = counter++;
				Logger.Active.Log ("Found Body: " + body.bodyName + ":" + body.flightGlobalsIndex + " -> SOI = " + body.sphereOfInfluence + ", Hill Sphere = " + body.hillSphere);
			}

			// Fix the maximum viewing distance of the map view camera (get the farthest away something can be from the root object)
			PSystemBody rootBody = PSystemManager.Instance.systemPrefab.rootBody;
            double maximumDistance = 1000d; // rootBody.children.Max(b => (b.orbitDriver != null) ? b.orbitDriver.orbit.semiMajorAxis * (1 + b.orbitDriver.orbit.eccentricity) : 0);
            if (rootBody != null)
            {
                maximumDistance = rootBody.celestialBody.Radius * 100d;
                if(rootBody.children != null && rootBody.children.Count > 0)
                {
                    foreach (PSystemBody body in rootBody.children)
                    {
                        if (body.orbitDriver != null)
                            maximumDistance = Math.Max(maximumDistance, body.orbitDriver.orbit.semiMajorAxis * (1d + body.orbitDriver.orbit.eccentricity));
                        else
                            Debug.Log("[Kopernicus]: Body " + body.name + " has no orbitdriver!");
                    }
                }
                else
                    Debug.Log("[Kopernicus]: Root body children null or 0");
            }
            else
                Debug.Log("[Kopernicus]: Root body null!");
            Debug.Log("Found max distance " + maximumDistance);
			PlanetariumCamera.fetch.maxDistance = ((float)maximumDistance * 3.0f) / ScaledSpace.Instance.scaleFactor;

			// Select the closest star to home
			StarLightSwitcher.HomeStar ().SetAsActive ();

            // Space center fix
            SpaceCenterFixer.pqsName = Templates.instance.homeName;
            if (SpaceCenter.Instance != null)
            {
                PQS homeP = null;
                Logger.Default.Log("** Space Center Post-Spawn rehome, finding " + Templates.instance.homeName);
                if (FlightGlobals.Bodies != null)
                {
                    Logger.Default.Log("Checking FlightGlobals.Bodies");
                    foreach (CelestialBody b in FlightGlobals.Bodies)
                    {
                        Logger.Default.Log("Checking body " + b.bodyName);
                        if (b.bodyName == Templates.instance.homeName)
                        {
                            Logger.Default.Log("Found.");
                            homeP = b.pqsController;
                            break;
                        }
                    }
                }
                if (homeP == null)
                {
                    Logger.Default.Log("No home PQS found. FlightGlobals.Bodies null or celestial body was renamed.");
                    PQS[] allPQS = Resources.FindObjectsOfTypeAll<PQS>();
                    foreach (PQS p in allPQS)
                    {
                        if (p.name == Templates.instance.homeName)
                        {
                            homeP = p;
                            break;
                        }
                    }
                }
                if(homeP != null)
                {
                    Debug.Log("home PQS found, finding KSC");
                    //DumpUpwards(homeP.transform, "");
                    //ApplyHome(Templates.instance.homeNode, homeP, homeB);
                    //PQSCity[] kscs = homeP.GetComponentsInChildren(typeof(PQSCity), true) as PQSCity[];
                    PQSCity[] kscs = Resources.FindObjectsOfTypeAll<PQSCity>();
                    PQSCity ksc = null;
                    foreach (PQSCity m in kscs)
                    {
                        if (m.name == "KSC")
                        {
                            if (m.sphere = homeP)
                            {
                                ksc = m;
                                break;
                            }
                            else
                            {
                                Logger.Default.Log("Found KSC not on this PQS.");
                                Utility.DumpUpwards(m.transform, "*");
                            }
                            
                        }
                    }
                    Debug.Log("Testing KSC");
                    if (ksc != null)
                    {
                        Debug.Log("KSC ok, reparenting.");
                        try
                        {
                            SpaceCenter.Instance.transform.SetParent(ksc.transform);
                            Debug.Log("Reparented. Starting.");
                            typeof(SpaceCenter).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(SpaceCenter.Instance, null);
                        }
                        catch (Exception e)
                        {
                            Debug.Log("[Kopernicus]: Failed to start space center, exception " + e);
                            Logger.Default.Log("SC FAIL ++++++++++++++++++++++++");
                            Utility.DumpUpwards(SpaceCenter.Instance.transform, "*");
                        }
                    }
                    else
                        Logger.Default.Log("Could not find KSC");
                }
                else
                    Logger.Default.Log("Still could not find home PQS. Spacecenter not reparented.");
                
            }
            else
            {
                Debug.Log("[Kopernicus]: Post-Spawn, no spacecenter");
                Logger.Default.Log("*** NO SPACE CENTER ****");
            }

			// Fixups complete, time to surrender to fate
			Destroy (this);
		}

		// Log the destruction of the injector
		public void OnDestroy()
		{
			Logger.Default.Log("Injector.OnDestroy(): Complete");
			Logger.Default.Flush ();
		}

        public static void ApplyHome(ConfigNode node, PQS pqs, CelestialBody body)
        {
            if (node == null)
            {
                Logger.Default.Log("ApplyHome: node null!");
                return;
            }
            if (pqs == null)
            {
                Logger.Default.Log("ApplyHome: pqs null!");
                return;
            }
            if (body == null)
            {
                Logger.Default.Log("ApplyHome: body null!");
                return;
            }
            if (Templates.instance == null)
            {
                Logger.Default.Log("ApplyHome: Templates null!");
                return;
            }

            Debug.Log("**AH No nulls");
            Logger.Default.Log("Applying home to body " + body.name);
            PQSCity ksc = Templates.instance.ksc;
            PQSCity[] others = Resources.FindObjectsOfTypeAll<PQSCity>().ToArray();
            foreach (PQSCity m in others)
            {
                if (m.name == "KSC")
                {
                    if (m == ksc)
                    {
                        Logger.Default.Log("***Found home in all PQSCities");
                        Utility.DumpUpwards(m.transform, "-");
                    }
                    else
                    {
                        Logger.Default.Log("***Found another KSC!");
                        Utility.DumpUpwards(m.transform, "-");
                    }
                }
            }
            if (ksc != null)
            {
                ksc.sphere = pqs;
                ksc.transform.SetParent(pqs.transform);
                Logger.Default.Log("**** AH setting KSC parent. Tree now:");
                Utility.DumpUpwards(ksc.transform, "*");
                if (node.HasNode("KSC"))
                {
                    if (node.HasValue("order"))
                        int.TryParse(node.GetValue("order"), out ksc.order);
                    if (node.HasValue("reorientFinalAngle"))
                        float.TryParse(node.GetValue("reorientFinalAngle"), out ksc.reorientFinalAngle);
                    if (node.HasValue("reorientInitialUp"))
                        ksc.reorientInitialUp = KSPUtil.ParseVector3(node.GetValue("reorientInitialUp"));
                    if (node.HasValue("reorientToSphere"))
                        bool.TryParse(node.GetValue("reorientToSphere"), out ksc.reorientToSphere);
                    if (node.HasValue("lodvisibleRangeMult"))
                    {
                        double dtmp;
                        if (double.TryParse(node.GetValue("lodvisibleRangeMult"), out dtmp))
                        {
                            foreach (PQSCity.LODRange l in ksc.lod)
                            {
                                l.visibleRange = (float)(l.visibleRange * dtmp);
                            }
                        }
                    }

                    if (node.HasValue("latitude") && node.HasValue("longitude"))
                    {
                        double lat, lon;
                        double.TryParse(node.GetValue("latitude"), out lat);
                        double.TryParse(node.GetValue("longitude"), out lon);

                        ksc.repositionRadial = Utility.LLAtoECEF(lat, lon, 0, body.Radius);
                    }
                    else if (node.HasValue("repositionRadial"))
                        ksc.repositionRadial = KSPUtil.ParseVector3(node.GetValue("repositionRadial"));

                    if (node.HasValue("repositionRadiusOffset"))
                        double.TryParse(node.GetValue("repositionRadiusOffset"), out ksc.repositionRadiusOffset);
                    if (node.HasValue("repositionToSphere"))
                        bool.TryParse(node.GetValue("repositionToSphere"), out ksc.repositionToSphere);
                    if (node.HasValue("repositionToSphereSurface"))
                        bool.TryParse(node.GetValue("repositionToSphereSurface"), out ksc.repositionToSphereSurface);
                    if (node.HasValue("repositionToSphereSurfaceAddHeight"))
                        bool.TryParse(node.GetValue("repositionToSphereSurfaceAddHeight"), out ksc.repositionToSphereSurfaceAddHeight);
                    ksc.Orientate();
                }
            }
            else
                Logger.Default.Log("No KSC!");

            Logger.Default.Log("**AH Space center time");
            if (SpaceCenter.Instance != null)
            {
                Logger.Default.Log("[Kopernicus]: Setting spacecenter");
                SpaceCenter sc = SpaceCenter.Instance;
                if (sc != null)
                {
                    Logger.Default.Log("**AH SC not null");
                    if (sc.transform != null)
                    {
                        Logger.Default.Log("**AH SC xform not null");
                        if (sc.transform.parent != null)
                            Logger.Default.Log("*AH SC parent is " + sc.transform.parent.name);
                        Logger.Default.Log("*AH SC gameobject is ? " + (sc.gameObject == null ? "Null" : "OK"));
                        if(sc.gameObject != null)
                            if (Part.GetComponentUpwards<CelestialBody>(sc.gameObject) == null)
                                Logger.Default.Log("No CB going upwards!");
                    }
                    else
                        Logger.Default.Log("SpaceCenter transform is null, can't change.");
                }
            }
            else
            {
                Debug.Log("[Kopernicus]: No spacecenter");
                Logger.Default.Log("No spacecenter when applying home for body " + body.name);
                /*GameObject scObject = new GameObject("SpaceCenter");
                scObject.transform.parent = pqs.transform;
                GameObject.DontDestroyOnLoad(scObject);
                SpaceCenter sc = scObject.AddComponent<SpaceCenter>();*/
            }
            /*Logger.Default.Log("******** Current homePQS setup");
            Utility.DumpUpwards(pqs.transform, "*");
            Utility.DumpDownwards(pqs.transform, "+");*/
        }
	}
} //namespace

