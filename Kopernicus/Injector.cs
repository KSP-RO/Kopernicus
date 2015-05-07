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
        public NameChanges nameChange = null;
        static public void DumpUpwards(Transform t, string prefix)
        {
            Logger.Default.Log(prefix + "Transform " + t.name);
            foreach(Component c in t.GetComponents<Component>())
                Logger.Default.Log(prefix + " has component " + c.name + " of type " + c.GetType().FullName);
            if(t.parent != null)
                DumpUpwards(t.parent, prefix + "  ");
      
        }
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
            
            // Grab templates, create namechanger
            templates = new Templates();
            nameChange = new NameChanges();


			// THIS IS WHERE THE MAGIC HAPPENS - OVERWRITE THE SYSTEM PREFAB SO KSP ACCEPTS OUR CUSTOM SOLAR SYSTEM AS IF IT WERE FROM SQUAD
			PSystemManager.Instance.systemPrefab = (new Configuration.Loader()).Generate();

			// SEARCH FOR THE ARCHIVES CONTROLLER PREFAB AND OVERWRITE IT WITH THE CUSTOM SYSTEM
			RDArchivesController archivesController = AssetBase.RnDTechTree.GetRDScreenPrefab ().GetComponentsInChildren<RDArchivesController> (true).First ();
			archivesController.systemPrefab = PSystemManager.Instance.systemPrefab;

			// Clear space center instance so it will accept nouveau Kerbin
			//SpaceCenter.Instance = null;
            Logger.Default.Log("*SC* is it null? " + (SpaceCenter.Instance == null).ToString());
            if (SpaceCenter.Instance != null)
            {
                SpaceCenter sc = SpaceCenter.Instance;
                Logger.Default.Log("** SpaceCenter");
                DumpUpwards(sc.transform, "");
            }

            // Set home stuff
            ApplyHome(Templates.instance.homeNode, Templates.instance.homePQS, Templates.instance.homeBody);

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
            Logger.Default.Log("Applying home to body " + body.name);
            PQSCity ksc = Templates.instance.ksc;
            if (ksc != null)
            {
                ksc.sphere = pqs;
                ksc.transform.parent = pqs.transform;
                PSystemSetup.Instance.pqsToActivate = pqs.gameObject.name;
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
            if (SpaceCenter.Instance != null)
            {
                Debug.Log("[Kopernicus]: Setting spacecenter");
                SpaceCenter sc = SpaceCenter.Instance;
                sc.transform.parent = sc.SpaceCenterTransform.parent = pqs.transform;
                sc.transform.position = ksc.transform.position;
                sc.transform.rotation = ksc.transform.rotation;
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
        }
	}
} //namespace

