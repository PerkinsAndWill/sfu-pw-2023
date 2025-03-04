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
using Excel = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using System.Windows;
using Rhino.Render.ChangeQueue;
using System.Xml.Linq;
using CefSharp.Handler;
using System.Reflection;
using CefSharp;

namespace DPredict.ViewModels
{
    public static class ProcessUtility
    {
        [DllImport("user32.dll")]
        static extern int GetWindowThreadProcessId(int hWnd, out int lpdwProcessId);

        public static Process GetExcelProcess(Excel.Application excelApp)
        {
            int id;
            GetWindowThreadProcessId(excelApp.Hwnd, out id);
            return Process.GetProcessById(id);
        }
    }

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

        [JsonProperty]
        internal Dictionary<string, object> data = new Dictionary<string, object>();

        [JsonProperty]
        internal Dictionary<string, List<double>> metrics = new Dictionary<string, List<double>>();

        [JsonProperty]
        internal string imageBytes;

        [JsonProperty]
        internal string timestamp;

        // user inputs
        internal Curve zone;

        internal List<Curve> interiorWalls = new List<Curve>();

        internal List<Brep> additionalBuildingGeometry = new List<Brep>();

        internal List<GeometryBase> context = new List<GeometryBase>();

        // refs to user inputs
        [JsonProperty]
        internal Guid zoneGuid;

        [JsonProperty]
        internal List<Guid> interiorWallsGuids = new List<Guid>();

        [JsonProperty]
        internal List<Guid> additionalBuildingGeometryGuids = new List<Guid>();

        [JsonProperty]
        internal List<Guid> contextGuids = new List<Guid>();

        // generated
        [JsonProperty]
        internal List<Guid> currentObjectsGuids = new List<Guid>();

        internal List<Guid> wallsGuid = new List<Guid>();
        internal List<Brep> walls = new List<Brep>();
        internal Mesh currentlySelectedWall;
        internal List<Brep> currentlySelectedWalls = new List<Brep>();
        internal List<int> wallDirections = new List<int>();
        internal List<Guid> daylightMeshesIds = new List<Guid>();
        internal int currentDaylightMeshIndex = 0;

        public static void Clear(Alternative alt, RhinoDoc doc, bool clearDoc = true)
        {
            // doc objects
            if (doc != null && clearDoc)
            {
                int countD = doc.Objects.Delete(alt.currentObjectsGuids, true);
                if (countD != alt.currentObjectsGuids.Count)
                {
                    Debug.WriteLine(String.Format("CurrentObjects: could not delete {0} objects\n", alt.currentObjectsGuids.Count - countD));
                }
            }

            // clear zone
            alt.zone = null;
            alt.zoneGuid = Guid.Empty;

            // clear interior walls
            alt.interiorWalls = new List<Curve>();
            alt.interiorWallsGuids = new List<Guid>();

            // clear additional building geometry
            alt.additionalBuildingGeometryGuids = new List<Guid>();
            alt.additionalBuildingGeometry = new List<Brep>();

            // clear context
            alt.context = new List<GeometryBase>();
            alt.contextGuids = new List<Guid>();

            // clear exterior walls (generated)
            alt.walls = new List<Brep>();
            alt.wallsGuid = new List<Guid>();

            // clear other geometries
            alt.currentObjectsGuids = new List<Guid>();

            // clear heatmaps
            alt.daylightMeshesIds = new List<Guid>();
        }

        /// <summary>
        /// Links ref objects (such as loaded from a .json file) to geometric objects within a rhino document
        /// </summary>
        /// <param name="alt"></param>
        /// <param name="doc"></param>
        /// <returns>true on success, false if one or more geometries could not be loaded. </returns>
        public static bool Relink(Alternative alt, RhinoDoc doc)
        {
            try
            {
                alt.zone = (Curve)doc.Objects.FindGeometry(alt.zoneGuid);
                alt.interiorWalls = alt.interiorWallsGuids.Select(guid => (Curve)doc.Objects.FindGeometry(guid)).ToList();
                alt.additionalBuildingGeometry = alt.additionalBuildingGeometryGuids.Select(guid => Brep.TryConvertBrep(doc.Objects.FindGeometry(guid))).ToList();
                alt.context = alt.contextGuids.Select(guid => doc.Objects.FindGeometry(guid)).ToList();
            }
            catch (Exception ex) {

                RhinoApp.WriteLine(ex.ToString());
            }
            if (alt.zone == null || alt.interiorWalls.Contains(null) || alt.additionalBuildingGeometry.Contains(null)  || alt.context.Contains(null))
            {
                return false;
            }
            return true;
        }
        public static string RandomHexString(int len)
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

        public BoundingBox BoundingBox(double pad = 0.2)
        {
            // Calculate the bounding box on the 
            BoundingBox bbox = zone.GetBoundingBox(false);

            double dx = (bbox.Max.X - bbox.Min.X) * pad;
            double dy = (bbox.Max.Y - bbox.Min.Y) * pad;
            double dz = (bbox.Max.Z - bbox.Min.Z) * pad;
            bbox.Inflate(dx, dy, dz);

            return bbox;
        }

        internal Alternative()
        {
            data["num"] = RandomHexString(5);
            data["name"] = "";

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
            data["floor_to_floor"] = (double)3.2;
            data["footprint_offset"] = 0;

            data["ceiling_reflectance"] = 0.7;
            data["floor_reflectance"] = 0.2;
            data["wall_reflectance"] = 0.5;
            data["glazing_transparency"] = 0.6;

            data["grid_size"] = 2;

            data["floor_num"] = 0;
            data["program"] = "2019::PrimarySchool::Classroom";

            data["wall_r_val"] = 30;
            data["roof_r_val"] = 20;
            data["ground_r_val"] = 20;
            data["win_u_val"] = 2.2;
            data["win_shgc"] = 0.3;

            data["enable_energy"] = false;
            data["enable_daylight"] = true;

#if !DEBUG
            data["weather_file"] = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), 
                                    "resources", "CAN_BC_Vancouver.Harbour.CS.712010_TMYx.2004-2018.epw");
#endif
        }
    }


    internal class UnicornViewModel : Rhino.UI.ViewModel
    {

        private string ImageToBase64String(Bitmap bitmap)
        {
            if (bitmap == null) return null;

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
        internal string epcSpreadsheet = "";
        internal bool clippingState = true;

        DateTime lastRequestDate;

        internal Task SaveCurrentAlt(string name, bool isAutomatedSave, bool registerAsCurrent=false)
        {
            // isAutomatedSave: determines if save current or save a copy
            return SaveCustomViewAlt(currentAlternative.currentObjectsGuids, currentAlternative.zone, name, isAutomatedSave, registerAsCurrent);
        }

        private RhinoView customView = null;
        private RhinoView userView = null;
        internal void InitViews()
        {
            userView = null;
            customView = null;
            parametricanalysisView = null;
            RhinoDoc doc = RhinoDoc.ActiveDoc;

            // Store current view
            bool oldViewMaximized = false;
            if (doc.Views.ActiveView != null)
            {
                userView = doc.Views.ActiveView;
                oldViewMaximized = userView.Maximized;
            }

            // Create and set up a new view
            parametricanalysisView = doc.Views.Find("ParametricAnalysisView", false);
            if (parametricanalysisView == null)
            {
                parametricanalysisView = doc.Views.Add("ParametricAnalysisView", DefinedViewportProjection.Perspective, new Rectangle(-10000, -10000, 800, 600), true);
            }
            else
            {
                parametricanalysisView.ActiveViewport.SetProjection(DefinedViewportProjection.Perspective, null, false);
            }
            if (parametricanalysisView == null)
            {
                RhinoApp.WriteLine("Failed to create parametric view.");
            }

            // Create and set up a new view
            customView = doc.Views.Find("CustomView", false);
            if (customView == null)
            {
                customView = doc.Views.Add("CustomView", DefinedViewportProjection.Perspective, new Rectangle(-9000, -9000, 800, 600), true);
            }
            else
            {
                customView.ActiveViewport.SetProjection(DefinedViewportProjection.Perspective, null, false);
            }
            if (customView == null)
            {
                RhinoApp.WriteLine("Failed to create custom view.");
            }

            // restore previous view
            if (userView != null && doc.Views.ActiveView != userView)
            {
                doc.Views.ActiveView = userView;
                userView.Maximized = oldViewMaximized;
            }
        }

        protected void UpdateUserView(bool forceUpdate = false)
        {
            RhinoDoc doc = RhinoDoc.ActiveDoc;
            if (doc.Views.ActiveView != null && doc.Views.ActiveView.ActiveViewport.Name != "CustomView" &&
                            doc.Views.ActiveView.ActiveViewport.Name != "ParametricAnalysisView")
            {
                userView = doc.Views.ActiveView;
                return;
            }
            else if (forceUpdate)
            {
                userView = doc.Views.Find("Top", false);  //doc.Views.GetViewList(true, false)[0];
            }
        }

        private RhinoView parametricanalysisView;
        internal void UpdateParametricViewport()
        {
            RhinoDoc doc = RhinoDoc.ActiveDoc;

            // Store current view
            RhinoView oldView = null;
            bool oldViewMaximized = false;
            if (doc.Views.ActiveView != null)
            {
                oldView = doc.Views.ActiveView;
                oldViewMaximized = oldView.Maximized;
            }

            // Create and set up a new view
            parametricanalysisView = doc.Views.Find("ParametricAnalysisView", false);
            if (parametricanalysisView == null)
            {
                parametricanalysisView = doc.Views.Add("ParametricAnalysisView", DefinedViewportProjection.Perspective, new Rectangle(-10000, -10000, 800, 600), true);
            }
            else
            {
                parametricanalysisView.ActiveViewport.SetProjection(DefinedViewportProjection.Perspective, null, false);
            }
            if (parametricanalysisView == null)
            {
                RhinoApp.WriteLine("Failed to create a new view.");
                return;
            }

            // restore previous view
            if (oldView != null && doc.Views.ActiveView != oldView)
            {
                doc.Views.ActiveView = oldView;
                oldView.Maximized = oldViewMaximized;
            }
        }

        internal void UpdateCurstomViewport()
        {
            RhinoDoc doc = RhinoDoc.ActiveDoc;

            // Store current view
            RhinoView oldView = null;
            bool oldViewMaximized = false;
            if (doc.Views.ActiveView != null)
            {
                oldView = doc.Views.ActiveView;
                oldViewMaximized = oldView.Maximized;
            }

            // Create and set up a new view
            customView = doc.Views.Find("CustomView", false);
            if (customView == null)
            {
                customView = doc.Views.Add("CustomView", DefinedViewportProjection.Perspective, new Rectangle(-9000, -9000, 800, 600), true);
            }
            else
            {
                customView.ActiveViewport.SetProjection(DefinedViewportProjection.Perspective, null, false);
            }
            if (customView == null)
            {
                RhinoApp.WriteLine("Failed to create a new view.");
                return;
            }

            // restore previous view
            if (oldView != null && doc.Views.ActiveView != oldView)
            {
                doc.Views.ActiveView = oldView;
                oldView.Maximized = oldViewMaximized;
            }

        }

        internal Task SaveObjectsAndImage(List<Guid> objectGuids, Alternative alt, double height, string name, string subfolder, bool moveFar = true)
        {
            return Task.Run(() =>
            {
                try
                {
                    RhinoApp.InvokeAndWait(delegate
                    {
                        RhinoDoc doc = RhinoDoc.ActiveDoc;
                        IEnumerable<RhinoObject> objects = objectGuids.Select(guid => doc.Objects.FindId(guid));

                        List<RhinoObject> objectsLst = objects.Where(obj => obj != null).ToList();

                        if (!objectsLst.Any())
                        {
                            RhinoApp.WriteLine("No objects found.");
                            return;
                        }

                        List<RhinoObject> allObjs = doc.Objects.Where(obj => obj != null).ToList();

                        ////Hide everything else so we could capture only our objects
                        allObjs.ForEach(obj =>
                        {
                            if (!objectGuids.Contains(obj.Id))
                            {
                                ObjectAttributes attributes = obj.Attributes;
                                attributes.ViewportId = userView.ActiveViewportID;
                                doc.Objects.ModifyAttributes(obj.Id, attributes, true);
                            }
                        });

                        Guid cPlaneGuid = Guid.Empty;
                        Clip(alt.zone, parametricanalysisView.ActiveViewportID, ref cPlaneGuid, true, height);

                        parametricanalysisView.ActiveViewport.ZoomBoundingBox(alt.BoundingBox());
                        parametricanalysisView.Redraw();

                        DisplayModeDescription displaymode = DisplayModeDescription.FindByName("Arctic");

                        // Capture the view to a bitmap
                        Bitmap bm = parametricanalysisView.CaptureToBitmap(new Size(parametricanalysisView.ActiveViewport.Size.Width, parametricanalysisView.ActiveViewport.Size.Height), displaymode);


                        string savingFolder = Path.Combine(UnicornPlugin.Instance.GetDataFolderPath(), subfolder);
                        if (!Directory.Exists(savingFolder))
                        {
                            Directory.CreateDirectory(savingFolder);
                        }

                        string altFileName = name;

                        string combinedPath = Path.Combine(savingFolder, altFileName);

                        string json = JsonConvert.SerializeObject(alt, GeometryResolver.Settings);
                        File.WriteAllText(combinedPath + ".json", json);
                        bm.Save(combinedPath + ".png");

                        // restore visibility of non-parametric analysis objects
                        allObjs.ForEach(obj =>
                        {
                            if (!objectGuids.Contains(obj.Id))
                            {
                                ObjectAttributes attributes = obj.Attributes;
                                attributes.ViewportId = Guid.Empty;
                                doc.Objects.ModifyAttributes(obj.Id, attributes, true);
                            }
                        });

                        // Delete clipping plane
                        if (cPlaneGuid != Guid.Empty)
                        {
                            doc.Objects.Delete(cPlaneGuid, true);
                        }

                    });
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Error in SaveCustomViewAlt: {ex.Message}");
                }
            });
        }

        internal Task SaveCustomViewAlt(List<Guid> objectGuids, Curve zone, string name, bool isAutomatedSave = false, bool registerAsCurrent=false)
        {
           
            return Task.Run(() =>
            {
                try
                {
                    RhinoApp.InvokeOnUiThread((Action)delegate
                    {
                        if (customView == null)
                        {
                            UpdateCurstomViewport(); 
                        }

                        RhinoDoc doc = RhinoDoc.ActiveDoc;
                        List<RhinoObject> objects = objectGuids.Select(guid => doc.Objects.Find(guid)).Where(obj => obj != null).ToList();

                        if (!objects.Any())
                        {
                            RhinoApp.WriteLine("No objects found.");
                            return;
                        }

                        // Calculate the bounding box on the 
                        BoundingBox bbox = zone.GetBoundingBox(false);

                        Guid cPlaneGuid = Guid.Empty;
                        ClipInViewport(true, customView.ActiveViewportID, ref cPlaneGuid);

                        double pad = 0.02;    // A little padding...
                        double dx = (bbox.Max.X - bbox.Min.X) * pad;
                        double dy = (bbox.Max.Y - bbox.Min.Y) * pad;
                        double dz = (bbox.Max.Z - bbox.Min.Z) * pad;
                        bbox.Inflate(dx, dy, dz);

                        // Zoom to the bounding box of selected objects

                        bool succeeded = customView.ActiveViewport.ZoomBoundingBox(bbox);
                        customView.Redraw();

                        DisplayModeDescription displaymode = DisplayModeDescription.FindByName("Arctic");

                        // Capture the view to a bitmap
                        Bitmap bm = customView.CaptureToBitmap(new Size(customView.ActiveViewport.Size.Width, customView.ActiveViewport.Size.Height), displaymode);

                        currentAlternative.imageBytes = ImageToBase64String(bm);
                        string number = currentAlternative.data["num"].ToString();  // keep the record
                        if (!isAutomatedSave) // save as a copy
                        {
                            currentAlternative.data["num"] = Alternative.RandomHexString(5);
                        }
                        currentAlternative.data["name"] = name;
                        currentAlternative.timestamp = DateTime.Now.ToString();

                        string json = JsonConvert.SerializeObject(currentAlternative, GeometryResolver.Settings);

                        string savingFolder = UnicornPlugin.Instance.GetDataFolderPath();
                        string altFileName = currentAlternative.data["num"].ToString();
                        string combined = Path.Combine(savingFolder, altFileName + ".json");
                        File.WriteAllText(combined, json);

                        if (registerAsCurrent)
                        {
                            LogAltAsCurrent(currentAlternative);
                        }

                        // restore original number, reset name
                        currentAlternative.data["num"] = number;
                        currentAlternative.data["name"] = "";

                        // Delete clipping plane of custom view
                        if (cPlaneGuid != Guid.Empty)
                        {
                            doc.Objects.Delete(cPlaneGuid, true);
                            doc.Views.Redraw();
                        }

                        UnicornPlugin.UIInterop.UpdateAlts();
                    });
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Error in SaveCustomViewAlt: {ex.Message}");
                }
            });
        }


        async internal void LoadAltAsCurrent(string name, bool compute = true, string subfolder = "", bool overWriteCurrent = true)
        {

            string fileToLoad = Path.Combine(Path.Combine(UnicornPlugin.Instance.GetDataFolderPath(), subfolder), name + ".json");
            if (File.Exists(fileToLoad))
            {
                Alternative alternative = LoadAlt(fileToLoad);
                RhinoDoc doc = RhinoDoc.ActiveDoc;
                string num = currentAlternative.data["num"].ToString();
                Alternative.Clear(currentAlternative, doc);
                if (!Alternative.Relink(alternative, doc))
                {
                    RhinoApp.WriteLine(String.Format("Attention: One or more geometries could not be loaded into the project. " +
                        "Aborted loading alternative '{0}' (id:{1})", alternative.data["name"], alternative.data["num"]));

                    // abort
                    Alternative.Clear(alternative, doc);
                    UnicornPlugin.UIInterop.UpdateUIData("isZoneSet", false);
                    UnicornPlugin.UIInterop.UpdateUIData("isInteriorWallsSet", false);
                    UnicornPlugin.UIInterop.UpdateUIData("isBuildingGeometrySet", false);
                    UnicornPlugin.UIInterop.UpdateUIData("isContextSet", false);

                    return;
                }
                RhinoApp.WriteLine(String.Format("Loaded alternative '{0}' (id:{1})", alternative.data["name"], alternative.data["num"]));
                currentAlternative = alternative;
                if (overWriteCurrent) currentAlternative.data["num"] = num;
                if (currentAlternative.data["name"].ToString()=="autoSave") currentAlternative.data["name"] = "";

                UnicornPlugin.UIInterop.SetUniqueSessionAltNum(currentAlternative.data["num"].ToString());

                Curve geometry = alternative.zone;
                //To set an initial wwrPerWall value
                double[] wwrPerWall = (double[])alternative.data["WWR_per_wall"];

                int[] vShadingCountsPerWall = (int[])alternative.data["verticalShadings_multiplier"];
                double[] vShadingDepthsPerWall = (double[])alternative.data["verticalShadings_depth"];
                int[] hShadingCountsPerWall = (int[])alternative.data["horizontalShadings_multiplier"];
                double[] hShadingDepthsPerWall = (double[])alternative.data["horizontalShadings_depth"];
                double[] overhangsOffsetPerWall = (double[])alternative.data["overhangs_offset"];
                double[] overhangsDepthPerWall = (double[])alternative.data["overhangs_depth"];


                int[] vShadingOnOffPerWall = (int[])alternative.data["verticalFnOnOff"];
                int[] hShadingOnOffPerWall = (int[])alternative.data["horizontalFnOnOff"];
                int[] overhangsOnOffPerWall = (int[])alternative.data["overhangsOnOff"];

                UnicornPlugin.UIInterop.SetNumWalls(wwrPerWall.Length);
                UnicornPlugin.UIInterop.setWWRShadingPerWall(wwrPerWall, vShadingCountsPerWall, vShadingDepthsPerWall, hShadingCountsPerWall, hShadingDepthsPerWall, overhangsOffsetPerWall, overhangsDepthPerWall, vShadingOnOffPerWall, hShadingOnOffPerWall, overhangsOnOffPerWall);

                UnicornPlugin.UIInterop.UpdateUIData("isZoneSet", alternative.zone != null);
                UnicornPlugin.UIInterop.UpdateUIData("isInteriorWallsSet", 
                    alternative.interiorWalls != null && alternative.interiorWalls.Where(x => x!=null).Count() > 0);
                UnicornPlugin.UIInterop.UpdateUIData("isBuildingGeometrySet", 
                    alternative.additionalBuildingGeometry != null && alternative.additionalBuildingGeometry.Where(x => x != null).Count() > 0);
                UnicornPlugin.UIInterop.UpdateUIData("isContextSet",
                    alternative.context != null && alternative.context.Where(x => x != null).Count() > 0);

                try
                {
                    IEnumerable<string> lines = File.ReadLines(currentAlternative.data["weather_file"].ToString());
                    string location = "";
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split(',');
                        location = parts[1] + ", " + parts[2] + ", " + parts[3];
                        break;
                    }
                    UnicornPlugin.UIInterop.UpdateUIData("weatherFileLocation", location);
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine("Failed loading epw file: {0}", ex.Message);
                }

                if (!compute)
                {
                    currentAlternative.data["enable_energy"] = false;
                    currentAlternative.data["enable_daylight"] = false;
                }
                else
                {
                    await UpdateData(currentAlternative, "ready", true, false, true, false);
                }
                UnicornPlugin.UIInterop.UpdateInputsData(currentAlternative.data);

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
            Dictionary<string, object> data = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> keyvaluePair in alternative.data)
            {
                string key = keyvaluePair.Key;
                object value = keyvaluePair.Value;
                object res = null;
                if (key == "WWR_per_wall" || key == "verticalShadings_depth" || key == "horizontalShadings_depth" || key == "overhangs_offset" || key == "overhangs_depth")
                {
                    try
                    {
                        res = ((JArray)value).ToObject<double[]>();
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }
                else if (key == "verticalShadings_multiplier" || key == "horizontalShadings_multiplier" || key == "verticalFnOnOff" || key == "horizontalFnOnOff" || key == "overhangsOnOff")
                {
                    try
                    {
                        res = ((JArray)value).ToObject<int[]>();
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }
                else //remaining are primitives that don't need casting back
                {
                    res = value;
                }
                data[key] = res;
            }
            alternative.data = data;
            return alternative;
        }
        internal void DeleteAlt(string name)
        {
            string fileToDelete = Path.Combine(UnicornPlugin.Instance.GetDataFolderPath(), name + ".json");
            if (File.Exists(fileToDelete))
            {
                File.Delete(fileToDelete);
                UnicornPlugin.UIInterop.UpdateAlts();
            }
        }

        internal string GetAlts()
        {
            IOrderedEnumerable<string> altFiles = Directory.GetFiles(UnicornPlugin.Instance.GetDataFolderPath(), "*.json")
                .Where(file => { Alternative alt = LoadAlt(file); return alt.data["name"].ToString() != "autoSave"; })
                .OrderByDescending(d => new FileInfo(d).LastWriteTime);
            return "[" + String.Join(",", altFiles.Select(fn => File.ReadAllText(fn))) + "]";
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
            RhinoDoc.DeleteRhinoObject += OnDeleteRhinoObject;
            RhinoDoc.SelectObjects += OnSelectObjects;
            RhinoDoc.DeselectAllObjects += DeselectAllObjects;
            RhinoDoc.DeselectObjects += OnSelectObjects;
            RhinoDoc.CloseDocument += ResetDPredict;
            RhinoDoc.EndOpenDocumentInitialViewUpdate += (sender, e) => { InitViews(); InitDoc(sender, e); };
            RhinoDoc.BeginSaveDocument += (sender, e) => { TempClearClippingPlance(); };
            RhinoDoc.EndSaveDocument += (sender, e) => { 
                RestoreClippingPlane(); 
                LogCurrentAlt(sender, e); 
            };
            RhinoApp.Closing += CloseExcelAndDelete;
            Rhino.UI.Panels.Show += OnShowPanel;

            UnicornPlugin.ServerLoaded += () =>
            {
                if (UnicornPlugin.UIInterop != null)
                {
                    UnicornPlugin.UIInterop.SetServerLoaded();
                }
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

            BuildingGeometrySet += (bgeom) =>
            {
                SetBuildingGeometry(bgeom);
            };

            InitEpcSpreadsheet();
        }

        private async void LogCurrentAlt(object sender, DocumentSaveEventArgs e)
        {

            SaveCurrentAlt("autoSave", isAutomatedSave: false, registerAsCurrent: true);
        }

        private void LogAltAsCurrent(Alternative alternative)
        {
            string logPath = Path.Combine(UnicornPlugin.Instance.GetDataFolderPath(), "currentID.txt");
            using (StreamWriter sw = new StreamWriter(logPath))
            {
                sw.WriteLine(alternative.data["num"].ToString(), FileMode.Create);
            }
        }

        public static string GetLastModifiedFile(string directoryPath)
        {
            IEnumerable<FileInfo> allFiles = new DirectoryInfo(directoryPath).EnumerateFiles();
            if (allFiles.Count() > 0)
            {
                return allFiles.OrderBy(fi => fi.LastWriteTime).Last().FullName;
            }
            else
            {
                return null;
            }
        }

        public static string GetRegisteredAltID()
        {
            string num = null;
            string logPath = Path.Combine(UnicornPlugin.Instance.GetDataFolderPath(), "currentID.txt");
            if (File.Exists(logPath))
            {
                using (StreamReader sr = new StreamReader(logPath))
                {
                    num = sr.ReadLine();
                }
            }
            return num;
        }

        private async void RelinkCurrentAlt()
        {
            await Task.Run(() =>
            {
                string altID = GetRegisteredAltID();
                if (altID != null)
                {
                    int waitCycles = 0;
                    while (!UnicornPlugin.UIInterop.Browser.CanExecuteJavascriptInMainFrame && waitCycles < 10)
                    {
                        System.Threading.Thread.Sleep(500);
                        waitCycles++;
                    }
                    // load working alt file. requires access to browser/UI
                    if (UnicornPlugin.UIInterop.Browser.CanExecuteJavascriptInMainFrame)
                    {
                        LoadAltAsCurrent(altID, compute: false, subfolder: "", overWriteCurrent: true);
                        UnicornPlugin.UIInterop.UpdateAlts();
                    }
                    else
                    {
                        RhinoApp.InvokeOnUiThread((Action)delegate
                        {
                            RhinoApp.WriteLine("Failed to link alternative because operation timed out.");
                        });
                    }
                }
            });
        }

        private void ResetDPredict(object sender, DocumentEventArgs e)
        {
            // clear previous session
            if (currentAlternative != null && currentAlternative.zoneGuid != null)
            {
                Alternative.Clear(currentAlternative, RhinoDoc.ActiveDoc);
            }

            UnicornPlugin.Instance.UpdateDataPath(null);
            UnicornPlugin.UIInterop.UpdateAlts();

            UnicornPlugin.UIInterop.UpdateUIData("isZoneSet", false);
            UnicornPlugin.UIInterop.UpdateUIData("isInteriorWallsSet", false);
            UnicornPlugin.UIInterop.UpdateUIData("isBuildingGeometrySet", false);
            UnicornPlugin.UIInterop.UpdateUIData("isContextSet", false);
        }

        static int glassMaterialIndex = 0;

        private void InitEpcSpreadsheet()
        {
            // create copy of EPC excel spreadsheet and store filename
            string originalExcelPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "resources", "EPC_PW_1.0.xlsx");
            if (File.Exists(originalExcelPath))
            {
                string tempDir = Path.Combine(Path.GetDirectoryName(UnicornPlugin.Instance.GetDataFolderPath()), "tmp");
                Directory.CreateDirectory(tempDir);
                string excelPath = Path.Combine(tempDir, String.Format("EPC_PW_1.0_{0}.xlsx", Guid.NewGuid()));
                File.Copy(originalExcelPath, excelPath);
                if (!File.Exists(excelPath))
                {
                    Console.WriteLine("Could not create a copy of the epc file.");
                }
                epcSpreadsheet = excelPath;
            }
            else
            {
                Console.WriteLine(String.Format("File not found: {0}", originalExcelPath));
            }
        }

        private void CloseExcelAndDelete(object sender, EventArgs e)
        {
            Process process = null;
            try
            {
                // first get running excel app & workbook
                Excel.Workbook workbook = null;
                Excel.Application excelApp = (Excel.Application)Marshal2.GetActiveObject("Excel.Application");
                foreach (Excel.Workbook xlWorkbook in excelApp.Workbooks)
                {
                    if (xlWorkbook.FullName == epcSpreadsheet)
                    {
                        workbook = xlWorkbook;
                        break;
                    }
                }

                if (workbook != null)
                {
                    process = ProcessUtility.GetExcelProcess(excelApp);
                    // then close/quit
                    workbook.Close(false);
                    excelApp.Quit();

                    Marshal.ReleaseComObject(workbook);
                    Marshal.ReleaseComObject(excelApp.Workbooks);
                    Marshal.ReleaseComObject(excelApp.Worksheets);
                    Marshal.ReleaseComObject(excelApp);

                    workbook = null;
                    excelApp = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //last delete file
            try
            {
                File.Delete(epcSpreadsheet);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"{ex.Message}");
            }

            if (process != null)
            {
                process.Kill();
            }

        }


        private void InitDoc(object sender, DocumentOpenEventArgs e)
        {
            currentAlternative = new Alternative();
            // point to correct dirs
            string localDataDir = null;
            if (e.FileName != null && !e.FileName.Contains("AppData") && !e.FileName.Contains("Template"))
            {
                localDataDir = e.FileName.Replace(".3dm", "-DPredictData");
            }
            UnicornPlugin.Instance.UpdateDataPath(localDataDir);

            if (localDataDir != null && Directory.Exists(localDataDir))
            {
                RelinkCurrentAlt();
            }
        }

        internal void SetContext(List<GeometryBase> geometries)
        {
            Task.Run(async () =>
            {
                await UpdateData(currentAlternative, "context", geometries);
                UnicornPlugin.UIInterop.UpdateUIData("isContextSet", geometries.Count > 0);
            });
        }

        internal void SetInteriorWalls(List<GeometryBase> geometries)
        {
            Task.Run(async () =>
            {
                await UpdateData(currentAlternative, "interior_walls", geometries);
                UnicornPlugin.UIInterop.UpdateUIData("isInteriorWallsSet", geometries.Count > 0);
            });
        }

        internal void SetBuildingGeometry(List<GeometryBase> geometries)
        {
            Task.Run(async () =>
            {
                await UpdateData(currentAlternative, "building_geometry", geometries);
                UnicornPlugin.UIInterop.UpdateUIData("isBuildingGeometrySet", geometries.Count > 0);
            });
        }

        internal void SetZone(Curve geometry, bool fresh = true)
        {
            Task.Run(async () =>
            {
                UnicornPlugin.UIInterop.SetUniqueSessionAltNum(currentAlternative.data["num"].ToString());

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

                if (fresh || (!fresh && !(numSegments == wwrPerWall.Length && numSegments == vShadingCountsPerWall.Length &&
                    numSegments == vShadingDepthsPerWall.Length && numSegments == hShadingCountsPerWall.Length &&
                    numSegments == hShadingDepthsPerWall.Length && numSegments == overhangsOffsetPerWall.Length &&
                    numSegments == overhangsDepthsPerWall.Length && numSegments == overhangsOnOff.Length &&
                    numSegments == horizontalFnOnOff.Length && numSegments == verticalFnOnOff.Length)))
                {
                    wwrPerWall = Enumerable.Repeat(0.3, numSegments).ToArray();
                    vShadingCountsPerWall = Enumerable.Repeat<int>(5, numSegments).ToArray();
                    vShadingDepthsPerWall = Enumerable.Repeat<double>(0.0, numSegments).ToArray();
                    hShadingCountsPerWall = Enumerable.Repeat<int>(5, numSegments).ToArray();
                    hShadingDepthsPerWall = Enumerable.Repeat<double>(0.0, numSegments).ToArray();
                    overhangsOffsetPerWall = Enumerable.Repeat<double>(0.0, numSegments).ToArray();
                    overhangsDepthsPerWall = Enumerable.Repeat<double>(0.0, numSegments).ToArray();


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

        public delegate void BuildingGeometrysetHandler(List<GeometryBase> bgeom);

        public static event BuildingGeometrysetHandler BuildingGeometrySet;

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
                        Alternative.Clear(currentAlternative, RhinoDoc.ActiveDoc);
                        RhinoDoc.ActiveDoc.Views.Redraw();
                        UnicornPlugin.UIInterop.UpdateUIData("isZoneSet", false);
                        UnicornPlugin.UIInterop.UpdateUIData("isInteriorWallsSet", false);
                        UnicornPlugin.UIInterop.UpdateUIData("isBuildingGeometrySet", false);
                        UnicornPlugin.UIInterop.UpdateUIData("isContextSet", false);
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
                            currentAlternative.contextGuids = objsRef.Select(oref => oref.ObjectId).ToList();

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
                        ContextSet(new List<GeometryBase>());
                        RhinoApp.WriteLine("No context selected.");
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
                        InteriorWallsSet(new List<GeometryBase>());
                        RhinoApp.WriteLine("No interior walls were selected.");
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

        internal Result SetBuildingGeometry()
        {
            try
            {
                // We run the entire selection process on the UI thread without Task.Run.
                RhinoApp.InvokeOnUiThread((Action)delegate
                {
                    GetObject getter = new GetObject();
                    getter.AcceptNothing(true);
                    getter.GeometryFilter = ObjectType.Extrusion | ObjectType.Surface | ObjectType.Brep;
                    getter.AcceptEnterWhenDone(true);
                    getter.EnablePreSelect(true, true);
                    getter.EnablePostSelect(true);
                    getter.SetCommandPrompt("Select the additional building geometry");

                    // Run the object selection process
                    GetResult result = getter.GetMultiple(0, 999);


                    if (result == GetResult.Object)
                    {
                        // Selection succeeded

                        Rhino.DocObjects.ObjRef[] objsRef = getter.Objects();
                        if (objsRef.Length > 0)
                        {
                            currentAlternative.additionalBuildingGeometryGuids = objsRef.Select(oref => oref.ObjectId).ToList();

                            // Access the selected object's geometry
                            List<GeometryBase> geometries = new List<GeometryBase>();
                            foreach (ObjRef oRef in objsRef)
                            {
                                geometries.Add(oRef.Object().Geometry);
                            }
                            BuildingGeometrySet(geometries);
                        }
                    }
                    else if (result == GetResult.Nothing)
                    {
                        BuildingGeometrySet(new List<GeometryBase>());
                        RhinoApp.WriteLine("No additional building geometries were selected.");
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
            if (File.Exists(f))
            {
                try
                {
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
                catch (Exception e)
                {
                    RhinoApp.WriteLine("Could not open file {0}. {1}", f, e.Message);
                }
            }
        }

        internal async Task<List<GrasshopperDataTree>> UpdateCurrentAlternative(string key, object newValue, bool silent = false)
        {
            return await UpdateData(currentAlternative, key, newValue, silent);
        }

        async private Task<List<GrasshopperDataTree>> UpdateData(Alternative alt, string key, object newValue, bool silent = false, bool loadToRhino = true, bool updateOnlyIfChanged = true)
        {
            try
            {
                UnicornPlugin.UIInterop.Log(key + " " + newValue);
                RhinoDoc doc = RhinoDoc.ActiveDoc;
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
                    alt.context = (List<GeometryBase>)newValue;
                }
                else if (key == "interior_walls")
                {
                    alt.interiorWalls = ((IEnumerable)newValue).Cast<Curve>().ToList();
                }
                else if (key == "building_geometry")
                {
                    alt.additionalBuildingGeometry = ((IEnumerable)newValue).Cast<GeometryBase>().ToList().Select(geo => Brep.TryConvertBrep(geo)).ToList();
                }
                else
                {
                    alt.data[key] = newValue;
                }

                if (!silent)
                {
                    if (key == "zone" && loadToRhino)
                    {
                        RhinoApp.InvokeOnUiThread((Action)delegate
                        {
                            UpdateUserView();
                            userView.ActiveViewport.ZoomBoundingBox(alt.BoundingBox());
                        });
                    }
                    return await ComputeFromData(alt, alt.context, loadToRhino);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message); 
                return null;
            }
        }


        internal void VisualizeAnalysis(string analysisName)
        {
            // Run the server in a separate thread
            if (!DataLoadServer.isServerRunning)
            {
                Task serverTask = Task.Run(() => DataLoadServer.StartServer());
            }

            //load csv data
            string analysisFile = Path.Combine(UnicornPlugin.Instance.GetDataFolderPath(), parametricAnalysisFolder, analysisName, "data.csv");
            if (File.Exists(analysisFile) && Directory.GetParent(analysisFile).GetFiles().Length > 1)
            {
                var csv = File.ReadAllText(analysisFile);

                VisualizeCorrelationsCSV(csv);

                //call js code to load data into DesignExplorer 
                UnicornPlugin.UIInterop.VisualizeAnalysisData(csv);
            }
            else
            {
                UnicornPlugin.UIInterop.UpdateUIData("showAnalysisVisualization", (object)false);
            }
        }

        internal void OpenAnalysisFolder()
        {
            string subfolder = parametricAnalysisFolder;
            string analysisFolder = Path.Combine(UnicornPlugin.Instance.GetDataFolderPath(), subfolder);
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
            string analysisFolder = Path.Combine(UnicornPlugin.Instance.GetDataFolderPath(), parametricAnalysisFolder);
            if (!Directory.Exists(analysisFolder))
            {
                Directory.CreateDirectory(analysisFolder);
            }
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

        private Guid mainViewClippingPlaneGuid = Guid.Empty;

        double clippingHeight = 0.0;
        internal void TempClearClippingPlance()
        {
            RhinoDoc doc = RhinoDoc.ActiveDoc;

            if (mainViewClippingPlaneGuid != Guid.Empty && doc != null)
            {
                    ObjRef oRef = new ObjRef(doc, mainViewClippingPlaneGuid);
                    ClippingPlaneSurface cps = oRef.ClippingPlaneSurface();
                    if (cps != null && currentAlternative != null)
                    {
                        clippingHeight = cps.Plane.OriginZ - currentAlternative.zone.GetBoundingBox(Plane.WorldXY).Center.Z;
                    }

                    doc.Objects.Delete(mainViewClippingPlaneGuid, true);
                    mainViewClippingPlaneGuid = Guid.Empty;
            }
        }

        internal void RestoreClippingPlane()
        {
            RhinoDoc doc = RhinoDoc.ActiveDoc;
            if (doc != null && !doc.IsClosing && clippingState && currentAlternative != null && currentAlternative.zone != null && userView != null)
            {
                Clip(currentAlternative.zone, userView.ActiveViewportID, ref mainViewClippingPlaneGuid, true, clippingHeight);
            }
        }

        internal void Clip(bool enable)
        {
            RhinoDoc doc = RhinoDoc.ActiveDoc;
            if (doc != null)
            {
                if (doc.Views.ActiveView != null && doc.Views.ActiveView.ActiveViewport.Name != "CustomView" &&
                                doc.Views.ActiveView.ActiveViewport.Name != "ParametricAnalysisView")
                {
                    userView = doc.Views.ActiveView;
                }
                Guid viewportGuid = (userView != null ? userView : doc.Views.GetViewList(true, false)[0]).ActiveViewportID;
                ClipInViewport(enable, viewportGuid, ref mainViewClippingPlaneGuid);
            }
        }

        internal void ClipInViewport(bool enable, Guid viewportGuid, ref Guid clippingPlaneGuid)
        {
            if (currentAlternative != null && currentAlternative.zone != null)
            {
                Curve curve = currentAlternative.zone;
                double height = 3.2;
                if (currentAlternative.data["floor_to_floor"] is double)
                {
                    height = (double)currentAlternative.data["floor_to_floor"];
                }
                else
                {
                    double.TryParse(currentAlternative.data["floor_to_floor"].ToString(), out height);
                }
                Clip(curve, viewportGuid, ref clippingPlaneGuid, enable, height);

            }
        }

        public static void Clip(Curve curve, Guid viewportGuid, ref Guid clippingPlaneGuid, bool enable, double height = 1)
        {
            bool flip = true;
            RhinoDoc doc = RhinoDoc.ActiveDoc;

            Plane xyPlane = Plane.WorldXY;
            Point3d center = curve.GetBoundingBox(xyPlane).Center;

            ClippingPlaneSurface cps = null;
            if (clippingPlaneGuid != Guid.Empty)
            {
                ObjRef oRef = new ObjRef(doc, clippingPlaneGuid);
                cps = oRef.ClippingPlaneSurface();
                if (cps != null)
                {
                    center.Z = cps.Plane.OriginZ;  // TODO: this prob doesn't make sense in the current setup
                }
                doc.Objects.Delete(clippingPlaneGuid, true);
            }
            if (clippingPlaneGuid == Guid.Empty || cps == null)
            {
                center += Rhino.Geometry.Vector3d.ZAxis * height;
            }
            clippingPlaneGuid = Guid.Empty;


            if (curve != null && enable)
            {

                Plane clippingPlane = new Plane(center, Vector3d.XAxis, Vector3d.YAxis);

                if (flip)
                    clippingPlane.Flip();

                clippingPlaneGuid = doc.Objects.AddClippingPlane(clippingPlane, 0.5, 0.5, viewportGuid);
            }
            doc.Views.Redraw();
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

        async internal Task RunParametricAnalysis(string samplesJSON, string focusDictStr, string overrideParams)
        {
            RhinoApp.InvokeOnUiThread((Action)delegate {
                UpdateUserView();
                UpdateParametricViewport();
            });
            isParametricAnalysisRunning = true;
            Dictionary<string, int> focusDict = JsonConvert.DeserializeObject<Dictionary<string, int>>(focusDictStr);

            List<Dictionary<string, string>> samples = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(samplesJSON);
            Dictionary<string, string> paramOverrides = JsonConvert.DeserializeObject<Dictionary<string, string>>(overrideParams);
            List<List<GrasshopperDataTree>> results = new List<List<GrasshopperDataTree>>();
            List<List<double>> allOutputs = new List<List<double>>();
            UnicornPlugin.UIInterop.UpdateParametricAnalysisProgress(0, samples.Count, false);

            List<string> altNames = new List<string>();
            string analysisSubfolder = Path.Combine(parametricAnalysisFolder, String.Format("{0:0000}-{1:00}-{2:00}_{3:00}.{4:00}", 
                DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute));

            string parentAnalysisFolder = Path.Combine(UnicornPlugin.Instance.GetDataFolderPath(), parametricAnalysisFolder);
            if (!Directory.Exists(parentAnalysisFolder))
            {
                Directory.CreateDirectory(parentAnalysisFolder);
            }

            //-------------Writing to CSV -------
            string savingFolder = Path.Combine(UnicornPlugin.Instance.GetDataFolderPath(), analysisSubfolder);
            if (!Directory.Exists(savingFolder))
            {
                Directory.CreateDirectory(savingFolder);
            }

            Dictionary<string, string>.KeyCollection paramNames = samples[0].Keys;

            string altFileName = "data";
            string csvLogFilepath = Path.Combine(savingFolder, altFileName + ".csv");

            string csvHeader = String.Join(",", paramNames.ToList()) + ",out:sDA,out:ASE,out:UDIa,out:MI,out:Heating,out:Cooling,img\n";
            using (var sw = new StreamWriter(csvLogFilepath))
            {
                sw.Write(csvHeader);
            }
            //--------------end csv prep

            int num = 0;
            int daylightMeshIndex = currentAlternative.currentDaylightMeshIndex;
            Alternative benchmark = CloneAlt(currentAlternative);
            Alternative.Relink(benchmark, RhinoDoc.ActiveDoc);
            foreach (Dictionary<string, string> sample in samples)
            {
                if (!isParametricAnalysisRunning)
                {
                    UnicornPlugin.UIInterop.UpdateParametricAnalysisProgress(-1, 0, false);
                    break;
                }

                Dictionary<string, string> alt = new Dictionary<string, string>(sample);
                // Append param overrides to alt
                foreach (KeyValuePair<string, string> pair in paramOverrides)
                {
                    alt[pair.Key] = pair.Value;
                }

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
                        int hasShading = double.Parse(pair.Value) > 0 ? 1 : 0;
                        int[] enableShading = Enumerable.Repeat(hasShading, numWalls).ToArray();
                        string whichShadingDevice = key == "verticalShadings_depth" ? "verticalFnOnOff" : "horizontalFnOnOff";
                        UpdateData(benchmark, whichShadingDevice, enableShading, true, false, false);
                        // ensure the other shading device is off
                        if (hasShading > 0)
                        {
                            string otherShadingDevice = key == "verticalShadings_depth" ? "horizontalFnOnOff" : "verticalFnOnOff";
                            UpdateData(benchmark, otherShadingDevice, enableShading.Select(x => 1 - x).ToArray(), true, false, false);
                        }
                    }

                    else if (key == "overhangs_depth")
                    {
                        val = Enumerable.Repeat(double.Parse(pair.Value), numWalls).ToArray();
                        int hasOverhangs = double.Parse(pair.Value) > 0 ? 1 : 0;
                        int[] enableOverhangs = Enumerable.Repeat(hasOverhangs, numWalls).ToArray();
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
                benchmark.data["num"] = altName;

                try
                {
                    benchmark.data["isParametric"] = true;
                    List<GrasshopperDataTree> res = await UpdateData(benchmark, "ready", true, false, false, false);

                    if (res != null)
                    {
                        results.Add(res);
                        List<double> outputs = getOutputsFromComputeResults(res);
                        allOutputs.Add(outputs);

                        //---------------log results to csv
                        List<double> allValues = new List<double>();
                        //Adding all the inputs
                        foreach (string vs in sample.Values)
                        {
                            double v = double.Parse(vs);
                            allValues.Add(v);
                        }
                        //Adding all the outputs
                        allValues.AddRange(outputs.Take(6));
                        var imgURL = "http://localhost:3000/" + analysisSubfolder + "/" + (altName + ".png");
                        string csvLine = string.Join(",", allValues) + "," + imgURL + "\n";
                        using (var sw = File.AppendText(csvLogFilepath))
                        {
                            sw.Write(csvLine);
                        }
                        //--------------end log results to csv

                        //--------------take screenshot
                        RhinoDoc doc = RhinoDoc.ActiveDoc;
                        List<Guid> guids = CollectResults(res, ref benchmark, false, parametricanalysisView.ActiveViewportID);
                        // add context guids as duplicate geometry
                        ObjectAttributes attributes = new ObjectAttributes();
                        attributes.ViewportId = parametricanalysisView.ActiveViewportID;
                        guids.AddRange(benchmark.context.Select(geom => geom==null? Guid.Empty : doc.Objects.Add(geom.Duplicate(), attributes)).ToList());
                        guids.AddRange(benchmark.additionalBuildingGeometry.Select(geom => doc.Objects.Add(geom.Duplicate(), attributes)).ToList());

                        SwitchDaylightMesh(daylightMeshIndex, benchmark).Wait();

                        List<Guid> visibleGuids = guids.Select(id => doc.Objects.FindId(id)).Where(obj => obj != null).Where(obj => !obj.IsHidden).Select(obj => obj.Id).ToList();
                        double height = benchmark.data["floor_to_floor"] is double ? (double)benchmark.data["floor_to_floor"] : 3.2;

                        SaveObjectsAndImage(visibleGuids, benchmark, height, altName, analysisSubfolder, true).Wait();


                        //guids.ForEach(id => RhinoDoc.ActiveDoc.Objects.Delete(id, true));
                        doc.Objects.Delete(guids, true);
                        guids.Clear();
                        if (doc.Views.ActiveView != null)
                        {
                            doc.Views.Redraw();
                        }
                        //--------------end screenshot
                    }
                }
                catch (Exception e)
                {
                    RhinoApp.Write(String.Format("{0}\n{1}", e.Message, e.StackTrace));
                }


                num++;
                int progressPerc = (int)(((num * 1.0) / samples.Count) * 100);

                UnicornPlugin.UIInterop.UpdateParametricAnalysisProgress(progressPerc, samples.Count - num, false);
            }

            //------------- calculating and sending correlation results to front-end -------
            if (allOutputs.Count > 0)
            {
                ComputeThenVisualizeCorrelations(paramNames.ToList(), samples.Take(num).ToList(), allOutputs.Take(num).ToList(), focusDict);
            }

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

        private void ComputeThenVisualizeCorrelations(List<string> paramNames, List<Dictionary<string, string>> samples,
            List<List<double>> allOutputs, Dictionary<string, int> focusDict)
        {
            //------------- calculating and sending correlation results to front-end -------
            Dictionary<string, Tuple<List<double>, List<double>, List<double>>> models = new Dictionary<string, Tuple<List<double>, List<double>, List<double>>>();
            UnicornPlugin.UIInterop.ClearParametricAnalysisModels();

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

            ComputeServer.WebAddress = "http://localhost:8081"; // port 5000 is rhino.compute, 8081 is compute.geometry
                                                                //ComputeServer.ApiKey = "";

            string definitionName = "ParametricRoom_Latest.gh";

            string p = "";
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
#if !DEBUG
            p = Path.Combine(Directory.GetParent(assembly.Location).FullName, "resources");
#else
            p = Path.Combine(Directory.GetParent(assembly.Location).FullName, "Definitions");
#endif

            string definitionPath = Path.Combine(p, definitionName);

            List<GrasshopperDataTree> trees = new List<GrasshopperDataTree>();
            #region data preparation
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

            if (alt.additionalBuildingGeometry != null && alt.additionalBuildingGeometry.Count > 0)
            {
                GrasshopperDataTree param1 = new GrasshopperDataTree("additional_building_geometry");
                param1.Add("0", alt.additionalBuildingGeometry.Select(c => new GrasshopperObject(c)).ToList());
                trees.Add(param1);
            }

            {
                GrasshopperDataTree param1 = new GrasshopperDataTree("epcSpreadsheet");
                List<GrasshopperObject> lst = new List<GrasshopperObject>() { new GrasshopperObject(epcSpreadsheet) };
                param1.Add("0", lst);
                trees.Add(param1);
            }
            #endregion

            try
            {
                object isParametric;
                alt.data.TryGetValue("isParametric", out isParametric);
                if (isParametric == null || !(bool)isParametric)
                {
                    lastRequestDate = DateTime.Now;
                }
                DateTime thisRequestDate = DateTime.Now;

                result = Rhino.Compute.GrasshopperCompute.EvaluateDefinition(definitionPath, trees);
                if (thisRequestDate < lastRequestDate) // Always use the latest request and discard the old ones
                {
                    return null;
                }

                if (loadToRhino)
                {
                    RhinoDoc doc = RhinoDoc.ActiveDoc;

                    int countD = doc.Objects.Delete(alt.currentObjectsGuids, true);
                    if (countD != alt.currentObjectsGuids.Count)
                    {
                        Debug.WriteLine(String.Format("Could not delete {0} objects\n", alt.currentObjectsGuids.Count-countD));
                    }
                    alt.currentObjectsGuids.Clear();


                    Layer resultsLayer = doc.Layers.FindName("DPredictResults");
                    int layerIndex = -1;
                    if (resultsLayer == null)
                    {
                        resultsLayer = new Layer();
                        resultsLayer.Name = "DPredictResults";
                        layerIndex = doc.Layers.Add(resultsLayer);
                    }
                    else
                    {
                        layerIndex = resultsLayer.Index;
                    }

                    List<Guid> addedObjectsguids = CollectResults(result, ref alt, true, Guid.Empty);  

                    //Setting all the objects created from Rhino Compute to the assigned layer.
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

                    SwitchDaylightMesh(alt.currentDaylightMeshIndex, alt);

                    doc.Layers.Modify(resultsLayer, layerIndex, true);
                    doc.Views.Redraw();

                    if (clippingState) Clip(clippingState);

                    await SaveCurrentAlt(currentAlternative.data["name"].ToString(), true);
                    UnicornPlugin.UIInterop.UpdateCurrentAlt();  


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

        private List<Guid> CollectResults(List<GrasshopperDataTree> values, ref Alternative alt, bool updateUI, Guid viewportId)
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

                                        Rhino.DocObjects.Material glassMaterial = new Rhino.DocObjects.Material
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

            alt.metrics["Cooling"] = new List<double>() { Math.Round(loadsMetrics[1], 2) };
            alt.metrics["Heating"] = new List<double>() { Math.Round(loadsMetrics[0], 2) };

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

            if (viewportId != Guid.Empty)
            {
                foreach (Guid id in addObjectsGuids)
                {
                    RhinoObject ro = doc.Objects.FindId(id);
                    ro.Attributes.ViewportId = viewportId;
                    ro.CommitChanges();
                }

                RhinoView view = doc.Views.Find(viewportId);
                if (view != null) view.Redraw();
            }
            else
            {
                doc.Views.Redraw();
            }
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

        public void OnDeleteRhinoObject(object sender, RhinoObjectEventArgs e)
        {
            if (replacedObjectGuids.Contains(e.ObjectId))
            {
                replacedObjectGuids.Remove(e.ObjectId);
            }
            else
            {
                if (e.ObjectId == currentAlternative.zoneGuid)
                {
                    Alternative.Clear(currentAlternative, RhinoDoc.ActiveDoc);
                    RhinoDoc.ActiveDoc.Views.Redraw();
                    UnicornPlugin.UIInterop.UpdateUIData("isZoneSet", false);
                    UnicornPlugin.UIInterop.UpdateUIData("isInteriorWallsSet", false);
                    UnicornPlugin.UIInterop.UpdateUIData("isBuildingGeometrySet", false);
                    UnicornPlugin.UIInterop.UpdateUIData("isContextSet", false);
                }
                else
                {
                    int indexOfWall = currentAlternative.interiorWallsGuids.IndexOf(e.ObjectId);
                    if (indexOfWall >= 0)
                    {
                        currentAlternative.interiorWalls.RemoveAt(indexOfWall);
                        UpdateData(currentAlternative, "interior_walls", currentAlternative.interiorWalls);
                        if (currentAlternative.interiorWalls.Count == 0)
                        {
                            UnicornPlugin.UIInterop.UpdateUIData("isInteriorWallsSet", false);
                        }
                        return;
                    }

                    int indexOfContextPart = currentAlternative.contextGuids.IndexOf(e.ObjectId);
                    if (indexOfContextPart >= 0)
                    {
                        currentAlternative.context.RemoveAt(indexOfContextPart);
                        UpdateData(currentAlternative, "context", currentAlternative.context);
                        if (currentAlternative.context.Count == 0)
                        {
                            UnicornPlugin.UIInterop.UpdateUIData("isContextSet", false);
                        }
                        return;
                    }

                    int indexOfBuildingGeomPart = currentAlternative.additionalBuildingGeometryGuids.IndexOf(e.ObjectId);
                    if (indexOfBuildingGeomPart >= 0)
                    {
                        currentAlternative.additionalBuildingGeometry.RemoveAt(indexOfBuildingGeomPart);
                        UpdateData(currentAlternative, "building_geometry", currentAlternative.additionalBuildingGeometry);
                        if (currentAlternative.context.Count == 0)
                        {
                            UnicornPlugin.UIInterop.UpdateUIData("isBuildingGeometrySet", false);
                        }
                        return;
                    }
                }
            }
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
                        currentAlternative.interiorWalls[indexOfWall] = (Curve)e.RhinoObject.Geometry;
                        UpdateData(currentAlternative, "interior_walls", currentAlternative.interiorWalls);
                    }
                    return;
                }

                int indexOfContextPart = currentAlternative.contextGuids.IndexOf(e.RhinoObject.Id);
                if (indexOfContextPart >= 0)
                {
                    RhinoObject obj = RhinoDoc.ActiveDoc.Objects.Find(e.RhinoObject.Id);
                    bool flag = !GeometryBase.GeometryEquals(e.RhinoObject.Geometry, obj.Geometry);
                    if (flag)
                    {
                        currentAlternative.context[indexOfContextPart] = e.RhinoObject.Geometry;
                        UpdateData(currentAlternative, "context", currentAlternative.context);
                    }
                    return;
                }

                int indexOfWBuildingGeom = currentAlternative.additionalBuildingGeometryGuids.IndexOf(e.RhinoObject.Id);
                if (indexOfWBuildingGeom >= 0)
                {
                    RhinoObject obj = RhinoDoc.ActiveDoc.Objects.Find(e.RhinoObject.Id);
                    bool flag = !GeometryBase.GeometryEquals(e.RhinoObject.Geometry, obj.Geometry);
                    if (flag)
                    {
                        currentAlternative.additionalBuildingGeometry[indexOfWBuildingGeom] = (Brep)e.RhinoObject.Geometry;
                        UpdateData(currentAlternative, "building_geometry", currentAlternative.additionalBuildingGeometry);
                    }
                    return;
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

        List<Guid> replacedObjectGuids = new List<Guid>();
        private void OnObjectReplaced(object sender, RhinoReplaceObjectEventArgs e)
        {
            if (currentAlternative == null) return;

            if (e.ObjectId == currentAlternative.zoneGuid)
            {
                replacedObjectGuids.Add(e.ObjectId);
                SetZone((Curve)e.NewRhinoObject.Geometry, false);
            }
            else
            {
                int indexOfWall = currentAlternative.interiorWallsGuids.IndexOf(e.ObjectId);
                if (indexOfWall >= 0)
                {
                    replacedObjectGuids.Add(e.ObjectId);
                    currentAlternative.interiorWalls[indexOfWall] = (Curve)e.NewRhinoObject.Geometry;
                    UpdateData(currentAlternative, "interior_walls", currentAlternative.interiorWalls);
                    return;
                }

                int indexOfContextPart = currentAlternative.contextGuids.IndexOf(e.ObjectId);
                if (indexOfContextPart >= 0)
                {
                    replacedObjectGuids.Add(e.ObjectId);
                    currentAlternative.context[indexOfContextPart] = e.NewRhinoObject.Geometry;
                    UpdateData(currentAlternative, "context", currentAlternative.context);
                    return;
                }

                int indexOfBuildingGeom = currentAlternative.additionalBuildingGeometryGuids.IndexOf(e.ObjectId);
                if (indexOfBuildingGeom >= 0)
                {
                    replacedObjectGuids.Add(e.ObjectId);
                    currentAlternative.additionalBuildingGeometry[indexOfContextPart] = (Brep)e.NewRhinoObject.Geometry;
                    UpdateData(currentAlternative, "building_geometry", currentAlternative.additionalBuildingGeometry);
                    return;
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
            Rhino.DocObjects.Material rhino_material = new Rhino.DocObjects.Material
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
            {
                UnicornPlugin.UIInterop.ConsoleLog("Lists must have at least two elements to compute correlation");
                return 0;
            }

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
            currentAlternative.contextGuids.ForEach(contextGuid =>
            {
                RhinoObject obj = RhinoDoc.ActiveDoc.Objects.FindId(contextGuid);
                if (obj != null)
                {
                    obj.Highlight(highlight);
                }
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

        private bool isParametricAnalysisRunning = false;
        internal void SetStopParametricAnalysis()
        {
            isParametricAnalysisRunning = false;
        }
    }
}