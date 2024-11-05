
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unicorn.ViewModels;
using Rhino.Compute;
using System;
using System.Diagnostics;

#if WIN
using CefSharp;
using CefSharp.WinForms;
#endif

namespace Unicorn
{
    public class UIInterop
    {
#if WIN
        public ChromiumWebBrowser Browser { get; private set; }

        public UnicornInterop UnicornInterop { get; private set; }
#endif

        UnicornViewModel ViewModel;
#if WIN
        public UIInterop(ChromiumWebBrowser browser, UnicornInterop unicornInterop)
        {
            Browser = browser;
            UnicornInterop = unicornInterop;
            ViewModel = new UnicornViewModel();
        }

#endif

        #region To UI (Generic)

        public void ShowDev()
        {
            Browser.ShowDevTools();
        }
        public void ShowUILoaderAsync(bool show)
        {
            Task<JavascriptResponse> task = Browser.EvaluateScriptAsync("showLoader", show);
        }
        public void UpdateAlts()
        {
            Browser.EvaluateScriptAsync("updateAlts()");
        }
        public void UpdateCurrentAlt()
        {
            Browser.EvaluateScriptAsync("updateCurrentAlt()");
        }

        public void UpdateParametricAnalysisProgress(int progress, int samplesLeft, bool isEstimate)
        {
            Browser.EvaluateScriptAsync("updateParametricAnalysisProgress", JsonConvert.SerializeObject(progress), JsonConvert.SerializeObject(samplesLeft), JsonConvert.SerializeObject(isEstimate));
        }

        internal void setWWRShadingPerWall(double[] wwrPerWall, int[] vShadingCountsPerWall, double[] vShadingDepthsPerWall, int[] hShadingCountsPerWall, double[] hShadingDepthsPerWall, double[] overhangsOffsetPerWall, double[] overhangsDepthtPerWall, int[] verticalFnOnOff, int[] horizontalFnOnOff, int[] overhangsOnOff)
        {
            Task<JavascriptResponse> task = Browser.EvaluateScriptAsync("setWWRShadingPerWall", JsonConvert.SerializeObject(wwrPerWall), JsonConvert.SerializeObject(vShadingCountsPerWall), JsonConvert.SerializeObject(vShadingDepthsPerWall), JsonConvert.SerializeObject(hShadingCountsPerWall), JsonConvert.SerializeObject(hShadingDepthsPerWall), JsonConvert.SerializeObject(overhangsOffsetPerWall), JsonConvert.SerializeObject(overhangsDepthtPerWall), JsonConvert.SerializeObject(verticalFnOnOff), JsonConvert.SerializeObject(horizontalFnOnOff), JsonConvert.SerializeObject(overhangsOnOff));
        }


        internal void SetNumWalls(int numSegments)
        {
            Task<JavascriptResponse> task = Browser.EvaluateScriptAsync("setNumWalls", JsonConvert.SerializeObject(numSegments));
        }

        public void UpdateSelectedWallsOnUI(int[] selectedWalls)
        {
            Task<JavascriptResponse> task = Browser.EvaluateScriptAsync("onWallSelectedInRhino", JsonConvert.SerializeObject(selectedWalls));

        }

        public void UpdateUIData(string key, object value)
        {
            Task<JavascriptResponse> task = Browser.EvaluateScriptAsync("updateUIData(\"" + key + "\" , " + JsonConvert.SerializeObject(value) + ")");
        }

        internal void UpdateParametricAnalysisModels(string paramName, List<double> slopes, List<double> intercepts, List<double> correlations)
        {
            string str = "updateParametricAnalysisModels(\"" + paramName + "\" , " + JsonConvert.SerializeObject(slopes) + "," + JsonConvert.SerializeObject(intercepts) + "," + JsonConvert.SerializeObject(correlations) + ")";
            Task<JavascriptResponse> task = Browser.EvaluateScriptAsync(str);
        }

        public void UpdateUIOutputData(Dictionary<string, List<double>> objects)
        {
            Task<JavascriptResponse> task = Browser.EvaluateScriptAsync("recieveData(" + JsonConvert.SerializeObject(objects) + ")");
        }

        internal void UpdateUIOutputDataTrees(Dictionary<string, List<List<double>>> metrics_trees)
        {
            Task<JavascriptResponse> task = Browser.EvaluateScriptAsync("recieveDataTrees(" + JsonConvert.SerializeObject(metrics_trees) + ")");
        }
        internal void UpdateInputsData(Dictionary<string, object> data)
        {
            if (!Browser.IsBrowserInitialized)
            {
                Browser.ExecuteScriptAsyncWhenPageLoaded("setInputsData(" + JsonConvert.SerializeObject(data) + ")");
            }
            else
            {
                Browser.ExecuteScriptAsync("setInputsData(" + JsonConvert.SerializeObject(data) + ")");

            }
        }

        internal void VisualizeAnalysisData(string csv)
        {
            Browser.EvaluateScriptAsync("visualizeAnalysisData", csv);
        }

        internal void SetServerLoaded()
        {
            Browser.EvaluateScriptAsync("setServerLoaded()");
        }

        #endregion

        #region To UI

        #endregion

        #region From UI

        public void devTools()
        {
            ShowDev();
        }
        public void Log(object payload)
        {
            Browser.EvaluateScriptAsync("log(" + JsonConvert.SerializeObject(payload) + ")");
        }



        public async Task<List<GrasshopperDataTree>> UpdateBackendData(string key, string value, bool silent = false)
        {
            object res = ViewModel.DeserializeData(key, value);
            return await ViewModel.UpdateCurrentAlternative(key, res, silent);
        }

        public void UnselectAllWalls()
        {
            ViewModel.UnselectAllWalls();
        }

        public void SelectAllWalls()
        {
            ViewModel.SelectAllWalls();
        }

        public void SelectWallsInRhino(string selectedWallsNum)
        {
            ViewModel.SelectWallsInRhino(selectedWallsNum);
        }

        public void ClipByFootprint(string enable)
        {
            ViewModel.Clip(JsonConvert.DeserializeObject<bool>(enable));
        }

        public void onContextHovered(bool highlight)
        {
            ViewModel.HighlightContext(highlight);
        }

        public async Task RunParametricAnalysis(string input, string focusStr)
        {
            await ViewModel.RunParametricAnalysis(input, focusStr);
        }
        public async Task RunParametricAnalysisEstimate(string input)
        {
            await ViewModel.RunParametricAnalysisEstimate(input);
        }

        public void OpenAnalysisFolder()
        {
            ViewModel.OpenAnalysisFolder();
        }
        public void VisualizeAnalysis(string analysisName)
        {
            ViewModel.VisualizeAnalysis(analysisName);
        }
        


        public void SwitchDaylightMesh(int daylightMeshIndex)
        {
            ViewModel.SwitchDaylightMesh(daylightMeshIndex);
        }

        public async void SetZone()
        {
            ViewModel.SetZone();
        }
        public void SetContext()
        {
            ViewModel.SetContext();
        }
        public void SetInteriorWalls()
        {
            ViewModel.SetInteriorWalls();
        }

        public void SetWeatherFile()
        {
            ViewModel.SetWeatherFile();
        }

        public void SaveAlt(string name)
        {
            ViewModel.SaveCurrentAlt(name, false);
        }
        public void LoadAlt(string name, string subfolder = "")
        {
            ViewModel.LoadAltAsCurrent(name, subfolder);
        }

        public void DeleteAlt(string name)
        {
            ViewModel.DeleteAlt(name);
        }

        public string GetAlts()
        {
            return ViewModel.GetAlts();
        }

        public string GetAnalysisFolders()
        {
            return JsonConvert.SerializeObject(ViewModel.GetAnalysisFolders());
        }

        public string GetCurrentAlt()
        {
            return ViewModel.GetCurrentAlt();
        }
        #endregion

        #region utility

        public void doRunScript(string script)
        {
            Rhino.RhinoApp.RunScript(script, false);
        }

        


        #endregion



    }
}
