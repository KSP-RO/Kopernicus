using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;

namespace Kopernicus
{
    public class Templates
    {
        static public Templates instance = null;
        public PQSCity ksc = null;
        public PQS pqsOcean = null;
        public PQS pqs = null;
        public GameObject corona;
        public Dictionary<string, MapSO> mapsGray;
        public Dictionary<string, MapSO> mapsRGB;

        public ConfigNode homeNode;
        public CelestialBody homeBody;
        public PQS homePQS;
        public Dictionary<string, NameChanger> nameChanger;

        // PQSmods
        public PQSMod_VoronoiCraters voronoiCraters;
        
        public Templates()
        {
            instance = this;

            PSystemBody Body = Utility.FindBody(PSystemManager.Instance.systemPrefab.rootBody, "Laythe");
            if (Body != null)
            {
                foreach (PQS oceanPQS in Body.pqsVersion.GetComponentsInChildren<PQS>(true))
                {
                    if (oceanPQS.name == "LaytheOcean")
                    {
                        pqsOcean = oceanPQS;
                        break;
                    }
                }
                pqs = Body.pqsVersion;
            }

            // We need to get the body for the Sun (to steal it's corona mesh)
            Body = Utility.FindBody(PSystemManager.Instance.systemPrefab.rootBody, "Sun");
            corona = Body.scaledVersion.GetComponentsInChildren<SunCoronas>(true).First().gameObject;

            // Get some mods
            voronoiCraters = Utility.FindBody(PSystemManager.Instance.systemPrefab.rootBody, "Mun").pqsVersion.GetComponentsInChildren<PQSMod_VoronoiCraters>(true)[0] as PQSMod_VoronoiCraters;
            

            // KSC
            Body = Utility.FindBody(PSystemManager.Instance.systemPrefab.rootBody, "Kerbin");
            foreach (PQSCity c in Body.pqsVersion.GetComponentsInChildren<PQSCity>(true))
            {
                if (c.name == "KSC")
                {
                    ksc = c;
                    Logger.Default.Log("Found KSC");
                    break;
                }
            }

            mapsGray = new Dictionary<string, MapSO>();
            mapsRGB = new Dictionary<string, MapSO>();
            nameChanger = new Dictionary<string, NameChanger>();

        }
    }
}
