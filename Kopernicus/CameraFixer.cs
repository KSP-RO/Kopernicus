using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;

namespace Kopernicus
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class SpaceCenterCameraFixer : MonoBehaviour
    {
        static public string pqsName = "Kerbin";
        public void Start()
        {
            SpaceCenterCamera[] cams = Resources.FindObjectsOfTypeAll<SpaceCenterCamera>();
            Type camType = typeof(SpaceCenterCamera);
            if (cams != null && cams.Length > 0)
            {
                Debug.Log("*CF cams length = " + cams.Length);
                foreach (SpaceCenterCamera cam in cams)
                {
                    cam.pqsName = pqsName;
                    camType.GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(cam, null);
                    cam.ResetCamera();
                }
            }
            else
            {
                Debug.Log("*CF cams null or empty");
            }


            SpaceCenterCamera2[] cams2 = Resources.FindObjectsOfTypeAll<SpaceCenterCamera2>();
            Type camType2 = typeof(SpaceCenterCamera2);
            PQSCity ksc = SpaceCenter.Instance.transform.parent.GetComponent<PQSCity>();
            double altitudeInitial = 0d;
            bool resetHeight = false;
            if (ksc != null)
            {
                resetHeight = true;
                Debug.Log("*CF Found KSC, resetting alt");
                if (ksc.repositionToSphere || ksc.repositionToSphereSurface)
                {
                    double nomHeight = ksc.sphere.GetSurfaceHeight((Vector3d)ksc.repositionRadial.normalized) - ksc.sphere.radius;
                    if (ksc.repositionToSphereSurface)
                    {
                        nomHeight += ksc.repositionRadiusOffset;
                    }
                    altitudeInitial = -nomHeight;
                }
                else
                {
                    altitudeInitial = -ksc.repositionRadiusOffset;
                }
            }
            if (cams2 != null && cams2.Length > 0)
            {
                Debug.Log("*CF cams2 length = " + cams.Length);
                foreach (SpaceCenterCamera2 cam in cams2)
                {
                    cam.pqsName = pqsName;
                    camType2.GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(cam, null);
                    if (resetHeight)
                    {
                        cam.altitudeInitial = (float)altitudeInitial;
                        cam.ResetCamera();
                    }
                }
            }
            else
            {
                Debug.Log("*CF cams2 null or empty");
            }
        }
    }
}
