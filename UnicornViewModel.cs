using CefSharp.DevTools.IndexedDB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rhino;
using Rhino.Commands;
using Rhino.Compute;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.Render;
using Rhino.Runtime;
using Servers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mesh = Rhino.Geometry.Mesh;

namespace Unicorn.ViewModels
{
    /// <summary>
    /// Used internally for RestHopperObject serialization
    /// </summary>
    class GeometryResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
    {
        static JsonSerializerSettings _settings;
        public static JsonSerializerSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = new JsonSerializerSettings { ContractResolver = new GeometryResolver() };
                    // return V6 ON_Objects for now
                    Rhino.FileIO.SerializationOptions options = new Rhino.FileIO.SerializationOptions();
                    options.RhinoVersion = 6;
                    options.WriteUserData = true;
                    _settings.Context = new System.Runtime.Serialization.StreamingContext(System.Runtime.Serialization.StreamingContextStates.All, options);
                    //_settings.Converters.Add(new ArchivableDictionaryResolver());
                }
                return _settings;
            }
        }

        protected override Newtonsoft.Json.Serialization.JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
        {
            Newtonsoft.Json.Serialization.JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (property.DeclaringType == typeof(Rhino.Geometry.Circle))
            {
                property.ShouldSerialize = _ =>
                {
                    return property.PropertyName != "IsValid" && property.PropertyName != "BoundingBox" && property.PropertyName != "Diameter" && property.PropertyName != "Circumference";
                };

            }
            if (property.DeclaringType == typeof(Rhino.Geometry.Plane))
            {
                property.ShouldSerialize = _ =>
                {
                    return property.PropertyName != "IsValid" && property.PropertyName != "OriginX" && property.PropertyName != "OriginY" && property.PropertyName != "OriginZ";
                };
            }

            if (property.DeclaringType == typeof(Rhino.Geometry.Point3f) ||
                property.DeclaringType == typeof(Rhino.Geometry.Point2f) ||
                property.DeclaringType == typeof(Rhino.Geometry.Vector2f) ||
                property.DeclaringType == typeof(Rhino.Geometry.Vector3f))
            {
                property.ShouldSerialize = _ =>
                {
                    return property.PropertyName == "X" || property.PropertyName == "Y" || property.PropertyName == "Z";
                };
            }

            if (property.DeclaringType == typeof(Rhino.Geometry.Line))
            {
                property.ShouldSerialize = _ =>
                {
                    return property.PropertyName == "From" || property.PropertyName == "To";
                };
            }

            if (property.DeclaringType == typeof(Rhino.Geometry.MeshFace))
            {
                property.ShouldSerialize = _ =>
                {
                    return property.PropertyName != "IsTriangle" && property.PropertyName != "IsQuad";
                };
            }
            return property;
        }
    }
    class Alternative
    {
        static Random rand = new Random();
        internal List<Guid> currentObjectsGuids = new List<Guid>();

        [JsonProperty]
        internal Dictionary<string, object> data = new Dictionary<string, object>();

        [JsonProperty]
        internal Dictionary<string, List<double>> metrics = new Dictionary<string, List<double>>();

        [JsonProperty]
        internal Curve zone;

        [JsonProperty]
        internal List<GeometryBase> interiorWalls = new List<GeometryBase>();

        [JsonProperty]
        internal string imageBytes;

        [JsonProperty]
        internal string timestamp;

        internal Guid zoneGuid;
        internal List<Guid> interiorWallsGuids = new List<Guid>();

        internal List<Guid> wallsGuid = new List<Guid>();
        internal List<Brep> walls = new List<Brep>();

        internal Mesh currentlySelectedWall;
        internal List<Brep> currentlySelectedWalls = new List<Brep>();
        internal List<int> wallDirections = new List<int>();
        internal List<Guid> daylightMeshesIds = new List<Guid>();
        internal string weatherLocation;
        internal int currentDaylightMeshIndex = 0;

        internal Alternative()
        {
            data["num"] = DateTime.Now.ToString().GetHashCode().ToString("x");

            data["WWR_per_wall"] = new double[] { 0.00, 0.00, 0.00, 0.00 };

            data["verticalShadings_multiplier"] = new int[] { 0, 0, 0, 0 };
            data["verticalShadings_depth"] = new double[] { 0.65, 0.65, 0.65, 0.65 };

            data["horizontalShadings_multiplier"] = new int[] { 0, 0, 0, 0 };
            data["horizontalShadings_depth"] = new double[] { 0.65, 0.65, 0.65, 0.65 };

            data["overhangs_offset"] = new double[] { 0, 0, 0, 0 };
            data["overhangs_depth"] = new double[] { 0.65, 0.65, 0.65, 0.65 };

            data["overhangsOnOff"] = new int[] { 1, 1, 1, 1 };
            data["verticalFnOnOff"] = new int[] { 1, 1, 1, 1 };
            data["horizontalFnOnOff"] = new int[] { 1, 1, 1, 1 };

            data["terrain"] = 0;
            data["heat_capacity"] = 0;

            data["heat_source"] = 0;
            data["hot_water_source"] = 0;
            data["building_height"] = 3.2;
            data["building_type"] = 0;

            data["height"] = 3.2;
            data["floor_to_floor"] = 3.2;
            data["footprint_offset"] = 0;

            data["ceiling_reflectance"] = 0.7;
            data["floor_reflectance"] = 0.2;
            data["wall_reflectance"] = 0.4;
            data["glazing_transparency"] = 0.3;

            data["grid_size"] = 2;

            data["floor_num"] = 0;
            data["program"] = "2019::PrimarySchool::Classroom";

            data["wall_r_val"] = 30;
            data["roof_r_val"] = 20;
            data["ground_r_val"] = 20;
            data["win_u_val"] = 2.2;
            data["win_shgc"] = 0.3;

            data["enable_energy"] = true;
            data["enable_daylight"] = true;
        }
    }


    internal class UnicornViewModel : Rhino.UI.ViewModel
    {

        private string ImageToBase64String(Bitmap bitmap)
        {

            MemoryStream ms = new MemoryStream();

            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

            byte[] arr = new byte[ms.Length];

            ms.Position = 0;
            ms.Read(arr, 0, (int)ms.Length);
            ms.Close();

            string strBase64 = Convert.ToBase64String(arr);

            return strBase64;
        }

        Alternative currentAlternative;
        internal List<GeometryBase> context = new List<GeometryBase>();
        internal List<Guid> contextGuids = new List<Guid>();

        DateTime lastRequestDate;

        internal Task SaveCurrentAlt(string name, bool isAutomatedSave)
        {
            return SaveCustomViewAlt(currentAlternative.currentObjectsGuids, currentAlternative.zone, name, isAutomatedSave);
        }

        internal Task SaveObjectsToImage(List<Guid> objectGuids, Alternative alt, double height, string name, string subfolder, bool moveFar = true)
        {
            return Task.Run(() =>
            {
                try
                {
                    RhinoApp.InvokeAndWait(delegate
                    {
                        RhinoDoc doc = RhinoDoc.ActiveDoc;
                        IEnumerable<RhinoObject> objects = objectGuids.Select(guid => doc.Objects.Find(guid));

                        List<RhinoObject> objectsLst = objects.Where(obj => obj != null).ToList();

                        if (!objectsLst.Any())
                        {
                            RhinoApp.WriteLine("No objects found.");
                            return;
                        }

                        // Create and set up a new view
                        RhinoView view = doc.Views.Add("CustomView", DefinedViewportProjection.Perspective, new Rectangle(0, 0, 800, 600), true);
                        if (view == null)
                        {
                            RhinoApp.WriteLine("Failed to create a new view.");
                            return;
                        }
                        List<RhinoObject> allObjs = doc.Objects.Where(obj => obj != null).ToList();

                        //Hide everything else so we could capture only our objects
                        allObjs.ForEach(obj =>
                        {
                            Rhino.RhinoDoc.ActiveDoc.Objects.Hide(obj.Id, true);
                        });


                        objectsLst.ForEach(obj =>
                        {
                            Rhino.RhinoDoc.ActiveDoc.Objects.Show(obj.Id, true);
                            ObjectAttributes attributes = obj.Attributes;

                            attributes.ViewportId = view.ActiveViewportID;

                            doc.Objects.ModifyAttributes(obj.Id, attributes, true);
                        });


                        // Calculate the bounding box on the 
                        BoundingBox bbox = alt.zone.GetBoundingBox(false);

                        Clip(alt.zone, view.ActiveViewportID, true, height);

                        const double pad = 0.02;    // A little padding...
                        double dx = (bbox.Max.X - bbox.Min.X) * pad;
                        double dy = (bbox.Max.Y - bbox.Min.Y) * pad;
                        double dz = (bbox.Max.Z - bbox.Min.Z) * pad;
                        bbox.Inflate(dx, dy, dz);

                        view.ActiveViewport.ZoomBoundingBox(bbox);
                        view.Redraw();
                        doc.Views.Redraw();

                        DisplayModeDescription displaymode = DisplayModeDescription.FindByName("Arctic");

                        // Capture the view to a bitmap
                        Bitmap bm = view.CaptureToBitmap(new Size(view.ActiveViewport.Size.Width, view.ActiveViewport.Size.Height), displaymode);

                        string imageBytes = ImageToBase64String(bm);



                        string savingFolder = UnicornPlugin.Instance.GetDataFolderPath() + subfolder;
                        if (!Directory.Exists(savingFolder))
                        {
                            Directory.CreateDirectory(savingFolder);
                        }

                        string altFileName = name;

                        string combinedPath = Path.Combine(savingFolder, altFileName);

                        string json = JsonConvert.SerializeObject(alt, GeometryResolver.Settings);
                        File.WriteAllText(combinedPath + ".json", json);
                        bm.Save(combinedPath + ".png");

                        allObjs.ForEach(obj =>
                        {
                            Rhino.RhinoDoc.ActiveDoc.Objects.Show(obj.Id, true);
                        });

                        // Close the custom view after capture
                        view.Close();
                    });
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Error in SaveCustomViewAlt: {ex.Message}");
                }
            });
        }


        internal Task SaveCustomViewAlt(List<Guid> objectGuids, Curve zone, string name, bool isAutomatedSave = false)
        {
            return Task.Run(() =>
            {
                try
                {
                    RhinoApp.InvokeOnUiThread((Action)delegate
                    {
                        RhinoDoc doc = RhinoDoc.ActiveDoc;
                        List<RhinoObject> objects = objectGuids.Select(guid => doc.Objects.Find(guid)).Where(obj => obj != null).ToList();

                        if (!objects.Any())
                        {
                            RhinoApp.WriteLine("No objects found.");
                            return;
                        }

                        // Calculate the bounding box on the 
                        BoundingBox bbox = zone.GetBoundingBox(false);

                        // Create and set up a new view
                        RhinoView view = doc.Views.Add("CustomView", DefinedViewportProjection.Perspective, new Rectangle(0, 0, 800, 600), true);
                        if (view == null)
                        {
                            RhinoApp.WriteLine("Failed to create a new view.");
                            return;
                        }
                        ClipInViewport(true, view.ActiveViewportID);

                        double pad = 0.02;    // A little padding...
                        double dx = (bbox.Max.X - bbox.Min.X) * pad;
                        double dy = (bbox.Max.Y - bbox.Min.Y) * pad;
                        double dz = (bbox.Max.Z - bbox.Min.Z) * pad;
                        bbox.Inflate(dx, dy, dz);

                        // Zoom to the bounding box of selected objects

                        bool succeeded = view.ActiveViewport.ZoomBoundingBox(bbox);
                        view.Redraw();
                        doc.Views.Redraw();

                        DisplayModeDescription displaymode = DisplayModeDescription.FindByName("Arctic");


                        // Capture the view to a bitmap
                        Bitmap bm = view.CaptureToBitmap(new Size(view.ActiveViewport.Size.Width, view.ActiveViewport.Size.Height), displaymode);

                        currentAlternative.imageBytes = ImageToBase64String(bm);
                        if (name == "current")
                        {
                            currentAlternative.data["isCurrent"] = true;
                            currentAlternative.data["num"] = RandomHexString(5);

                        }
                        else
                        {
                            currentAlternative.data["isCurrent"] = false;
                            currentAlternative.data["num"] = RandomHexString(5);
                        }
                        currentAlternative.timestamp = DateTime.Now.ToString();

                        string json = JsonConvert.SerializeObject(currentAlternative, GeometryResolver.Settings);

                        string savingFolder = UnicornPlugin.Instance.GetDataFolderPath();
                        if (!Directory.Exists(savingFolder))
                        {
                            Directory.CreateDirectory(savingFolder);
                        }

                        string altFileName = name;
                        string combined = Path.Combine(savingFolder, altFileName + ".json");
                        File.WriteAllText(combined, json);

                        // Close the custom view after capture
                        view.Close();
                    });
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Error in SaveCustomViewAlt: {ex.Message}");
                }
            });
        }

        private static string RandomHexString(int len)
        {
            Random rdm = new Random();

            var hex = "0123456789ABCDEF";
            var output = "";
            for (var i = 0; i < len; ++i)
            {
                output += hex[(int)Math.Floor(rdm.NextDouble() * hex.Length)];
            }
            return output;
        }



        async internal void LoadAltAsCurrent(string name, string subfolder = "")
        {

            string fileToLoad = Path.Combine(Path.Combine(UnicornPlugin.Instance.GetDataFolderPath(), subfolder), name + ".json");
            if (File.Exists(fileToLoad))
            {
                Alternative alternative = LoadAlt(fileToLoad);

                // Clean up geometery of old alternative
                currentAlternative.currentObjectsGuids.ForEach(id => RhinoDoc.ActiveDoc.Objects.Delete(id, true));
                currentAlternative.currentObjectsGuids.Clear();
                RhinoDoc.ActiveDoc.Objects.Delete(currentAlternative.zoneGuid, true);


                currentAlternative = alternative;

                // update the guids to point to  zone and interior walls of the loaded alternative
                currentAlternative.zoneGuid = RhinoDoc.ActiveDoc.Objects.Add(alternative.zone);
                currentAlternative.interiorWallsGuids = new List<Guid>();
                alternative.interiorWalls.ForEach(iw =>
                {
                    currentAlternative.interiorWallsGuids.Add(RhinoDoc.ActiveDoc.Objects.Add(iw));
                });

                Curve geometry = alternative.zone;
                //To set an initial wwrPerWall value
                double[] wwrPerWall = ((Newtonsoft.Json.Linq.JArray)alternative.data["WWR_per_wall"]).ToObject<double[]>();

                int[] vShadingCountsPerWall = ((Newtonsoft.Json.Linq.JArray)alternative.data["verticalShadings_multiplier"]).ToObject<int[]>();
                double[] vShadingDepthsPerWall = ((Newtonsoft.Json.Linq.JArray)alternative.data["verticalShadings_depth"]).ToObject<double[]>();
                int[] hShadingCountsPerWall = ((Newtonsoft.Json.Linq.JArray)alternative.data["horizontalShadings_multiplier"]).ToObject<int[]>();
                double[] hShadingDepthsPerWall = ((Newtonsoft.Json.Linq.JArray)alternative.data["horizontalShadings_depth"]).ToObject<double[]>();
                double[] overhangsOffsetPerWall = ((Newtonsoft.Json.Linq.JArray)alternative.data["overhangs_offset"]).ToObject<double[]>();
                double[] overhangsDepthPerWall = ((Newtonsoft.Json.Linq.JArray)alternative.data["overhangs_depth"]).ToObject<double[]>();


                int[] vShadingOnOffPerWall = ((Newtonsoft.Json.Linq.JArray)alternative.data["verticalFnOnOff"]).ToObject<int[]>();
                int[] hShadingOnOffPerWall = ((Newtonsoft.Json.Linq.JArray)alternative.data["horizontalFnOnOff"]).ToObject<int[]>();
                int[] overhangsOnOffPerWall = ((Newtonsoft.Json.Linq.JArray)alternative.data["overhangsOnOff"]).ToObject<int[]>();

                UnicornPlugin.UIInterop.SetNumWalls(wwrPerWall.Length);
                UnicornPlugin.UIInterop.setWWRShadingPerWall(wwrPerWall, vShadingCountsPerWall, vShadingDepthsPerWall, hShadingCountsPerWall, hShadingDepthsPerWall, overhangsOffsetPerWall, overhangsDepthPerWall, vShadingOnOffPerWall, hShadingOnOffPerWall, overhangsOnOffPerWall);

                UpdateData(currentAlternative, "WWR_per_wall", wwrPerWall, true);
                UpdateData(currentAlternative, "interior_walls", alternative.interiorWalls, true);
                await UpdateData(currentAlternative, "zone", geometry);
            }
            else
            {
                //TODO error notification : file not found
            }
        }
        internal Alternative LoadAlt(string filepath)
        {
            string json = File.ReadAllText(filepath);
            Alternative alternative = JsonConvert.DeserializeObject<Alternative>(json, GeometryResolver.Settings);
            return alternative;
        }
        internal void DeleteAlt(string name)
        {
            string fileToDelete = UnicornPlugin.Instance.GetDataFolderPath() + name + ".json";
            if (File.Exists(fileToDelete))
            {
                File.Delete(fileToDelete);
            }
        }

        internal string GetAlts()
        {
            IOrderedEnumerable<string> altFiles = Directory.GetFiles(UnicornPlugin.Instance.GetDataFolderPath(), "*.json").OrderByDescending(d => new FileInfo(d).CreationTime);
            return "[" + String.Join(",", altFiles.Select(fn => File.ReadAllText(fn))) + "]";
        }
        internal string GetCurrentAlt()
        {
            string[] altFiles = Directory.GetFiles(UnicornPlugin.Instance.GetDataFolderPath(), "current.json");
            return String.Join("", altFiles.Select(fn => File.ReadAllText(fn)));
        }


        internal object DeserializeData(string key, string value)
        {
            object res = value;
            if (key == "WWR_per_wall" || key == "verticalShadings_depth" || key == "horizontalShadings_depth" || key == "overhangs_offset" || key == "overhangs_depth")
            {
                try
                {
                    res = JArray.Parse(value).Select(v => (double)v).ToArray<double>();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else if (key == "verticalShadings_multiplier" || key == "horizontalShadings_multiplier" || key == "verticalFnOnOff" || key == "horizontalFnOnOff" || key == "overhangsOnOff")
            {
                try
                {
                    res = JArray.Parse(value).Select(v => (int)v).ToArray<int>();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else //remaining are primitives that don't need casting back
            {
                res = JsonConvert.DeserializeObject(value);
            }
            return res;
        }

        public UnicornViewModel()
        {
            RhinoDoc.ReplaceRhinoObject += OnObjectReplaced;
            RhinoDoc.ModifyObjectAttributes += OnModifyObjectAttributes;
            RhinoDoc.SelectObjects += OnSelectObjects;
            RhinoDoc.DeselectAllObjects += DeselectAllObjects;
            RhinoDoc.DeselectObjects += OnSelectObjects;
            RhinoDoc.EndOpenDocument += InitDoc;
            Rhino.UI.Panels.Show += OnShowPanel;

            UnicornPlugin.ServerLoaded += () =>
            {
                UnicornPlugin.UIInterop.SetServerLoaded();
            };

            ZoneSet += (curve) =>
            {
                SetZone(curve);
            };

            ContextSet += (cntx) =>
            {
                SetContext(cntx);
            };

            InteriorWallsSet += (iws) =>
            {
                SetInteriorWalls(iws);
            };

            InitDataOnView();
        }
        static int glassMaterialIndex = 0;

        private void InitDoc(object sender, DocumentOpenEventArgs e)
        {
            Rhino.RhinoDoc.ActiveDoc.AdjustModelUnitSystem(UnitSystem.Meters, false);

        }

        internal void InitDataOnView()
        {
            currentAlternative = new Alternative();

        }

        internal void SetContext(List<GeometryBase> geometries)
        {
            Task.Run(async () =>
            {
                await UpdateData(currentAlternative, "context", geometries);
                UnicornPlugin.UIInterop.UpdateUIData("isContextSet", true);
            });
        }
        internal  void SetInteriorWalls(List<GeometryBase> geometries)
        {
            Task.Run(async () =>
            {
                await UpdateData(currentAlternative, "interior_walls", geometries);
                UnicornPlugin.UIInterop.UpdateUIData("isInteriorWallsSet", true);
            });
        }



        internal void SetZone(Curve geometry, bool fresh = true)
        {
            Task.Run(async () =>
            {

                //To set an initial wwrPerWall value
                int numSegments = geometry.DuplicateSegments().Count();
                UnicornPlugin.UIInterop.SetNumWalls(numSegments);
                UnicornPlugin.UIInterop.UpdateInputsData(currentAlternative.data);

                double[] wwrPerWall, vShadingDepthsPerWall, hShadingDepthsPerWall, overhangsOffsetPerWall, overhangsDepthsPerWall;
                int[] vShadingCountsPerWall, hShadingCountsPerWall, overhangsOnOff, verticalFnOnOff, horizontalFnOnOff;

                wwrPerWall = (currentAlternative.data["WWR_per_wall"] as double[]);
                vShadingDepthsPerWall = (currentAlternative.data["verticalShadings_depth"] as double[]);
                vShadingCountsPerWall = (currentAlternative.data["verticalShadings_multiplier"] as int[]);
                hShadingCountsPerWall = (currentAlternative.data["horizontalShadings_multiplier"] as int[]);
                hShadingDepthsPerWall = (currentAlternative.data["horizontalShadings_depth"] as double[]);
                overhangsOffsetPerWall = (currentAlternative.data["overhangs_offset"] as double[]);
                overhangsDepthsPerWall = (currentAlternative.data["overhangs_depth"] as double[]);

                overhangsOnOff = (currentAlternative.data["overhangsOnOff"] as int[]);
                verticalFnOnOff = (currentAlternative.data["overhangsOnOff"] as int[]);
                horizontalFnOnOff = (currentAlternative.data["overhangsOnOff"] as int[]);

                if (wwrPerWall == null)
                {
                    Console.WriteLine("errr");
                }

                if (fresh || (!fresh && !(numSegments == wwrPerWall.Length && numSegments == vShadingCountsPerWall.Length && numSegments == vShadingDepthsPerWall.Length && numSegments == hShadingCountsPerWall.Length && numSegments == hShadingDepthsPerWall.Length && numSegments == overhangsOffsetPerWall.Length && numSegments == overhangsDepthsPerWall.Length && numSegments == overhangsOnOff.Length && numSegments == horizontalFnOnOff.Length && numSegments == verticalFnOnOff.Length)))
                {
                    wwrPerWall = Enumerable.Repeat(0.00, numSegments).ToArray();
                    wwrPerWall[0] = 0.5;
                    vShadingCountsPerWall = Enumerable.Repeat<int>(0, numSegments).ToArray();
                    vShadingDepthsPerWall = Enumerable.Repeat<double>(0.65, numSegments).ToArray();
                    hShadingCountsPerWall = Enumerable.Repeat<int>(0, numSegments).ToArray();
                    hShadingDepthsPerWall = Enumerable.Repeat<double>(0.65, numSegments).ToArray();
                    overhangsOffsetPerWall = Enumerable.Repeat<double>(0.0, numSegments).ToArray();
                    overhangsDepthsPerWall = Enumerable.Repeat<double>(0.65, numSegments).ToArray();


                    verticalFnOnOff = Enumerable.Repeat<int>(0, numSegments).ToArray();
                    horizontalFnOnOff = Enumerable.Repeat<int>(0, numSegments).ToArray();
                    overhangsOnOff = Enumerable.Repeat<int>(0, numSegments).ToArray();
                }

                UnicornPlugin.UIInterop.setWWRShadingPerWall(wwrPerWall, vShadingCountsPerWall, vShadingDepthsPerWall, hShadingCountsPerWall, hShadingDepthsPerWall, overhangsOffsetPerWall, overhangsDepthsPerWall, verticalFnOnOff, horizontalFnOnOff, overhangsOnOff);

                UpdateData(currentAlternative, "WWR_per_wall", wwrPerWall, true);

                UpdateData(currentAlternative, "verticalShadings_multiplier", vShadingCountsPerWall, true);
                UpdateData(currentAlternative, "verticalShadings_depth", vShadingDepthsPerWall, true);
                UpdateData(currentAlternative, "horizontalShadings_multiplier", hShadingCountsPerWall, true);
                UpdateData(currentAlternative, "horizontalShadings_depth", hShadingDepthsPerWall, true);
                UpdateData(currentAlternative, "overhangs_offset", overhangsOffsetPerWall, true);
                UpdateData(currentAlternative, "overhangs_depth", overhangsDepthsPerWall, true);

                UpdateData(currentAlternative, "overhangsOnOff", overhangsOnOff, true);
                UpdateData(currentAlternative, "verticalFnOnOff", verticalFnOnOff, true);
                UpdateData(currentAlternative, "horizontalFnOnOff", horizontalFnOnOff, true);


                await UpdateData(currentAlternative, "zone", geometry, false, true, false);
                UnicornPlugin.UIInterop.UpdateUIData("isZoneSet", true);
            });
        }


        public delegate void ZoneSetHandler(Curve curve);

        public static event ZoneSetHandler ZoneSet;

        public delegate void ContextSetHandler(List<GeometryBase> geoms);

        public static event ContextSetHandler ContextSet;

        public delegate void InteriorWallsSetHandler(List<GeometryBase> geoms);

        public static event InteriorWallsSetHandler InteriorWallsSet;

        internal Result SetZone()
        {
            try
            {
                // We run the entire selection process on the UI thread without Task.Run.
                RhinoApp.InvokeOnUiThread((Action)delegate
                {
                    GetObject getter = new GetObject();
                    getter.AcceptNothing(true);
                    getter.GeometryFilter = ObjectType.Curve;
                    getter.GeometryAttributeFilter = GeometryAttributeFilter.ClosedCurve;
                    getter.AcceptEnterWhenDone(true);
                    getter.EnablePreSelect(true, true);
                    getter.EnablePostSelect(true);
                    getter.SetCommandPrompt("Select a footprint (polycurve)");

                    // Run the object selection process
                    GetResult result = getter.Get();

                    if (result == GetResult.Object)
                    {
                        // Selection succeeded
                        Rhino.DocObjects.ObjRef objRef = getter.Object(0);
                        if (objRef != null)
                        {
                            RhinoObject selectedObject = objRef.Object();
                            Curve geometry = selectedObject.Geometry as Curve;

                            if (geometry != null)
                            {
                                currentAlternative.zoneGuid = objRef.ObjectId;
                                ZoneSet(geometry);
                                RhinoApp.WriteLine("A footprint was successfuly selected, processing starts...");
                            }
                        }
                    }
                    else if (result == GetResult.Nothing)
                    {
                        RhinoApp.WriteLine("Selection was canceled.");
                    }
                    else
                    {
                        RhinoApp.WriteLine("Selection failed.");
                    }
                });

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"An error occurred: {ex.Message}");
                return Result.Failure;
            }
        }

        internal Result SetContext()
        {
            try
            {
                // We run the entire selection process on the UI thread without Task.Run.
                RhinoApp.InvokeOnUiThread((Action)delegate
                {
                    GetObject getter = new GetObject();
                    getter.AcceptNothing(true);
                    getter.GeometryFilter = ObjectType.Brep | ObjectType.Mesh;
                    getter.AcceptEnterWhenDone(true);
                    getter.EnablePreSelect(true, true);
                    getter.EnablePostSelect(true);
                    getter.SetCommandPrompt("Select the building's context");

                    // Run the object selection process
                    GetResult result = getter.GetMultiple(0, 999);


                    if (result == GetResult.Object)
                    {
                        // Selection succeeded

                        Rhino.DocObjects.ObjRef[] objsRef = getter.Objects();
                        if (objsRef.Length > 0)
                        {
                            contextGuids = objsRef.Select(oref => oref.ObjectId).ToList();

                            // Access the selected object's geometry
                            List<GeometryBase> geometries = new List<GeometryBase>();
                            foreach (ObjRef oRef in objsRef)
                            {
                                geometries.Add(oRef.Object().Geometry);
                            }

                            ContextSet(geometries);


                        }
                    }
                    else if (result == GetResult.Nothing)
                    {
                        RhinoApp.WriteLine("Selection was canceled.");
                    }
                    else
                    {
                        RhinoApp.WriteLine("Selection failed.");
                    }
                });

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"An error occurred: {ex.Message}");
                return Result.Failure;
            }

        }

        internal Result SetInteriorWalls()
        {
            try
            {
                // We run the entire selection process on the UI thread without Task.Run.
                RhinoApp.InvokeOnUiThread((Action)delegate
                {
                    GetObject getter = new GetObject();
                    getter.AcceptNothing(true);
                    getter.GeometryFilter = ObjectType.Curve;
                    getter.AcceptEnterWhenDone(true);
                    getter.EnablePreSelect(true, true);
                    getter.EnablePostSelect(true);
                    getter.SetCommandPrompt("Select the interior walls(s)");

                    // Run the object selection process
                    GetResult result = getter.GetMultiple(0, 999);


                    if (result == GetResult.Object)
                    {
                        // Selection succeeded

                        Rhino.DocObjects.ObjRef[] objsRef = getter.Objects();
                        if (objsRef.Length > 0)
                        {
                            currentAlternative.interiorWallsGuids = objsRef.Select(oref => oref.ObjectId).ToList();

                            // Access the selected object's geometry
                            List<GeometryBase> geometries = new List<GeometryBase>();
                            foreach (ObjRef oRef in objsRef)
                            {
                                geometries.Add(oRef.Object().Geometry);
                            }

                            InteriorWallsSet(geometries);


                        }
                    }
                    else if (result == GetResult.Nothing)
                    {
                        RhinoApp.WriteLine("Selection was canceled.");
                    }
                    else
                    {
                        RhinoApp.WriteLine("Selection failed.");
                    }
                });

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"An error occurred: {ex.Message}");
                return Result.Failure;
            }

        }
            internal void SetInteriorWalls2()
        {
            // do something
            Rhino.DocObjects.ObjRef[] objsRef = new Rhino.DocObjects.ObjRef[0];
            Result getResult = RhinoGet.GetMultipleObjects("Select the interior walls(s)", true, ObjectType.Curve, out objsRef);

            if (getResult == Result.Success)
            {
                // Access the selected object's geometry
                if (objsRef.Length > 0)
                {
                    currentAlternative.interiorWallsGuids = objsRef.Select(oref => oref.ObjectId).ToList();

                    List<GeometryBase> geometries = new List<GeometryBase>();
                    foreach (ObjRef oRef in objsRef)
                    {
                        geometries.Add(oRef.Object().Geometry);
                    }
                    UnicornPlugin.UIInterop.UpdateUIData("isInteriorWallsSet", true);
                    UpdateData(currentAlternative, "interior_walls", geometries);
                }

            }
        }
        internal void SetWeatherFile()
        {
            Rhino.UI.OpenFileDialog dialog = new Rhino.UI.OpenFileDialog();
            dialog.ShowOpenDialog();

            string f = dialog.FileName;
            IEnumerable<string> lines = File.ReadLines(f);
            string location = "";
            foreach (string line in lines)
            {
                string[] parts = line.Split(',');
                location = parts[1] + ", " + parts[2] + ", " + parts[3];
                break;
            }

            UnicornPlugin.UIInterop.UpdateUIData("isWeatherFileSet", true);
            UnicornPlugin.UIInterop.UpdateUIData("weatherFileLocation", location);

            UpdateData(currentAlternative, "weather_file", f);
        }

        internal async Task<List<GrasshopperDataTree>> UpdateCurrentAlternative(string key, object newValue, bool silent = false)
        {
            return await UpdateData(currentAlternative, key, newValue, silent);
        }

        async private Task<List<GrasshopperDataTree>> UpdateData(Alternative alt, string key, object newValue, bool silent = false, bool loadToRhino = true, bool updateOnlyIfChanged = true)
        {

            UnicornPlugin.UIInterop.Log(key + " " + newValue);

            if (alt.data.ContainsKey(key) && (alt.data[key] != null && alt.data[key].Equals(newValue) && updateOnlyIfChanged))
            {
                return null;
            }
            if (key == "zone")
            {
                alt.zone = (Curve)newValue;
            }
            else if (key == "context")
            {
                
                context = (List<GeometryBase>)newValue;
            }
            else if (key == "interior_walls")
            {
                alt.interiorWalls = (List<GeometryBase>)newValue;
            }
            else
            {
                alt.data[key] = newValue;
            }


            if (!silent)
            {
                return await ComputeFromData(alt, context, loadToRhino);
            }
            else
            {
                return null;
            }

        }


        internal void VisualizeAnalysis(string analysisName)
        {
            // Run the server in a separate thread
            if(!DataLoadServer.isServerRunning)
            {
                Task serverTask = Task.Run(() => DataLoadServer.StartServer());
            }
          
            //load csv data
            string analysisFile = UnicornPlugin.Instance.GetDataFolderPath() + parametricAnalysisFolder + "\\" +analysisName + "\\data.csv";
            var csv = File.ReadAllText(analysisFile);

            VisualizeCorrelationsCSV(csv);

            //call js code to load data into DesignExplorer 
            UnicornPlugin.UIInterop.VisualizeAnalysisData(csv);

        }

        internal void OpenAnalysisFolder()
        {
            string subfolder = parametricAnalysisFolder;
            string analysisFolder = UnicornPlugin.Instance.GetDataFolderPath() + subfolder;
            if (Directory.Exists(analysisFolder))
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = @"c:\windows\explorer.exe";
                psi.Arguments = analysisFolder;
                Process.Start(psi);
            }
        }

        internal string[] GetAnalysisFolders()
        {
            string analysisFolder = UnicornPlugin.Instance.GetDataFolderPath() + parametricAnalysisFolder;
            return Directory.EnumerateDirectories(analysisFolder).Select(x => x.Substring(x.LastIndexOf("\\") + 1)).ToArray();
        }
        async internal Task RunParametricAnalysisEstimate(string sampleJSON)
        {
            Alternative benchmark = currentAlternative;
            List<Dictionary<string, string>> samples = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(sampleJSON);
            List<List<GrasshopperDataTree>> results = new List<List<GrasshopperDataTree>>();
            List<List<double>> allOutputs = new List<List<double>>();
            UnicornPlugin.UIInterop.UpdateParametricAnalysisProgress(0, 1, true);
            Dictionary<string, string> sample = samples[0];
            int numWalls = ((double[])benchmark.data["WWR_per_wall"]).Length;

            foreach (KeyValuePair<string, string> pair in sample)
            {
                object val = pair.Value;
                string key = pair.Key;

                // If any of these params is being analyzed, apply the same value for all walls
                if (key.Contains("WWR_per_wall"))
                {
                    val = Enumerable.Repeat(double.Parse(pair.Value), numWalls).ToArray();
                }
                else if (key == "verticalShadings_depth" || key == "horizontalShadings_depth" || key == "overhangs_offset" || key == "overhangs_depth")
                {
                    val = Enumerable.Repeat(double.Parse(pair.Value), numWalls).ToArray();
                }
                else if (key == "verticalShadings_multiplier" || key == "horizontalShadings_multiplier")
                {
                    val = Enumerable.Repeat(int.Parse(pair.Value), numWalls).ToArray();
                }

                UpdateData(benchmark, pair.Key, val, true, false, false);
            }

            List<GrasshopperDataTree> res = await UpdateData(benchmark, "ready", true, false, false, false);

            UnicornPlugin.UIInterop.UpdateParametricAnalysisProgress(100, 0, true);
        }

        internal void Clip(bool enable)
        {
            RhinoDoc doc = RhinoDoc.ActiveDoc;
            RhinoView view = doc.Views.ActiveView;
            Guid viewportGuid = (view != null ? view : doc.Views.GetViewList(true, false)[0]).ActiveViewportID;
            ClipInViewport(enable, viewportGuid);
        }

        internal void ClipInViewport(bool enable, Guid viewportGuid)
        {
            if (currentAlternative != null && currentAlternative.zone != null)
            {
                RhinoDoc doc = RhinoDoc.ActiveDoc;
                Curve curve = currentAlternative.zone;
                if (currentAlternative.data["floor_to_floor"] is double)
                {
                    double height = (double)currentAlternative.data["floor_to_floor"] - 0.25;
                    Clip(curve, viewportGuid, enable, height);
                }
            }

        }

        internal void Clip(Curve curve, Guid viewportGuid, bool enable, double height = 1)
        {
            bool flip = true;

            RhinoDoc doc = RhinoDoc.ActiveDoc;
            foreach (RhinoObject obj in doc.Objects)
            {
                if (obj is ClippingPlaneObject)
                {
                    doc.Objects.Delete(obj.Id, true);
                }
            }

            if (curve != null && enable)
            {
                Plane xyPlane = Plane.WorldXY;
                Point3d center = curve.GetBoundingBox(xyPlane).Center;
                var newCenter = center + Rhino.Geometry.Vector3d.ZAxis * height;
                xyPlane.Origin = newCenter;

                Plane clippingPlane = xyPlane;


                if (flip)
                    clippingPlane.Flip();

                Guid id = doc.Objects.AddClippingPlane(clippingPlane, 0.5, 0.5, viewportGuid);
                doc.Views.Redraw();
            }
        }

        Alternative CloneAlt(Alternative alt)
        {
            string json = JsonConvert.SerializeObject(alt, GeometryResolver.Settings);
            Alternative newAlt = JsonConvert.DeserializeObject<Alternative>(json, GeometryResolver.Settings);
            // update the guids to point to  zone and interior walls of the loaded alternative

            newAlt.data["WWR_per_wall"] = ((JArray)newAlt.data["WWR_per_wall"]).ToObject<double[]>();

            newAlt.data["verticalShadings_multiplier"] = ((JArray)newAlt.data["verticalShadings_multiplier"]).ToObject<int[]>();
            newAlt.data["verticalShadings_depth"] = ((JArray)newAlt.data["verticalShadings_depth"]).ToObject<double[]>();
            newAlt.data["horizontalShadings_multiplier"] = ((JArray)newAlt.data["horizontalShadings_multiplier"]).ToObject<int[]>();
            newAlt.data["horizontalShadings_depth"] = ((JArray)newAlt.data["horizontalShadings_depth"]).ToObject<double[]>();
            newAlt.data["overhangs_offset"] = ((JArray)newAlt.data["overhangs_offset"]).ToObject<double[]>();
            newAlt.data["overhangs_depth"] = ((JArray)newAlt.data["overhangs_depth"]).ToObject<double[]>();


            return newAlt;
        }
        static string parametricAnalysisFolder = "analysis";

        async internal Task RunParametricAnalysis(string samplesJSON, string focusDictStr)
        {

            Dictionary<string, int> focusDict = JsonConvert.DeserializeObject<Dictionary<string, int>>(focusDictStr);

            List<Dictionary<string, string>> samples = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(samplesJSON);
            List<List<GrasshopperDataTree>> results = new List<List<GrasshopperDataTree>>();
            List<List<double>> allOutputs = new List<List<double>>();
            UnicornPlugin.UIInterop.UpdateParametricAnalysisProgress(0, samples.Count, false);

            List<string> altNames = new List<string>();
            string analysisSubfolder = parametricAnalysisFolder + "/"  + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day + "_" + DateTime.Now.Hour + "." + DateTime.Now.Minute;

            string parentAnalysisFolder = UnicornPlugin.Instance.GetDataFolderPath() + parametricAnalysisFolder;
            if (!Directory.Exists(parentAnalysisFolder))
            {
                Directory.CreateDirectory(parentAnalysisFolder);
            }

            int num = 0;
            foreach (Dictionary<string, string> alt in samples)
            {
                Alternative benchmark = CloneAlt(currentAlternative);
                // Setting WWRs array to zeros initially
                int numWalls = ((double[])benchmark.data["WWR_per_wall"]).Length;
                double[] initWWR = Enumerable.Repeat(0.00, numWalls).ToArray();
                string altName = "";
                int paramNum = 0;

                //TODO for per-wall params apply to all walls
                foreach (KeyValuePair<string, string> pair in alt)
                {

                    object val = pair.Value;
                    string key = pair.Key;

                    altName += paramNum + "_" + Math.Round(double.Parse(pair.Value), 1) + ((paramNum < alt.Keys.Count - 1) ? "_" : "");

                    // If any of these params is being analyzed, apply the same value for all walls
                    if (key.Contains("WWR_per_wall"))
                    {
                        val = Enumerable.Repeat(double.Parse(pair.Value), numWalls).ToArray();

                        /*
                        var directionIndex = (key.Contains("(S)") ? 0 : (key.Contains("(E)") ? 1 : (key.Contains("(N)") ? 2 : (key.Contains("(W)") ? 3 : -1))));
                        if(directionIndex >= 0)
                        {
                            for(var i = 0; i < initWWR.Length; i++)
                            {
                                if (benchmark.wallDirections[i] == directionIndex)
                                {
                                    initWWR[i] = double.Parse(pair.Value);
                                }
                            }
                        }
                        UpdateData(benchmark, "WWR_per_wall", initWWR, true, false, false);
                        continue;
                        */
                    }
                    else if (key == "verticalShadings_depth" || key == "horizontalShadings_depth")
                    {
                        val = Enumerable.Repeat(double.Parse(pair.Value), numWalls).ToArray();
                    }

                    else if (key == "overhangs_offset" || key == "overhangs_depth")
                    {
                        val = Enumerable.Repeat(double.Parse(pair.Value), numWalls).ToArray();
                        int[] enableOverhangs = Enumerable.Repeat(1, numWalls).ToArray();
                        UpdateData(benchmark, "overhangsOnOff", enableOverhangs, true, false, false);
                    }
                    else if (key == "verticalShadings_multiplier" || key == "horizontalShadings_multiplier")
                    {
                        val = Enumerable.Repeat(int.Parse(pair.Value), numWalls).ToArray();
                        int[] enableShading = Enumerable.Repeat(1, numWalls).ToArray();

                        if (key == "verticalShadings_multiplier")
                        {
                            UpdateData(benchmark, "verticalFnOnOff", enableShading, true, false, false);
                        }
                        else if (key == "horizontalShadings_multiplier")
                        {
                            UpdateData(benchmark, "horizontalFnOnOff", enableShading, true, false, false);
                        }
                    }


                    UpdateData(benchmark, pair.Key, val, true, false, false);


                    paramNum++;
                }
                altNames.Add(altName);

                List<GrasshopperDataTree> res = await UpdateData(benchmark, "ready", true, false, false, false);

                if (res != null)
                {
                    results.Add(res);
                    List<double> outputs = getOutputsFromComputeResults(res);
                    allOutputs.Add(outputs);

                    bool takeSreenshot = true;
                    if (takeSreenshot)
                    {
                        RhinoDoc doc = RhinoDoc.ActiveDoc;

                        List<Guid> guids = CollectResults(res, ref benchmark, false);

                        SwitchDaylightMesh(0, benchmark).Wait();

                        List<Guid> visibleGuids = guids.Select(id => doc.Objects.Find(id)).Where(obj => obj != null).Where(obj => !obj.IsHidden).Select(obj => obj.Id).ToList();

                        double height = benchmark.data["floor_to_floor"] is double ? (double)benchmark.data["floor_to_floor"] : 3.2;

                        SaveObjectsToImage(visibleGuids, benchmark, height, altName, analysisSubfolder, true).Wait();


                        guids.ForEach(id => RhinoDoc.ActiveDoc.Objects.Delete(id, true));
                        guids.Clear();
                        doc.Views.Redraw();

                    }
                }


                num++;
                int progressPerc = (int)(((num * 1.0) / samples.Count) * 100);

                UnicornPlugin.UIInterop.UpdateParametricAnalysisProgress(progressPerc, samples.Count - num, false);
            }

            Dictionary<string, string>.KeyCollection paramNames = samples[0].Keys;

            if (allOutputs.Count == 0)
            {
                return;
            }
            var csvv = "WWR_per_wall,floor_to_floor,out:sDA,out:ASE,out:aUDI,out:MI,out:Heating,out:Cooling,img \n 0.3,2.4,20,22,26,714,0,0,http://localhost:3000/analysis/0.49_3-11-2024/WWR_per_wall_0.3_floor_to_floor_2.4.png 0.3,5,36,61,55,1149,0,0,http://localhost:3000/analysis/0.49_3-11-2024/WWR_per_wall_0.3_floor_to_floor_5.png\n 0.7,2.4,31,39,41,1541,0,0,http://localhost:3000/analysis/0.49_3-11-2024/WWR_per_wall_0.7_floor_to_floor_2.4.png\n 0.7,5,96,97,64,3066,0,0,http://localhost:3000/analysis/0.49_3-11-2024/WWR_per_wall_0.7_floor_to_floor_5.png\n";

            //-------------Writing to CSV -------
            string csvHeader = String.Join(",", paramNames.ToList()) + ",out:sDA,out:ASE,out:aUDI,out:MI,out:Heating,out:Cooling,img\n";
            string csvBody = "";


            for (int sampleNum = 0; sampleNum < samples.Count; sampleNum++)
            {
                List<double> allValues = new List<double>();

                Dictionary<string, string> s = samples[sampleNum];
                //Adding all the inputs
                foreach (string vs in s.Values)
                {
                    double v = double.Parse(vs);
                    allValues.Add(v);
                }
                //Adding all the outputs
                allValues.AddRange(allOutputs[sampleNum].Take(6));
                var imgURL = "http://localhost:3000/" + analysisSubfolder + "/" + (altNames[sampleNum] + ".png");
                csvBody += string.Join(",", allValues) + "," + imgURL + "\n";
            }

            string csv = csvHeader + csvBody;
            string savingFolder = Path.Combine(UnicornPlugin.Instance.GetDataFolderPath(), analysisSubfolder);
            if (!Directory.Exists(savingFolder))
            {
                Directory.CreateDirectory(savingFolder);
            }
            string altFileName = "data";
            string combined = Path.Combine(savingFolder, altFileName + ".csv");

            File.WriteAllText(combined, csv);
            //------------- calculating and sending correlation results to front-end -------

            
            ComputeThenVisualizeCorrelations(paramNames.ToList(), samples, allOutputs, focusDict);

        }
        public void VisualizeCorrelationsCSV(string csvData)
        {
            // Split the CSV string into lines
            //var lines = csvData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            var lines = Regex.Split(csvData, "\r\n|\n")
                             .Where(line => !string.IsNullOrWhiteSpace(line))
                             .ToArray();


            // Extract the headers and rows
            var headers = lines[0].Split(',').Select(h => h.Trim()).ToList();
            var dataRows = lines.Skip(1).Select(line => line.Split(',').Select(value => value.Trim()).ToList()).ToList();

            // Separate parameter names and output names
            
            int paramCount = headers.IndexOf(headers.First(h => h.Contains("out:")));  // Assumes "output1" marks the start of output columns
            var paramNames = headers.Take(paramCount).ToList();
            var outputNames = headers.Skip(paramCount).ToList();

            // Create samples and allOutputs lists
            var samples = new List<Dictionary<string, string>>();
            var allOutputs = new List<List<double>>();

            // Process each data row
            foreach (var row in dataRows)
            {
                var sample = new Dictionary<string, string>();
                for (int i = 0; i < paramCount; i++)
                {
                    sample[paramNames[i]] = row[i];
                }
                samples.Add(sample);

                List<double> outputs = new List<double>();
                for (int i = 0; i < outputNames.Count; i++)
                {
                    var output = row[i + paramCount];
                    if (outputNames[i].Contains("out:"))
                    {
                        outputs.Add(double.Parse(output));
                    }
                    
                }
                allOutputs.Add(outputs);

                // Add output values for this row
                //var outputs = row.Skip(paramCount).Select(double.Parse).ToList();
               
            }

            // Call the function with the prepared inputs
            ComputeThenVisualizeCorrelations(paramNames, samples, allOutputs, null);
        }

        private void ComputeThenVisualizeCorrelations(List<string> paramNames, List<Dictionary<string, string>> samples, List<List<double>> allOutputs, Dictionary<string, int> focusDict)
        {
            //------------- calculating and sending correlation results to front-end -------
            Dictionary<string, Tuple<List<double>, List<double>, List<double>>> models = new Dictionary<string, Tuple<List<double>, List<double>, List<double>>>();
            foreach (string k in paramNames)
            {

                List<double> values = new List<double>();
                foreach (Dictionary<string, string> d in samples)
                {
                    values.Add(double.Parse(d[k]));
                }

                List<double> slopes = new List<double>();
                List<double> intercepts = new List<double>();
                List<double> correlations = new List<double>();
                // TODO: for each parameter K, if it is not relevant for the specific metric then set it to zero

                for (int i = 0; i < 6; i++) //6 == num of metrics
                {
                    List<double> metrics = allOutputs.Select(o => o[i]).ToList();
                    double rSquared, yIntercept, slope, correlation;
                    correlation = ComputeCorrelation(values, metrics);
                    LinearRegression(values, metrics, out rSquared, out yIntercept, out slope);

                    int focus = 1;
                    if (focusDict != null)
                    {
                        focus = (focusDict[k] == 0) || (focusDict[k] == 1 && i >= 4) || (focusDict[k] == 2 && i < 4) ? 1 : 0;
                    }
                    
                    slopes.Add(slope * focus);
                    intercepts.Add(yIntercept * focus);
                    correlations.Add(correlation * focus);
                };
                models[k] = new Tuple<List<double>, List<double>, List<double>>(slopes, intercepts, correlations);

                UnicornPlugin.UIInterop.UpdateParametricAnalysisModels(k, slopes, intercepts, correlations);
            }
        }

        private List<double> getOutputsFromComputeResults(List<GrasshopperDataTree> res)
        {
            List<double> outputs = new List<double>();

            List<GrasshopperObject> da_metrics = res.Find(x => x.ParamName == "RH_OUT:DA_all_metrics").InnerTree.Values.ToList()[0];
            List<GrasshopperObject> energy_metrics = res.Find(x => x.ParamName == "RH_OUT:energy_loads_metric").InnerTree.Values.ToList()[0];


            List<double> parsedMetrics = da_metrics.Union(energy_metrics).Select(o => double.Parse(o.Data)).ToList(); ;


            for (int i = 0; i < parsedMetrics.Count; i++)
            {
                outputs.Add(parsedMetrics[i]);
            }

            return outputs;
        }
        async internal Task<List<GrasshopperDataTree>> ComputeFromData(Alternative alt, List<GeometryBase> context, bool loadToRhino = true)
        {
            if (alt.zone == null)
            {
                return null;
            }

            List<GrasshopperDataTree> result = null;
            if (loadToRhino)
                UnicornPlugin.UIInterop.ShowUILoaderAsync(true);

            ComputeServer.WebAddress = "http://localhost:8081/"; // port 5000 is rhino.compute, 8081 is compute.geometry
                                                                 //ComputeServer.ApiKey = "";

            string definitionName = "ParametricRoom_Latest.gh";
            string definitionPath = "";

            string p = "";
#if !DEBUG
               
                if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    // Get the AppData folder path on Windows
                    string appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
                    string grasshopperLibrariesPath = Path.Combine(appDataPath, "Grasshopper", "Libraries", "MITACS folder");

                    p = grasshopperLibrariesPath;
                }
#else
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            p = Path.Combine(Directory.GetParent(assembly.Location).FullName, "Definitions");
#endif
            definitionPath = p;


            definitionPath = Path.Combine(definitionPath, definitionName);

            List<GrasshopperDataTree> trees = new List<GrasshopperDataTree>();
            List<string> altKeys = alt.data.Keys.ToList();
            for (int i = 0; i < altKeys.Count; i++)
            {
                string key = altKeys[i];
                GrasshopperObject value1 = new GrasshopperObject(alt.data[key]);
                GrasshopperDataTree param1 = new GrasshopperDataTree(key);

                IEnumerable enumerable = (alt.data[key] as IEnumerable);
                if (enumerable != null && !(alt.data[key] is string))
                {
                    List<GrasshopperObject> lst = new List<GrasshopperObject>();
                    foreach (object item in enumerable)
                    {
                        lst.Add(new GrasshopperObject(item));
                    }
                    param1.Add("0", lst);
                }
                else
                {
                    param1.Add("0", new List<GrasshopperObject> { value1 });
                }

                trees.Add(param1);
            }

            {
                GrasshopperDataTree param1 = new GrasshopperDataTree("zone");
                List<GrasshopperObject> lst = new List<GrasshopperObject>() { new GrasshopperObject(alt.zone) };
                param1.Add("0", lst);
                trees.Add(param1);
            }

            if (context != null && context.Count > 0)
            {
                GrasshopperDataTree param1 = new GrasshopperDataTree("context");
                param1.Add("0", context.Select(c => new GrasshopperObject(c)).ToList());
                trees.Add(param1);
            }

            if (alt.interiorWalls != null && alt.interiorWalls.Count > 0)
            {
                GrasshopperDataTree param1 = new GrasshopperDataTree("interior_walls");
                param1.Add("0", alt.interiorWalls.Select(c => new GrasshopperObject(c)).ToList());
                trees.Add(param1);
            }

            if (alt.walls != null && alt.walls.Count > 0)
            {
                GrasshopperDataTree param1 = new GrasshopperDataTree("walls");
                param1.Add("0", alt.walls.Select(w => new GrasshopperObject(w)).ToList());
                trees.Add(param1);
                List<Point3d> c2 = alt.walls.Select(w => w.GetBoundingBox(true).Center).ToList();

            }

            try
            {
                lastRequestDate = DateTime.Now;
                DateTime thisRequestDate = DateTime.Now;

                result = Rhino.Compute.GrasshopperCompute.EvaluateDefinition(definitionPath, trees);
                if (thisRequestDate < lastRequestDate) // Always use the latest request and discard the old ones
                {
                    return null;
                }

                if (loadToRhino)
                {
                    RhinoDoc doc = RhinoDoc.ActiveDoc;

                    alt.currentObjectsGuids.ForEach(id =>
                    {
                        Rhino.DocObjects.ObjRef objRef = new Rhino.DocObjects.ObjRef(doc, id);
                        doc.Objects.Delete(objRef, true, true);
                    });
                    alt.currentObjectsGuids.Clear();


                    Layer resultsLayer = doc.Layers.FindName("UnicornResults");
                    int layerIndex = -1;
                    if (resultsLayer == null)
                    {
                        resultsLayer = new Layer();
                        resultsLayer.Name = "UnicornResults";
                        layerIndex = doc.Layers.Add(resultsLayer);
                    }
                    else
                    {
                        layerIndex = resultsLayer.Index;
                    }

                    List<Guid> addedObjectsguids = CollectResults(result, ref alt, true);

                    //Setting all the objects created from Rhino Compute as non-selectable and non-changable i.e. locked.
                    addedObjectsguids.ForEach(id =>
                    {
                        RhinoObject o = doc.Objects.Find(id);
                        if (o != null && layerIndex >= 0)
                        {
                            ObjectAttributes attributes = o.Attributes;
                            attributes.LayerIndex = layerIndex;
                            attributes.Mode = ObjectMode.Normal;

                            // Commit the changes
                            doc.Objects.ModifyAttributes(o.Id, attributes, true);
                        }

                    });


                    if (resultsLayer != null)
                    {
                        //   resultsLayer.IsLocked = true;
                    }

                    SwitchDaylightMesh(alt.currentDaylightMeshIndex, alt);

                    doc.Layers.Modify(resultsLayer, layerIndex, true);
                    //doc.Views.Redraw();


                    await SaveCurrentAlt("current", true);
                    doc.Views.Redraw();
                    UnicornPlugin.UIInterop.UpdateCurrentAlt();

                    Clip(true);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                UnicornPlugin.UIInterop.ShowUILoaderAsync(false);
                return null;
            }
            if (loadToRhino)
                UnicornPlugin.UIInterop.ShowUILoaderAsync(false);
            return result;
        }

        private List<Guid> CollectResults(List<GrasshopperDataTree> values, ref Alternative alt, bool updateUI)
        {
            List<Guid> addObjectsGuids = new List<Guid>();
            Dictionary<string, List<CommonObject>> objects = new Dictionary<string, List<CommonObject>>();

            Dictionary<string, List<System.Drawing.Color>> daylight_colors = new Dictionary<string, List<System.Drawing.Color>>();
            Dictionary<string, List<double>> metrics = new Dictionary<string, List<double>>();
            Dictionary<string, List<List<double>>> metrics_trees = new Dictionary<string, List<List<double>>>();
            List<int> wallDirections = new List<int>();
            List<Mesh> daylightMeshes = new List<Mesh>();
            Dictionary<Guid, Mesh> windows = new Dictionary<Guid, Mesh>();
            Dictionary<Guid, Brep> walls = new Dictionary<Guid, Brep>();

            RhinoDoc doc = RhinoDoc.ActiveDoc;
            // for each output (RH_OUT:*)...
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].ParamName.Contains("_metrics_tree"))
                {
                    List<List<double>> tmp = new List<List<double>>();

                    foreach (KeyValuePair<string, List<GrasshopperObject>> pair in values[i].InnerTree)
                    {
                        List<double> parsed = pair.Value.Select(o => double.Parse(o.Data)).ToList();
                        tmp.Add(parsed);
                    }
                    metrics_trees[values[i].ParamName] = tmp;
                }

                else if (values[i].ParamName.Contains("wall_facing_directions"))
                {
                    List<int> tmp = new List<int>();
                    // 0 for North, 1 for East, 2 for West, 3 for South
                    foreach (KeyValuePair<string, List<GrasshopperObject>> pair in values[i].InnerTree)
                    {
                        List<int> parsed = pair.Value.Select(o => int.Parse(o.Data)).ToList();
                        wallDirections = parsed;
                        alt.wallDirections = wallDirections;
                    }
                    UnicornPlugin.UIInterop.UpdateUIData("wallDirections", alt.wallDirections);
                }

                // ...iterate through data tree structure...
                int pairNum = 0;
                foreach (KeyValuePair<string, List<GrasshopperObject>> pair in values[i].InnerTree)
                {
                    List<GrasshopperObject> branch = pair.Value;

                    if (values[i].ParamName.Contains("_metric"))
                    {
                        List<double> parsed = branch.Select(o => double.Parse(o.Data)).ToList();
                        metrics[values[i].ParamName] = parsed;
                    }
                    else if (values[i].ParamName.Contains("_view"))
                    {
                        Console.WriteLine("");
                    }
                    else
                    {
                        // ...and for each branch...
                        for (int j = 0; j < branch.Count; j++)
                        {
                            // ...load rhino geometry into doc
                            GrasshopperObject v = branch[j];
                            if (v != null)
                            {
                                if (values[i].ParamName.Contains("all_daylight_colors"))
                                {
                                    string parsed = JsonConvert.DeserializeObject<string>(v.Data);

                                    string colorsName = "all_daylight_colors" + pairNum;
                                    if (!daylight_colors.ContainsKey(colorsName))
                                    {
                                        daylight_colors[colorsName] = new List<System.Drawing.Color>();
                                    }

                                    daylight_colors[colorsName].Add(GetColorFromString(parsed));
                                }
                                else if (values[i].ParamName.Contains("weather_location"))
                                {
                                    alt.weatherLocation = JsonConvert.DeserializeObject<string>(v.Data);
                                    UnicornPlugin.UIInterop.UpdateUIData("weatherLocation", alt.weatherLocation);

                                    Console.WriteLine(alt.weatherLocation);
                                }
                                else
                                {
                                    CommonObject obj = null;

                                    try
                                    {
                                        Dictionary<string, string> parsed = JsonConvert.DeserializeObject<Dictionary<string, string>>(v.Data);
                                        obj = Rhino.FileIO.File3dmObject.FromJSON(parsed);

                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                        continue;
                                    }
                                    if (!objects.ContainsKey(values[i].ParamName))
                                    {
                                        objects[values[i].ParamName] = new List<CommonObject>();
                                    }

                                    objects[values[i].ParamName].Add(obj);

                                    if (values[i].ParamName.Contains("windows_brep"))
                                    {
                                        Brep window = (Brep)obj;

                                        Guid id = doc.Objects.AddBrep(window);

                                        RhinoObject ro = doc.Objects.Find(id);
                                        ro.Attributes.ObjectColor = System.Drawing.Color.LightBlue;
                                        ro.Attributes.ColorSource = ObjectColorSource.ColorFromObject;

                                        Material glassMaterial = new Rhino.DocObjects.Material
                                        {
                                            Name = "Glass",
                                            DiffuseColor = System.Drawing.Color.FromArgb(80, 255, 255, 255), // Transparent white
                                            ReflectionColor = System.Drawing.Color.White,
                                            Transparency = 0.8, // 80% transparent
                                            Reflectivity = 0.6  // Some reflectivity for realistic glass
                                        };

                                        if (RhinoDoc.ActiveDoc != null && !RhinoDoc.ActiveDoc.Materials.Contains(glassMaterial))
                                        {
                                            glassMaterialIndex = RhinoDoc.ActiveDoc.Materials.Add(glassMaterial);
                                        }


                                        ro.Attributes.MaterialIndex = glassMaterialIndex; // Assign the material index.
                                        ro.Attributes.MaterialSource = ObjectMaterialSource.MaterialFromObject; // Use object's material.

                                        ro.CommitChanges();

                                        addObjectsGuids.Add(id);
                                    }
                                    else if (values[i].ParamName.Contains("walls_brep"))
                                    {
                                        Guid id = doc.Objects.AddBrep((Brep)obj);
                                        addObjectsGuids.Add(id);
                                        walls[id] = (Brep)obj;
                                    }
                                    else if (values[i].ParamName.Contains("interior_walls"))
                                    {
                                        Guid id = doc.Objects.AddBrep((Brep)obj);
                                        addObjectsGuids.Add(id);
                                    }
                                    else if (values[i].ParamName.Contains("shading_mesh"))
                                    {
                                        Guid id = doc.Objects.AddMesh((Mesh)obj);
                                        addObjectsGuids.Add(id);
                                    }
                                    else if (values[i].ParamName.Contains("Overhangs_mesh"))
                                    {
                                        Guid id = doc.Objects.AddMesh((Mesh)obj);
                                        addObjectsGuids.Add(id);
                                    }
                                    else if (values[i].ParamName.Contains("daylight_meshes"))
                                    {
                                        daylightMeshes.Add((Mesh)obj);
                                    }


                                }
                            }
                        }
                    }
                    pairNum += 1;
                }
            }

            int num = 0;
            foreach (KeyValuePair<Guid, Brep> w in walls)
            {
                w.Value.SetUserString("ID", w.Key.ToString());
                w.Value.SetUserString("num", num + "");
                num++;
            }

            alt.currentObjectsGuids = addObjectsGuids;
            alt.walls = walls.Values.ToList();
            alt.wallsGuid = walls.Keys.ToList();

            if (updateUI)
            {
                UnicornPlugin.UIInterop.UpdateUIOutputData(metrics);
                UnicornPlugin.UIInterop.UpdateUIOutputDataTrees(metrics_trees);                
                
            }


            List<double> daMetrics = metrics["RH_OUT:DA_all_metrics"];
            List<double> loadsMetrics = metrics["RH_OUT:energy_loads_metric"];

            alt.metrics["sDA"] = new List<double>() { Math.Round(daMetrics[0], 2) };
            alt.metrics["ASE"] = new List<double>() { Math.Round(daMetrics[1]) };
            alt.metrics["UDIa"] = new List<double>() { Math.Round(daMetrics[2]) };
            alt.metrics["MI"] = new List<double>() { Math.Round(daMetrics[3]) };

            alt.metrics["Cooling"] = new List<double>() { Math.Round(loadsMetrics[0], 2) };
            alt.metrics["Heating"] = new List<double>() { Math.Round(loadsMetrics[1], 2) };

            alt.daylightMeshesIds = new List<Guid>();

            if (daylightMeshes != null && daylightMeshes.Count > 0)
            {
                for (int i = 0; i < daylightMeshes.Count; i++)
                {
                    daylightMeshes[i].VertexColors.CreateMonotoneMesh(System.Drawing.Color.White);

                    if (daylight_colors.ContainsKey("all_daylight_colors" + i))
                    {
                        daylightMeshes[i].VertexColors.SetColors(daylight_colors["all_daylight_colors" + i].ToArray());
                    }

                    Guid id = doc.Objects.AddMesh(daylightMeshes[i]);

                    addObjectsGuids.Add(id);

                    alt.daylightMeshesIds.Add(id);
                }

            }



            doc.Views.Redraw();
            if (doc.Objects.Count < 1)
            {
                Console.WriteLine("No rhino objects to load!");
                return new List<Guid>();
            }


            return addObjectsGuids;
        }



        // ------------- Event Listeners ---------------
        private void OnShowPanel(object sender, Rhino.UI.ShowPanelEventArgs e)
        {


            uint sn = e.DocumentSerialNumber;
            // TOOD...
        }

        /// <summary>
        /// Called when an object's attributes have been modified
        /// </summary>
        public void OnModifyObjectAttributes(object sender, Rhino.DocObjects.RhinoModifyObjectAttributesEventArgs e)
        {
            if (currentAlternative == null) return;
            if (e.RhinoObject.Id == currentAlternative.zoneGuid)
            {
                RhinoObject obj = RhinoDoc.ActiveDoc.Objects.Find(currentAlternative.zoneGuid);
                bool flag = !GeometryBase.GeometryEquals(e.RhinoObject.Geometry, obj.Geometry);
                if (flag)
                {
                    SetZone((Curve)e.RhinoObject.Geometry, false);
                }
            }
            else
            {
                int indexOfWall = currentAlternative.interiorWallsGuids.IndexOf(e.RhinoObject.Id);
                if (indexOfWall >= 0)
                {
                    RhinoObject obj = RhinoDoc.ActiveDoc.Objects.Find(e.RhinoObject.Id);
                    bool flag = !GeometryBase.GeometryEquals(e.RhinoObject.Geometry, obj.Geometry);
                    if (flag)
                    {
                        currentAlternative.interiorWalls[indexOfWall] = e.RhinoObject.Geometry;
                        UpdateData(currentAlternative, "interior_walls", currentAlternative.interiorWalls);
                    }
                }
                else
                {
                    int indexOfContextPart = contextGuids.IndexOf(e.RhinoObject.Id);
                    if (indexOfContextPart >= 0)
                    {
                        RhinoObject obj = RhinoDoc.ActiveDoc.Objects.Find(e.RhinoObject.Id);
                        bool flag = !GeometryBase.GeometryEquals(e.RhinoObject.Geometry, obj.Geometry);
                        if (flag)
                        {
                            context[indexOfContextPart] = e.RhinoObject.Geometry;
                            UpdateData(currentAlternative, "context", context);
                        }
                    }
                }
            }
        }


        internal void UnselectAllWalls()
        {
            currentAlternative.currentlySelectedWalls.Clear();
            UnicornPlugin.UIInterop.UpdateSelectedWallsOnUI(new int[] { });
            RhinoDoc.ActiveDoc.Objects.UnselectAll();
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        internal void SelectWallsInRhino(string selectedWallsNum)
        {
            int[] selected = JArray.Parse(selectedWallsNum).Select(v => (int)v).ToArray<int>();

            currentAlternative.currentlySelectedWalls.Clear();

            if (currentAlternative.walls.Count == 0)
                return;

            IEnumerable<Guid> selectedWallsGuids = selected.Select(num =>
            {
                Brep wall = currentAlternative.walls[num];
                Guid wallGuid = new Guid(wall.GetUserString("ID"));
                return wallGuid;
            });

            IEnumerable<Brep> selectedWallsObjs = selected.Select(num =>
            {
                return currentAlternative.walls[num];
            });

            //RhinoDoc.ActiveDoc.Objects.UnselectAll();
            currentAlternative.currentlySelectedWalls.AddRange(selectedWallsObjs);
            RhinoDoc.ActiveDoc.Objects.Select(selectedWallsGuids, true);


            int[] wallsNum = currentAlternative.walls.Select(w => int.Parse(w.GetUserString("num"))).ToArray();
            IEnumerable<Guid> unselectedWallsGuids = wallsNum.Where(n => !selected.Contains(n)).Select(num =>
            {
                Brep wall = currentAlternative.walls[num];
                Guid wallGuid = new Guid(wall.GetUserString("ID"));
                return wallGuid;
            });
            RhinoDoc.ActiveDoc.Objects.Select(unselectedWallsGuids, false);

            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        internal void SelectAllWalls()
        {
            if (currentAlternative != null)
            {
                currentAlternative.currentlySelectedWalls.AddRange(currentAlternative.walls);
                int[] wallsNum = currentAlternative.currentlySelectedWalls.Select(w => int.Parse(w.GetUserString("num"))).ToArray();
                UnicornPlugin.UIInterop.UpdateSelectedWallsOnUI(wallsNum);
                currentAlternative.currentlySelectedWalls.ForEach(sw =>
                {
                    RhinoDoc.ActiveDoc.Objects.Select(new Guid(sw.GetUserString("ID")));
                });
                RhinoDoc.ActiveDoc.Views.Redraw();
            }

        }

        void DeselectAllObjects(object sender, RhinoDeselectAllObjectsEventArgs args)
        {
            if (currentAlternative == null) return;
            currentAlternative.currentlySelectedWalls.Clear();
            UnicornPlugin.UIInterop.UpdateSelectedWallsOnUI(new int[] { });


        }
        void OnSelectObjects(object sender, RhinoObjectSelectionEventArgs args)
        {
            if (currentAlternative == null) return;

            if (args.Selected) // objects were selected
            {
                // do something
                foreach (RhinoObject obj in args.RhinoObjects)
                {
                    Brep wall = GetBrepInList(currentAlternative.walls, obj.Geometry);
                    if (wall != null)
                    {

                        int wallIndex = currentAlternative.currentlySelectedWalls.FindIndex((b) => AreWallsEqual(b, wall));
                        if (wallIndex < 0)
                        {
                            currentAlternative.currentlySelectedWalls.Add(wall);
                        }
                        int[] wallsNum = currentAlternative.currentlySelectedWalls.Select(w => int.Parse(w.GetUserString("num"))).ToArray();

                        UnicornPlugin.UIInterop.UpdateSelectedWallsOnUI(wallsNum);
                    }
                }
            }
            else
            {

                // do something
                foreach (RhinoObject obj in args.RhinoObjects)
                {
                    Brep wall = GetBrepInList(currentAlternative.walls, obj.Geometry);

                    if (wall != null)
                    {
                        int wallIndex = currentAlternative.currentlySelectedWalls.FindIndex((b) => AreWallsEqual(b, wall));
                        if (wallIndex >= 0)
                        {
                            currentAlternative.currentlySelectedWalls.RemoveAt(wallIndex);
                        }
                    }

                }


            }

        }

        private void OnObjectReplaced(object sender, RhinoReplaceObjectEventArgs e)
        {
            if (currentAlternative == null) return;

            if (e.ObjectId == currentAlternative.zoneGuid)
            {
                SetZone((Curve)e.NewRhinoObject.Geometry, false);
            }
            else
            {
                int indexOfWall = currentAlternative.interiorWallsGuids.IndexOf(e.ObjectId);
                if (indexOfWall >= 0)
                {
                    currentAlternative.interiorWalls[indexOfWall] = e.NewRhinoObject.Geometry;
                    UpdateData(currentAlternative, "interior_walls", currentAlternative.interiorWalls);
                }
                else
                {
                    int indexOfContextPart = contextGuids.IndexOf(e.ObjectId);
                    if (indexOfContextPart >= 0)
                    {
                        context[indexOfContextPart] = e.NewRhinoObject.Geometry;
                        UpdateData(currentAlternative, "context", context);
                    }
                }
            }

        }
        private bool ContainsGeom(List<Brep> geoms, GeometryBase target)
        {
            return GetBrepInList(geoms, target) != null;
        }

        private bool AreWallsEqual(GeometryBase wall1, GeometryBase wall2)
        {
            if (wall1.GetUserString("ID") != null && wall2.GetUserString("ID") != null)
            {
                return wall1.GetUserString("ID").Equals(wall2.GetUserString("ID"));
            }
            else
            {
                return GeometryBase.GeometryEquals(wall1, wall2);
            }

        }
        private Brep GetBrepInList(List<Brep> geoms, GeometryBase target)
        {
            int aa = geoms.FindIndex((b) => Object.ReferenceEquals(b, target));
            int bb = geoms.FindIndex((b) => GeometryBase.GeometryEquals(b, target));
            int cc = geoms.FindIndex((b) => GeometryBase.GeometryReferenceEquals(b, target));
            int dd = geoms.FindIndex((b) => b.Equals(target));
            int ee = geoms.FindIndex((b) => b == target);
            int ff = geoms.FindIndex((b) => b.GetUserString("ID").Equals(target.GetUserString("ID")));
            return geoms.Find(g => AreWallsEqual(g, target));
        }
        private Mesh GetMeshInList(List<Mesh> geoms, GeometryBase target)
        {
            int aa = geoms.FindIndex((b) => Object.ReferenceEquals(b, target));
            int bb = geoms.FindIndex((b) => GeometryBase.GeometryEquals(b, target));
            int cc = geoms.FindIndex((b) => GeometryBase.GeometryReferenceEquals(b, target));
            int dd = geoms.FindIndex((b) => b.Equals(target));
            int ee = geoms.FindIndex((b) => b == target);
            int ff = geoms.FindIndex((b) => b.GetUserString("ID").Equals(target.GetUserString("ID")));
            return geoms.Find(g => AreWallsEqual(g, target));
        }
        //  ---------------------------- Utils  ----------------
        public static Rhino.Commands.Result AddMaterial(RhinoDoc doc, RhinoObject obj, string matName, System.Drawing.Color diffuseColor, System.Drawing.Color specularColor)
        {
            // Create a Rhino material with a texture.
            Material rhino_material = new Rhino.DocObjects.Material
            {
                Name = matName,
                DiffuseColor = diffuseColor,
                SpecularColor = specularColor
            };

            // Use the Rhino material to create a Render material.
            RenderMaterial render_material = RenderMaterial.CreateBasicMaterial(rhino_material, doc);
            doc.RenderMaterials.Add(render_material);

            if (obj != null)
            {
                // Assign the render material to the sphere object.
                obj.RenderMaterial = render_material;
                obj.CommitChanges();
            }

            doc.Views.Redraw();

            return Rhino.Commands.Result.Success;
        }
        /// <summary>
        /// Fits a line to a collection of (x,y) points.
        /// </summary>
        /// <param name="xVals">The x-axis values.</param>
        /// <param name="yVals">The y-axis values.</param>
        /// <param name="rSquared">The r^2 value of the line.</param>
        /// <param name="yIntercept">The y-intercept value of the line (i.e. y = ax + b, yIntercept is b).</param>
        /// <param name="slope">The slop of the line (i.e. y = ax + b, slope is a).</param>
        public static void LinearRegression(
            List<double> xVals,
            List<double> yVals,
            out double rSquared,
            out double yIntercept,
            out double slope)
        {
            if (xVals.Count != yVals.Count)
            {
                throw new Exception("Input values should be with the same length.");
            }

            double sumOfX = 0;
            double sumOfY = 0;
            double sumOfXSq = 0;
            double sumOfYSq = 0;
            double sumCodeviates = 0;

            for (int i = 0; i < xVals.Count; i++)
            {
                double x = xVals[i];
                double y = yVals[i];
                sumCodeviates += x * y;
                sumOfX += x;
                sumOfY += y;
                sumOfXSq += x * x;
                sumOfYSq += y * y;
            }

            int count = xVals.Count;
            double ssX = sumOfXSq - ((sumOfX * sumOfX) / count);
            double ssY = sumOfYSq - ((sumOfY * sumOfY) / count);

            double rNumerator = (count * sumCodeviates) - (sumOfX * sumOfY);
            double rDenom = (count * sumOfXSq - (sumOfX * sumOfX)) * (count * sumOfYSq - (sumOfY * sumOfY));
            double sCo = sumCodeviates - ((sumOfX * sumOfY) / count);

            double meanX = sumOfX / count;
            double meanY = sumOfY / count;
            double dblR = rNumerator / Math.Sqrt(rDenom);

            rSquared = dblR * dblR;
            yIntercept = meanY - ((sCo / ssX) * meanX);
            slope = sCo / ssX;
        }

        public static double ComputeCorrelation(List<double> list1, List<double> list2)
        {
            if (list1 == null || list2 == null)
                throw new ArgumentNullException("Lists cannot be null");

            int count = list1.Count;
            if (count != list2.Count)
                throw new ArgumentException("Lists must have the same number of elements");

            if (count < 2)
                throw new ArgumentException("Lists must have at least two elements to compute correlation");

            double avg1 = list1.Average();
            double avg2 = list2.Average();

            double sum1 = list1.Zip(list2, (x, y) => (x - avg1) * (y - avg2)).Sum();
            double sumSq1 = list1.Sum(x => Math.Pow(x - avg1, 2));
            double sumSq2 = list2.Sum(x => Math.Pow(x - avg2, 2));

            double stdDev1 = Math.Sqrt(sumSq1 / count);
            double stdDev2 = Math.Sqrt(sumSq2 / count);

            stdDev1 = stdDev1 == 0 ? 1 : stdDev1;
            stdDev2 = stdDev2 == 0 ? 1 : stdDev2;

            double covariance = sum1 / count;

            return covariance / (stdDev1 * stdDev2);
        }

        public static System.Drawing.Color GetColorFromString(string colorString)
        {
            // Split the input string by commas and convert the components to integers
            string[] components = colorString.Split(',');
            if (components.Length != 3)
            {
                throw new ArgumentException("Invalid color string format. Expected 'R, G, B'.");
            }

            if (int.TryParse(components[0].Trim(), out int red) &&
                int.TryParse(components[1].Trim(), out int green) &&
                int.TryParse(components[2].Trim(), out int blue))
            {
                // Ensure the RGB components are within the valid range [0, 255]
                red = Math.Max(0, Math.Min(255, red));
                green = Math.Max(0, Math.Min(255, green));
                blue = Math.Max(0, Math.Min(255, blue));

                // Create and return the Color object
                return System.Drawing.Color.FromArgb(red, green, blue);
            }
            else
            {
                throw new ArgumentException("Invalid color component format. Expected integers.");
            }
        }

        internal void HighlightContext(bool highlight)
        {
            contextGuids.ForEach(contextGuid =>
            {
                RhinoObject obj = RhinoDoc.ActiveDoc.Objects.FindId(contextGuid);
                obj.Highlight(highlight);
            });
        }

        internal void SwitchDaylightMesh(int daylightMeshIndex)
        {
            SwitchDaylightMesh(daylightMeshIndex, currentAlternative);
        }

        internal Task SwitchDaylightMesh(int daylightMeshIndex, Alternative alt)
        {

            return Task.Run(() =>
            {
                alt.currentDaylightMeshIndex = daylightMeshIndex;
                alt.daylightMeshesIds.ForEach(meshID => { Rhino.RhinoDoc.ActiveDoc.Objects.Hide(meshID, true); });
                Rhino.RhinoDoc.ActiveDoc.Objects.Show(alt.daylightMeshesIds[daylightMeshIndex], true);
                RhinoDoc.ActiveDoc.Views.Redraw();
            });
        }

        
    }
}