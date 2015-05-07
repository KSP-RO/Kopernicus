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
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Kopernicus
{
    public class Utility
    {
        /**
         * @return LocalSpace game object
         */
        public static GameObject LocalSpace
        {
            get { return GameObject.Find(PSystemManager.Instance.localSpaceName); }
        }

        // Static object representing the deactivator
        private static GameObject deactivator;

        /**
         * Get an object which is deactivated, essentially, and children are prefabs
         * @return shared deactivated object for making prefabs
         */
        public static Transform Deactivator
        {
            get
            {
                if (deactivator == null)
                {
                    deactivator = new GameObject("__deactivator");
                    deactivator.SetActive(false);
                    UnityEngine.Object.DontDestroyOnLoad(deactivator);
                }
                return deactivator.transform;
            }
        }

        /**
         * Copy one objects fields to another object via reflection
         * @param source Object to copy fields from
         * @param destination Object to copy fields to
         **/
        public static void CopyObjectFields<T>(T source, T destination)
        {
            // Reflection based copy
            foreach (FieldInfo field in (typeof(T)).GetFields())
            {
                // Only copy non static fields
                if (!field.IsStatic)
                {
                    Logger.Active.Log("Copying \"" + field.Name + "\": " + (field.GetValue(destination) ?? "<NULL>") + " => " + (field.GetValue(source) ?? "<NULL>"));
                    field.SetValue(destination, field.GetValue(source));
                }
            }
        }

        /**
         * Recursively searches for a named transform in the Transform heirarchy.  The requirement of
         * such a function is sad.  This should really be in the Unity3D API.  Transform.Find() only
         * searches in the immediate children.
         *
         * @param transform Transform in which is search for named child
         * @param name Name of child to find
         * 
         * @return Desired transform or null if it could not be found
         */
        public static Transform FindInChildren(Transform transform, string name)
        {
            // Is this null?
            if (transform == null)
            {
                return null;
            }

            // Are the names equivalent
            if (transform.name == name)
            {
                return transform;
            }

            // If we did not find a transform, search through the children
            foreach (Transform child in transform)
            {
                // Recurse into the child
                Transform t = FindInChildren(child, name);
                if (t != null)
                {
                    return t;
                }
            }

            // Return the transform (will be null if it was not found)
            return null;
        }

        // Dump an object by reflection
        public static void DumpObjectFields(object o, string title = "---------")
        {
            // Dump the raw PQS of Dres (by reflection)
            Logger.Active.Log("---------" + title + "------------");
            foreach (FieldInfo field in o.GetType().GetFields())
            {
                if (!field.IsStatic)
                {
                    Logger.Active.Log(field.Name + " = " + field.GetValue(o));
                }
            }
            Logger.Active.Log("--------------------------------------");
        }

        public static void DumpObjectProperties(object o, string title = "---------")
        {
            // Iterate through all of the properties
            Logger.Active.Log("--------- " + title + " ------------");
            foreach (PropertyInfo property in o.GetType().GetProperties())
            {
                if (property.CanRead)
                    Logger.Active.Log(property.Name + " = " + property.GetValue(o, null));
            }
            Logger.Active.Log("--------------------------------------");
        }

        /**
         * Recursively searches for a named PSystemBody
         *
         * @param body Parent body to begin search in
         * @param name Name of body to find
         * 
         * @return Desired body or null if not found
         */
        public static PSystemBody FindBody(PSystemBody body, string name)
        {
            // Is this the body wer are looking for?
            if (body.celestialBody.bodyName == name)
                return body;

            // Otherwise search children
            foreach (PSystemBody child in body.children)
            {
                PSystemBody b = FindBody(child, name);
                if (b != null)
                    return b;
            }

            // Return null because we didn't find shit
            return null;
        }

        /** 
         * Get the reference Geosphere of KSP (Jool's scaled space mesh)
         * 
         * @return The reference geosphere (1000 unit radius)
         */
        public static Mesh ReferenceGeosphere()
        {
            // We need to get the body for Jool (to steal it's mesh)
            PSystemBody Jool = Utility.FindBody(PSystemManager.Instance.systemPrefab.rootBody, "Jool");

            // Return it's mesh
            return Jool.scaledVersion.GetComponent<MeshFilter>().sharedMesh;
        }

        // Print out a tree containing all the objects in the game
        public static void PerformObjectDump()
        {
            Logger.Active.Log("--------- Object Dump -----------");
            foreach (GameObject b in GameObject.FindObjectsOfType(typeof(GameObject)))
            {
                // Essentially, we iterate through all game objects currently alive and search for 
                // the ones without a parent.  Extrememly inefficient and terrible, but its just for
                // exploratory purposes
                if (b.transform.parent == null)
                {
                    // Print out the tree of child objects
                    GameObjectWalk(b, "");
                }
            }
            Logger.Active.Log("---------------------------------");
        }

        public static void PrintTransform(Transform t, string title = "")
        {
            Logger.Active.Log("------" + title + "------");
            Logger.Active.Log("Position: " + t.localPosition);
            Logger.Active.Log("Rotation: " + t.localRotation);
            Logger.Active.Log("Scale: " + t.localScale);
            Logger.Active.Log("------------------");
        }

        // Print out the tree of components 
        public static void GameObjectWalk(GameObject o, String prefix = "")
        {
            // If null, don't do anything
            if (o == null)
                return;

            // Print this object
            Logger.Active.Log(prefix + o);
            Logger.Active.Log(prefix + " >>> Components <<< ");
            foreach (Component c in o.GetComponents(typeof(Component)))
            {
                Logger.Active.Log(prefix + " " + c);
            }
            Logger.Active.Log(prefix + " >>> ---------- <<< ");

            // Game objects are related to each other via transforms in Unity3D.
            foreach (Transform b in o.transform)
            {
                if (b != null)
                    GameObjectWalk(b.gameObject, "    " + prefix);
            }
        }

        // Print out the celestial bodies
        public static void PSystemBodyWalk(PSystemBody b, String prefix = "")
        {
            Logger.Active.Log(prefix + b.celestialBody.bodyName + ":" + b.flightGlobalsIndex);
            foreach (PSystemBody c in b.children)
            {
                PSystemBodyWalk(c, prefix + "    ");
            }
        }

        public static void CopyMesh(Mesh source, Mesh dest)
        {
            //ProfileTimer.Push("CopyMesh");
            Vector3[] verts = new Vector3[source.vertexCount];
            source.vertices.CopyTo(verts, 0);
            dest.vertices = verts;

            int[] tris = new int[source.triangles.Length];
            source.triangles.CopyTo(tris, 0);
            dest.triangles = tris;

            Vector2[] uvs = new Vector2[source.uv.Length];
            source.uv.CopyTo(uvs, 0);
            dest.uv = uvs;

            Vector2[] uv2s = new Vector2[source.uv2.Length];
            source.uv2.CopyTo(uv2s, 0);
            dest.uv2 = uv2s;

            Vector3[] normals = new Vector3[source.normals.Length];
            source.normals.CopyTo(normals, 0);
            dest.normals = normals;

            Vector4[] tangents = new Vector4[source.tangents.Length];
            source.tangents.CopyTo(tangents, 0);
            dest.tangents = tangents;

            Color[] colors = new Color[source.colors.Length];
            source.colors.CopyTo(colors, 0);
            dest.colors = colors;

            Color32[] colors32 = new Color32[source.colors32.Length];
            source.colors32.CopyTo(colors32, 0);
            dest.colors32 = colors32;

            //ProfileTimer.Pop("CopyMesh");
        }

        public static Mesh DuplicateMesh(Mesh source)
        {
            // Create new mesh object
            Mesh dest = new Mesh();

            //ProfileTimer.Push("CopyMesh");
            Vector3[] verts = new Vector3[source.vertexCount];
            source.vertices.CopyTo(verts, 0);
            dest.vertices = verts;

            int[] tris = new int[source.triangles.Length];
            source.triangles.CopyTo(tris, 0);
            dest.triangles = tris;

            Vector2[] uvs = new Vector2[source.uv.Length];
            source.uv.CopyTo(uvs, 0);
            dest.uv = uvs;

            Vector2[] uv2s = new Vector2[source.uv2.Length];
            source.uv2.CopyTo(uv2s, 0);
            dest.uv2 = uv2s;

            Vector3[] normals = new Vector3[source.normals.Length];
            source.normals.CopyTo(normals, 0);
            dest.normals = normals;

            Vector4[] tangents = new Vector4[source.tangents.Length];
            source.tangents.CopyTo(tangents, 0);
            dest.tangents = tangents;

            Color[] colors = new Color[source.colors.Length];
            source.colors.CopyTo(colors, 0);
            dest.colors = colors;

            Color32[] colors32 = new Color32[source.colors32.Length];
            source.colors32.CopyTo(colors32, 0);
            dest.colors32 = colors32;

            //ProfileTimer.Pop("CopyMesh");
            return dest;
        }

        // Taken from Nathankell's RSS Utils.cs; uniformly scaled vertices
        public static void ScaleVerts(Mesh mesh, float scaleFactor)
        {
            //ProfileTimer.Push("ScaleVerts");
            Vector3[] vertices = new Vector3[mesh.vertexCount];
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                Vector3 v = mesh.vertices[i];
                v *= scaleFactor;
                vertices[i] = v;
            }
            mesh.vertices = vertices;
            //ProfileTimer.Pop("ScaleVerts");
        }

        public static void RecalculateTangents(Mesh theMesh)
        {
            int vertexCount = theMesh.vertexCount;
            Vector3[] vertices = theMesh.vertices;
            Vector3[] normals = theMesh.normals;
            Vector2[] texcoords = theMesh.uv;
            int[] triangles = theMesh.triangles;
            int triangleCount = triangles.Length / 3;

            var tangents = new Vector4[vertexCount];
            var tan1 = new Vector3[vertexCount];
            var tan2 = new Vector3[vertexCount];

            int tri = 0;

            for (int i = 0; i < (triangleCount); i++)
            {
                int i1 = triangles[tri];
                int i2 = triangles[tri + 1];
                int i3 = triangles[tri + 2];

                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                Vector3 v3 = vertices[i3];

                Vector2 w1 = texcoords[i1];
                Vector2 w2 = texcoords[i2];
                Vector2 w3 = texcoords[i3];

                float x1 = v2.x - v1.x;
                float x2 = v3.x - v1.x;
                float y1 = v2.y - v1.y;
                float y2 = v3.y - v1.y;
                float z1 = v2.z - v1.z;
                float z2 = v3.z - v1.z;

                float s1 = w2.x - w1.x;
                float s2 = w3.x - w1.x;
                float t1 = w2.y - w1.y;
                float t2 = w3.y - w1.y;

                float r = 1.0f / (s1 * t2 - s2 * t1);
                var sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                var tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;

                tri += 3;
            }
            for (int i = 0; i < (vertexCount); i++)
            {
                Vector3 n = normals[i];
                Vector3 t = tan1[i];

                // Gram-Schmidt orthogonalize
                Vector3.OrthoNormalize(ref n, ref t);

                tangents[i].x = t.x;
                tangents[i].y = t.y;
                tangents[i].z = t.z;

                // Calculate handedness
                tangents[i].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0.0f) ? -1.0f : 1.0f;
            }
            theMesh.tangents = tangents;
        }

        // Serialize a mesh to disk
        public static void SerializeMesh(Mesh mesh, string path)
        {
            // Open an output filestream
            System.IO.FileStream outputStream = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            System.IO.BinaryWriter writer = new System.IO.BinaryWriter(outputStream);

            // Write the vertex count of the mesh
            writer.Write(mesh.vertices.Length);
            foreach (Vector3 vertex in mesh.vertices)
            {
                writer.Write(vertex.x);
                writer.Write(vertex.y);
                writer.Write(vertex.z);
            }
            writer.Write(mesh.uv.Length);
            foreach (Vector2 uv in mesh.uv)
            {
                writer.Write(uv.x);
                writer.Write(uv.y);
            }
            writer.Write(mesh.triangles.Length);
            foreach (int triangle in mesh.triangles)
            {
                writer.Write(triangle);
            }

            // Finish writing
            writer.Close();
            outputStream.Close();
        }

        // Deserialize a mesh from disk
        public static Mesh DeserializeMesh(string path)
        {
            System.IO.FileStream inputStream = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            System.IO.BinaryReader reader = new System.IO.BinaryReader(inputStream);

            // Get the vertices
            int count = reader.ReadInt32();
            Vector3[] vertices = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                Vector3 vertex;
                vertex.x = reader.ReadSingle();
                vertex.y = reader.ReadSingle();
                vertex.z = reader.ReadSingle();
                vertices[i] = vertex;
            }

            // Get the uvs
            int uv_count = reader.ReadInt32();
            Vector2[] uvs = new Vector2[uv_count];
            for (int i = 0; i < uv_count; i++)
            {
                Vector2 uv;
                uv.x = reader.ReadSingle();
                uv.y = reader.ReadSingle();
                uvs[i] = uv;
            }

            // Get the triangles
            int tris_count = reader.ReadInt32();
            int[] triangles = new int[tris_count];
            for (int i = 0; i < tris_count; i++)
                triangles[i] = reader.ReadInt32();

            // Close
            reader.Close();
            inputStream.Close();

            // Create the mesh
            Mesh m = new Mesh();
            m.vertices = vertices;
            m.triangles = triangles;
            m.uv = uvs;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        // Credit goes to Kragrathea.
        public static Texture2D BumpToNormalMap(Texture2D source, float strength)
        {
            strength = Mathf.Clamp(strength, 0.0F, 10.0F);
            var result = new Texture2D(source.width, source.height, TextureFormat.ARGB32, true);
            for (int by = 0; by < result.height; by++)
            {
                for (var bx = 0; bx < result.width; bx++)
                {
                    var xLeft = source.GetPixel(bx - 1, by).grayscale * strength;
                    var xRight = source.GetPixel(bx + 1, by).grayscale * strength;
                    var yUp = source.GetPixel(bx, by - 1).grayscale * strength;
                    var yDown = source.GetPixel(bx, by + 1).grayscale * strength;
                    var xDelta = ((xLeft - xRight) + 1) * 0.5f;
                    var yDelta = ((yUp - yDown) + 1) * 0.5f;
                    result.SetPixel(bx, by, new Color(xDelta, yDelta, 1.0f, xDelta));
                }
            }
            result.Apply();
            return result;
        }

        // Convert latitude-longitude-altitude with body radius to a vector.
        public static Vector3 LLAtoECEF(double lat, double lon, double alt, double radius)
        {
            const double degreesToRadians = Math.PI / 180.0;
            lat = (lat - 90) * degreesToRadians;
            lon *= degreesToRadians;
            double x, y, z;
            double n = radius; // for now, it's still a sphere, so just the radius
            x = (n + alt) * -1.0 * Math.Sin(lat) * Math.Cos(lon);
            y = (n + alt) * Math.Cos(lat); // for now, it's still a sphere, so no eccentricity
            z = (n + alt) * -1.0 * Math.Sin(lat) * Math.Sin(lon);
            return new Vector3((float)x, (float)y, (float)z);
        }

        public static Texture2D LoadTexture(string path, bool compress, bool upload, bool unreadable)
        {
            Texture2D map = null;
            path = KSPUtil.ApplicationRootPath + "GameData/" + path;
            if (System.IO.File.Exists(path))
            {
                bool uncaught = true;
                try
                {
                    if (path.ToLower().EndsWith(".dds"))
                    {
                        // Borrowed from stock KSP 1.0 DDS loader (hi Mike!)
                        // Also borrowed the extra bits from Sarbian.
                        byte[] buffer = System.IO.File.ReadAllBytes(path);
                        System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(new System.IO.MemoryStream(buffer));
                        uint num = binaryReader.ReadUInt32();
                        if (num == DDSHeaders.DDSValues.uintMagic)
                        {

                            DDSHeaders.DDSHeader dDSHeader = new DDSHeaders.DDSHeader(binaryReader);

                            if (dDSHeader.ddspf.dwFourCC == DDSHeaders.DDSValues.uintDX10)
                            {
                                new DDSHeaders.DDSHeaderDX10(binaryReader);
                            }

                            bool alpha = (dDSHeader.dwFlags & 0x00000002) != 0;
                            bool fourcc = (dDSHeader.dwFlags & 0x00000004) != 0;
                            bool rgb = (dDSHeader.dwFlags & 0x00000040) != 0;
                            bool alphapixel = (dDSHeader.dwFlags & 0x00000001) != 0;
                            bool luminance = (dDSHeader.dwFlags & 0x00020000) != 0;
                            bool rgb888 = dDSHeader.ddspf.dwRBitMask == 0x000000ff && dDSHeader.ddspf.dwGBitMask == 0x0000ff00 && dDSHeader.ddspf.dwBBitMask == 0x00ff0000;
                            //bool bgr888 = dDSHeader.ddspf.dwRBitMask == 0x00ff0000 && dDSHeader.ddspf.dwGBitMask == 0x0000ff00 && dDSHeader.ddspf.dwBBitMask == 0x000000ff;
                            bool rgb565 = dDSHeader.ddspf.dwRBitMask == 0x0000F800 && dDSHeader.ddspf.dwGBitMask == 0x000007E0 && dDSHeader.ddspf.dwBBitMask == 0x0000001F;
                            bool argb4444 = dDSHeader.ddspf.dwABitMask == 0x0000f000 && dDSHeader.ddspf.dwRBitMask == 0x00000f00 && dDSHeader.ddspf.dwGBitMask == 0x000000f0 && dDSHeader.ddspf.dwBBitMask == 0x0000000f;
                            bool rbga4444 = dDSHeader.ddspf.dwABitMask == 0x0000000f && dDSHeader.ddspf.dwRBitMask == 0x0000f000 && dDSHeader.ddspf.dwGBitMask == 0x000000f0 && dDSHeader.ddspf.dwBBitMask == 0x00000f00;

                            bool mipmap = (dDSHeader.dwCaps & DDSHeaders.DDSPixelFormatCaps.MIPMAP) != (DDSHeaders.DDSPixelFormatCaps)0u;
                            bool isNormalMap = ((dDSHeader.ddspf.dwFlags & 524288u) != 0u || (dDSHeader.ddspf.dwFlags & 2147483648u) != 0u);
                            if (fourcc)
                            {
                                if (dDSHeader.ddspf.dwFourCC == DDSHeaders.DDSValues.uintDXT1)
                                {
                                    map = new Texture2D((int)dDSHeader.dwWidth, (int)dDSHeader.dwHeight, TextureFormat.DXT1, mipmap);
                                    map.LoadRawTextureData(binaryReader.ReadBytes((int)(binaryReader.BaseStream.Length - binaryReader.BaseStream.Position)));
                                }
                                else if (dDSHeader.ddspf.dwFourCC == DDSHeaders.DDSValues.uintDXT3)
                                {
                                    map = new Texture2D((int)dDSHeader.dwWidth, (int)dDSHeader.dwHeight, (TextureFormat)11, mipmap);
                                    map.LoadRawTextureData(binaryReader.ReadBytes((int)(binaryReader.BaseStream.Length - binaryReader.BaseStream.Position)));
                                }
                                else if (dDSHeader.ddspf.dwFourCC == DDSHeaders.DDSValues.uintDXT5)
                                {
                                    map = new Texture2D((int)dDSHeader.dwWidth, (int)dDSHeader.dwHeight, TextureFormat.DXT5, mipmap);
                                    map.LoadRawTextureData(binaryReader.ReadBytes((int)(binaryReader.BaseStream.Length - binaryReader.BaseStream.Position)));
                                }
                                else if (dDSHeader.ddspf.dwFourCC == DDSHeaders.DDSValues.uintDXT2)
                                {
                                    Debug.Log("[Kopernicus]: DXT2 not supported" + path);
                                }
                                else if (dDSHeader.ddspf.dwFourCC == DDSHeaders.DDSValues.uintDXT4)
                                {
                                    Debug.Log("[Kopernicus]: DXT4 not supported: " + path);
                                }
                                else if (dDSHeader.ddspf.dwFourCC == DDSHeaders.DDSValues.uintDX10)
                                {
                                    Debug.Log("[Kopernicus]: DX10 dds not supported: " + path);
                                }
                                else
                                    fourcc = false;
                            }
                            if(!fourcc)
                            {
                                TextureFormat textureFormat = TextureFormat.ARGB32;
                                bool ok = true;
                                if (rgb && (rgb888 /*|| bgr888*/))
                                {
                                    // RGB or RGBA format
                                    textureFormat = alphapixel
                                    ? TextureFormat.RGBA32
                                    : TextureFormat.RGB24;
                                }
                                else if (rgb && rgb565)
                                {
                                    // Nvidia texconv B5G6R5_UNORM
                                    textureFormat = TextureFormat.RGB565;
                                }
                                else if (rgb && alphapixel && argb4444)
                                {
                                    // Nvidia texconv B4G4R4A4_UNORM
                                    textureFormat = TextureFormat.ARGB4444;
                                }
                                else if (rgb && alphapixel && rbga4444)
                                {
                                    textureFormat = TextureFormat.RGBA4444;
                                }
                                else if (!rgb && alpha != luminance)
                                {
                                    // A8 format or Luminance 8
                                    textureFormat = TextureFormat.Alpha8;
                                }
                                else
                                {
                                    ok = false;
                                    Debug.Log("[Kopernicus]: Only DXT1, DXT5, A8, RGB24, RGBA32, RGB565, ARGB4444 and RGBA4444 are supported");
                                }
                                if (ok)
                                {
                                    map = new Texture2D((int)dDSHeader.dwWidth, (int)dDSHeader.dwHeight, textureFormat, mipmap);
                                    map.LoadRawTextureData(binaryReader.ReadBytes((int)(binaryReader.BaseStream.Length - binaryReader.BaseStream.Position)));
                                }

                            }
                            if (map != null)
                                if (upload)
                                    map.Apply(false, unreadable);
                        }
                        else
                            Debug.Log("[Kopernicus]: Bad DDS header.");
                    }
                    else
                    {
                        map = new Texture2D(2, 2);
                        map.LoadImage(System.IO.File.ReadAllBytes(path));
                        if (compress)
                            map.Compress(true);
                        if (upload)
                            map.Apply(false, unreadable);
                    }
                }
                catch (Exception e)
                {
                    uncaught = false;
                    Debug.Log("[Kopernicus]: failed to load " + path + " with exception " + e.Message);
                }
                if (map == null && uncaught)
                {
                    Debug.Log("[Kopernicus]: failed to load " + path);
                }
            }
            else
                Debug.Log("[Kopernicus]: texture does not exist! " + path);

            return map;
        }

        /** 
         * Enumerable class to iterate over parents.  Defined to allow us to use Linq
         * and predicates. 
         *
         * See examples: http://msdn.microsoft.com/en-us/library/78dfe2yb(v=vs.110).aspx
         **/
        public class ParentEnumerator : IEnumerable<GameObject>
        {
            // The game object who and whose parents are going to be enumerated
            private GameObject initial;

            // Enumerator class
            public class Enumerator : IEnumerator<GameObject>
            {
                public GameObject original;
                public GameObject current;

                public Enumerator(GameObject initial)
                {
                    this.original = initial;
                    this.current = this.original;
                }

                public bool MoveNext()
                {
                    if (current.transform.parent != null && current.transform.parent == current.transform)
                    {
                        current = current.transform.parent.gameObject;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                public void Reset()
                {
                    current = original;
                }

                void IDisposable.Dispose() { }

                public GameObject Current
                {
                    get { return current; }
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(initial);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return (IEnumerator)GetEnumerator();
            }

            IEnumerator<GameObject> IEnumerable<GameObject>.GetEnumerator()
            {
                return (IEnumerator<GameObject>)GetEnumerator();
            }

            public ParentEnumerator(GameObject initial)
            {
                this.initial = initial;
            }
        }

        /** 
         * Enumerable class to iterate over parents.  Defined to allow us to use Linq
         * and predicates.  Allows this fun operation to find a sun closest to us under 
         * the tree
         * 
         *     Utility.ReferenceBodyEnumerator e = new Utility.ReferenceBodyEnumerator(FlightGlobals.currentMainBody);
         *     CelestialBody sun = e.First(p => p.GetComponentsInChildren(typeof (ScaledSun), true).Length > 0);
         *
         * See examples: http://msdn.microsoft.com/en-us/library/78dfe2yb(v=vs.110).aspx
         **/
        public class ReferenceBodyEnumerator : IEnumerable<CelestialBody>
        {
            // The game object who and whose parents are going to be enumerated
            private CelestialBody initial;

            // Enumerator class
            public class Enumerator : IEnumerator<CelestialBody>
            {
                public CelestialBody original;
                public CelestialBody current;

                public Enumerator(CelestialBody initial)
                {
                    this.original = initial;
                    this.current = this.original;
                }

                public bool MoveNext()
                {
                    if (current.referenceBody != null)
                    {
                        current = current.referenceBody;
                        return true;
                    }

                    return false;
                }

                public void Reset()
                {
                    current = original;
                }

                void IDisposable.Dispose() { }

                public CelestialBody Current
                {
                    get { return current; }
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(initial);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return (IEnumerator)GetEnumerator();
            }

            IEnumerator<CelestialBody> IEnumerable<CelestialBody>.GetEnumerator()
            {
                return (IEnumerator<CelestialBody>)GetEnumerator();
            }

            public ReferenceBodyEnumerator(CelestialBody initial)
            {
                this.initial = initial;
            }
        }

        public static void Log(object s)
        {
#if DEBUG
            Logger.Active.Log(s);
#endif
        }
    }
}

