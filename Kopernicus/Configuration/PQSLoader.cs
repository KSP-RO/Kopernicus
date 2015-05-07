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
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

// Disable the "private fields `` is assigned but its value is never used warning"
#pragma warning disable 0414

namespace Kopernicus
{
	namespace Configuration 
	{
		[RequireConfigType(ConfigType.Node)]
		public class PQSLoader : IParserEventSubscriber
		{
			// PQS Material Type Enum
			private enum PQSMaterialType
			{
				Vacuum,
				AtmosphericBasic,
				AtmosphericMain,
				AtmosphericOptimized
			};

			// PQS we are creating
			public PQS pqsVersion { get; private set; }

			// Required PQSMods
			private PQSMod_CelestialBodyTransform   transform;
			private PQSMod_MaterialSetDirection     lightDirection;
			private PQSMod_UVPlanetRelativePosition uvs;
			private PQSMod_QuadMeshColliders        collider;
			
			// Surface physics material
			[ParserTarget("PhysicsMaterial", optional = true, allowMerge = true)]
			private PhysicsMaterialParser physicsMaterial
			{
				set { collider.physicsMaterial = value.material; }
			}

			// PQS level of detail settings
			[ParserTarget("minLevel", optional = true)]
			private NumericParser<int> minLevel 
			{
				set { pqsVersion.minLevel = value.value; }
			}

			[ParserTarget("maxLevel", optional = true)]
			private NumericParser<int> maxLevel 
			{
				set { pqsVersion.maxLevel = value.value; }
			}

			[ParserTarget("minDetailDistance", optional = true)]
			private NumericParser<double> minDetailDistance 
			{
				set { pqsVersion.minDetailDistance = value.value; }
			}

			[ParserTarget("maxQuadLengthsPerFrame", optional = true)]
			private NumericParser<float> maxQuadLengthsPerFrame 
			{
				set { pqsVersion.maxQuadLenghtsPerFrame = value.value; }
			}

			[PreApply]
			[ParserTarget("materialType", optional = true)]
			private EnumParser<PQSMaterialType> materialType
			{
				set 
				{
					if (value.value == PQSMaterialType.AtmosphericOptimized)
						pqsVersion.surfaceMaterial = new PQSMainOptimisedLoader ();
					else if (value.value == PQSMaterialType.AtmosphericMain)
						pqsVersion.surfaceMaterial = new PQSMainShaderLoader ();
					else if (value.value == PQSMaterialType.AtmosphericBasic)
						pqsVersion.surfaceMaterial = new PQSProjectionAerialQuadRelativeLoader ();
					else if (value.value == PQSMaterialType.Vacuum)
						pqsVersion.surfaceMaterial = new PQSProjectionSurfaceQuadLoader ();

					surfaceMaterial = pqsVersion.surfaceMaterial;
				}
			}

			// Surface Material of the PQS
			[ParserTarget("Material", optional = true, allowMerge = true)]
			private Material surfaceMaterial;

			// Fallback Material of the PQS (its always the same material)
			[ParserTarget("FallbackMaterial", optional = true, allowMerge = true)]
			private PQSProjectionFallbackLoader fallbackMaterial;
				
			// PQS Mods
			[ParserTargetCollection("Mods", optional = true, nameSignificance = NameSignificance.Type, typePrefix = "Kopernicus.Configuration.ModLoader.")]
			private List<ModLoader.ModLoader> mods = new List<ModLoader.ModLoader> (); 

			/**
			 * Constructor for new PQS
			 **/
			public PQSLoader ()
			{
				// Create a new PQS
				GameObject controllerRoot = new GameObject ();
				controllerRoot.transform.parent = Utility.Deactivator;
				this.pqsVersion = controllerRoot.AddComponent<PQS> ();

				// I am at this time unable to determine some of the magic parameters which cause the PQS to work...
				Utility.CopyObjectFields(Templates.instance.pqs, pqsVersion);
                pqsVersion.surfaceMaterial = Templates.instance.pqs.surfaceMaterial;

				// These parameters magically make the PQS work for some reason.  Need to decipher...
				/*pqsVersion.maxFrameTime = 0.075f;
				pqsVersion.subdivisionThreshold = 1;
				pqsVersion.collapseSeaLevelValue = 2;
				pqsVersion.collapseAltitudeValue = 16;
				pqsVersion.collapseAltitudeMax = 10000000;
				pqsVersion.visRadSeaLevelValue = 5;
				pqsVersion.visRadAltitudeValue = 1.79999995231628;
				pqsVersion.visRadAltitudeMax = 10000;*/

				// Create the fallback material (always the same shader)
				fallbackMaterial = new PQSProjectionFallbackLoader ();
				pqsVersion.fallbackMaterial = fallbackMaterial; 
				fallbackMaterial.name = Guid.NewGuid ().ToString ();

				// Create the celestial body transform
				GameObject mod = new GameObject("_CelestialBody");
				mod.transform.parent = controllerRoot.transform;
				transform = mod.AddComponent<PQSMod_CelestialBodyTransform>();
				transform.sphere = pqsVersion;
				transform.forceActivate = false;
				transform.deactivateAltitude = 115000;
				transform.forceRebuildOnTargetChange = false;
				transform.planetFade = new PQSMod_CelestialBodyTransform.AltitudeFade();
				transform.planetFade.fadeFloatName = "_PlanetOpacity";
				transform.planetFade.fadeStart = 100000.0f;
				transform.planetFade.fadeEnd = 110000.0f;
				transform.planetFade.valueStart = 0.0f;
				transform.planetFade.valueEnd = 1.0f;
				transform.planetFade.secondaryRenderers = new List<GameObject>();
				transform.secondaryFades = new PQSMod_CelestialBodyTransform.AltitudeFade[0];
				transform.requirements = PQS.ModiferRequirements.Default;
				transform.modEnabled = true;
				transform.order = 10;

				// Create the material direction
				mod = new GameObject("_Material_SunLight");
				mod.transform.parent = controllerRoot.gameObject.transform;
				lightDirection = mod.AddComponent<PQSMod_MaterialSetDirection>();
				lightDirection.sphere = pqsVersion;
				lightDirection.valueName = "_sunLightDirection";
				lightDirection.requirements = PQS.ModiferRequirements.Default;
				lightDirection.modEnabled = true;
				lightDirection.order = 100;

				// Create the UV planet relative position
				mod = new GameObject("_Material_SurfaceQuads");
				mod.transform.parent = controllerRoot.transform;
				uvs = mod.AddComponent<PQSMod_UVPlanetRelativePosition>();
				uvs.sphere = pqsVersion;
				uvs.requirements = PQS.ModiferRequirements.Default;
				uvs.modEnabled = true;
				uvs.order = 999999;

				// Crete the quad mesh colliders
				mod = new GameObject("QuadMeshColliders");
				mod.transform.parent = controllerRoot.gameObject.transform;
				collider = mod.AddComponent<PQSMod_QuadMeshColliders>();
				collider.sphere = pqsVersion;
				collider.maxLevelOffset = 0;
				collider.physicsMaterial = new PhysicMaterial();
				collider.physicsMaterial.name = "Ground";
				collider.physicsMaterial.dynamicFriction = 0.6f;
				collider.physicsMaterial.staticFriction = 0.8f;
				collider.physicsMaterial.bounciness = 0.0f;
				collider.physicsMaterial.frictionDirection2 = Vector3.zero;
				collider.physicsMaterial.dynamicFriction2 = 0.0f;
				collider.physicsMaterial.staticFriction2 = 0.0f;
				collider.physicsMaterial.frictionCombine = PhysicMaterialCombine.Maximum;
				collider.physicsMaterial.bounceCombine = PhysicMaterialCombine.Average;
				collider.requirements = PQS.ModiferRequirements.Default;
				collider.modEnabled = true;
				collider.order = 100;

				// Create physics material editor
				physicsMaterial = new PhysicsMaterialParser (collider.physicsMaterial);
            }

			/**
			 * Constructor for pre-existing PQS
			 * 
			 * @param pqsVersion Existing PQS to augment
			 **/
			public PQSLoader (PQS pqsVersion)
			{
				this.pqsVersion = pqsVersion;

				// Get the required PQS information
				transform = pqsVersion.GetComponentsInChildren<PQSMod_CelestialBodyTransform> (true).Where (mod => mod.transform.parent == pqsVersion.transform).FirstOrDefault ();
				lightDirection = pqsVersion.GetComponentsInChildren<PQSMod_MaterialSetDirection>(true).Where (mod => mod.transform.parent == pqsVersion.transform).FirstOrDefault ();
				uvs = pqsVersion.GetComponentsInChildren<PQSMod_UVPlanetRelativePosition>(true).Where (mod => mod.transform.parent == pqsVersion.transform).FirstOrDefault ();
				collider = pqsVersion.GetComponentsInChildren<PQSMod_QuadMeshColliders>(true).Where (mod => mod.transform.parent == pqsVersion.transform).FirstOrDefault ();

				// Create physics material editor
				physicsMaterial = new PhysicsMaterialParser (collider.physicsMaterial);

				// Clone the surface material of the PQS
				if (PQSMainOptimisedLoader.UsesSameShader (pqsVersion.surfaceMaterial))
					pqsVersion.surfaceMaterial = new PQSMainOptimisedLoader (pqsVersion.surfaceMaterial);
				else if (PQSMainShaderLoader.UsesSameShader (pqsVersion.surfaceMaterial))
					pqsVersion.surfaceMaterial = new PQSMainShaderLoader (pqsVersion.surfaceMaterial);
				else if (PQSProjectionAerialQuadRelativeLoader.UsesSameShader (pqsVersion.surfaceMaterial))
					pqsVersion.surfaceMaterial = new PQSProjectionAerialQuadRelativeLoader (pqsVersion.surfaceMaterial);
				else if (PQSProjectionSurfaceQuadLoader.UsesSameShader (pqsVersion.surfaceMaterial))
					pqsVersion.surfaceMaterial = new PQSProjectionSurfaceQuadLoader (pqsVersion.surfaceMaterial);
				surfaceMaterial = pqsVersion.surfaceMaterial;
				surfaceMaterial.name = Guid.NewGuid ().ToString ();

				// Clone the fallback material of the PQS
				fallbackMaterial = new PQSProjectionFallbackLoader (pqsVersion.fallbackMaterial);
				pqsVersion.fallbackMaterial = fallbackMaterial; 
				fallbackMaterial.name = Guid.NewGuid ().ToString ();
			}


			void IParserEventSubscriber.Apply(ConfigNode node)
			{

			}

			void IParserEventSubscriber.PostApply(ConfigNode node)
			{
                List<PQSMod> cpMods = pqsVersion.GetComponentsInChildren<PQSMod>(true).ToList();
				// Add all created mods to the PQS
                foreach (ModLoader.ModLoader loader in mods)
                {
                    List<PQSMod> currentMods = cpMods.Where(m => m.GetType() == loader.mod.GetType()).ToList();
                    if (currentMods.Count > 0)
                    {
                        for (int i = 0; i < currentMods.Count; i++)
                        {
                            PQSMod delMod = pqsVersion.GetComponentsInChildren(currentMods[i].GetType(), true)[i] as PQSMod;
                            delMod.transform.parent = null;
                            delMod.sphere = null;
                            PQSMod.Destroy(delMod);
                            cpMods.Remove(currentMods[i]);
                        }
                    }
                    loader.mod.transform.parent = pqsVersion.transform;
                    loader.mod.sphere = pqsVersion;
                    Logger.Active.Log("PQSLoader.PostApply(ConfigNode): Added PQS Mod => " + loader.mod.GetType());
                }

				// Make sure all the PQSMods exist in Localspace
				pqsVersion.gameObject.SetLayerRecursive(Constants.GameLayers.LocalSpace);
			}
		}
	}
}

#pragma warning restore 0414
