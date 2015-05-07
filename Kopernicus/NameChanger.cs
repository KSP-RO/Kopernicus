using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace Kopernicus
{
    public class NameChanger
    {
        string oldName, newName;
        List<Component> components;
        List<GameObject> gameObjects;
        CelestialBody body;

        public NameChanger(string o, string n)
        {
            oldName = o;
            newName = n;
            components = new List<Component>();
            gameObjects = new List<GameObject>();
        }

        public void AddComponent(Component c)
        {
            if(c != null)
                components.Add(c);
        }

        public void AddGameObject(GameObject g)
        {
            if(g != null)
                gameObjects.Add(g);
        }

        public void SetBody(CelestialBody b)
        {
            body = b;
        }

        public void Apply()
        {
            foreach (Component c in components)
                if(c != null)
                    c.name = c.name.Replace(oldName, newName);
            foreach (GameObject g in gameObjects)
                if(g != null)
                    g.name = g.name.Replace(oldName, newName);

            components.Clear();
            gameObjects.Clear();
            foreach (CelestialBody b in FlightGlobals.Bodies)
            {
                if (b.bodyName != oldName)
                    continue;
                b.bodyName = b.bodyName.Replace(oldName, newName);
                b.gameObject.name = b.gameObject.name.Replace(oldName, newName);
                b.transform.name = b.transform.name.Replace(oldName, newName);
                b.bodyTransform.name = b.bodyTransform.name.Replace(oldName, newName);

                if (body.pqsController != null)
                {
                    body.pqsController.name = body.pqsController.name.Replace(oldName, newName);
                    body.pqsController.transform.name = body.pqsController.transform.name.Replace(oldName, newName);
                    body.pqsController.gameObject.name = body.pqsController.gameObject.name.Replace(oldName, newName);

                    foreach (PQS p in b.pqsController.transform.GetComponentsInChildren<PQS>())
                    {
                        p.name = p.name.Replace(oldName, newName);
                        p.transform.name = p.transform.name.Replace(oldName, newName);
                        p.gameObject.name = p.gameObject.name.Replace(oldName, newName);
                    }
                }
            }
            if (ScaledSpace.Instance != null)
            {
                foreach (Transform t in ScaledSpace.Instance.scaledSpaceTransforms)
                {
                    if(t.name == oldName)
                        t.name = t.name.Replace(oldName, newName);
                }
            }
        }
    }

    public class NameChanges
    {
        public static NameChanges instance = null;
        public Dictionary<string, NameChanger> nameDict;
        public static Dictionary<string, NameChanger> Names
        {
            get
            {
                if (instance == null)
                    return null;
                return instance.nameDict;
            }
        }

        public NameChanges()
        {
            instance = this;
            nameDict = new Dictionary<string, NameChanger>();
        }
    }

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class NameChangeRunner : MonoBehaviour
    {
        public void Start()
        {
            //Dumps
            PQSCity ksc = null;
            foreach (PQSCity p in Resources.FindObjectsOfTypeAll<PQSCity>())
            {
                if (p.name == "KSC")
                {
                    ksc = p;
                    break;
                }
            }
            if (ksc != null)
            {
                Logger.Default.Log("Found KSC!");
                Logger.Default.Log("Transform = transform of go? " + (ksc.gameObject.transform == ksc.transform ? "Yes" : "No"));
                Logger.Default.Log("Transform name = " + ksc.transform.name + ", go name " + ksc.gameObject.name);
                Injector.DumpUpwards(ksc.transform, "*");
            }
            if (SpaceCenter.Instance != null)
            {
                SpaceCenter sc = SpaceCenter.Instance;
                Logger.Default.Log("Found Space center!");
                Logger.Default.Log("Transform = transform of go? " + (sc.gameObject.transform == sc.transform ? "Yes" : "No"));
                Logger.Default.Log("Transform name = " + sc.transform.name + ", go name " + sc.gameObject.name);
                Injector.DumpUpwards(sc.transform, "*");
            }
            if (NameChanges.instance == null)
            {
                Debug.Log("*NameChanger* ERROR instance is null!");
                return;
            }
            foreach (NameChanger n in NameChanges.Names.Values)
                n.Apply();
        }
    }
}
