using Rhino.PlugIns;
using Rhino.UI;
using System;
using System.Diagnostics;
using System.IO;

namespace DPredict
{
    ///<summary>
    /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    /// class. DO NOT create instances of this class yourself. It is the
    /// responsibility of Rhino to create an instance of this class.</para>
    /// <para>To complete plug-in information, please also see all PlugInDescription
    /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
    /// "Show All Files" to see it in the "Solution Explorer" window).</para>
    ///</summary>
    public class UnicornPlugin : Rhino.PlugIns.PlugIn
    {

        public delegate void ServerLoadedHandler();

        public static event ServerLoadedHandler ServerLoaded;


        public static UIInterop UIInterop;

        public static UnicornInterop UnicornInterop;
        public UnicornPlugin()
        {
            Instance = this;
        }

        ///<summary>Gets the only instance of the UnicornPlugin plug-in.</summary>
        public static UnicornPlugin Instance { get; private set; }

        // You can override methods here to change the plug-in behavior on
        // loading and shut down, add options pages to the Rhino _Option command
        // and maintain plug-in wide options in a document.
        /// <summary>
        /// The tabbed dockbar user control
        /// </summary>
        public UnicornPanel PanelUserControl { get; set; }
        private Process rhinoComputeProcess, webServerProcess;
        private bool isProductionMode = false;

        // public UnicornPanelWebView PanelUserControlWV { get; set; }

        // You can override methods here to change the plug-in behavior on
        // loading and shut down, add options pages to the Rhino _Option command
        // and maintain plug-in wide options in a document.


        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {

            Panels.RegisterPanel(this, typeof(UnicornPanel), "DPredict", DPredict.Properties.Resources.dpredict, PanelType.System);

            string tmp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DPredict");
            if (!Directory.Exists(tmp))
                Directory.CreateDirectory(tmp);

#if !DEBUG

            // If we are building for release, then the Server should start on its own.
            if (rhinoComputeProcess == null)
            {
                try
                {
                    // Create a new process
                    rhinoComputeProcess = new Process();
                    
                    // Get the AppData folder path on Windows
                    string appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
                    string grasshopperLibrariesPath = Path.Combine(appDataPath, "Grasshopper", "Libraries", "DPredict");

                    string path = grasshopperLibrariesPath;

                    string indexPath = string.Format(@"{0}\Server\compute.geometry.exe", path);
                    indexPath = indexPath.Replace("\\", "/");
                    rhinoComputeProcess.StartInfo.FileName = indexPath;
                    
                    // Configure the process using the StartInfo properties
                    rhinoComputeProcess.StartInfo.Arguments = ""; // Add arguments if necessary
                    rhinoComputeProcess.StartInfo.CreateNoWindow = true; // Set this to false if you want to see the window
                    rhinoComputeProcess.StartInfo.UseShellExecute = false; // Set this to true if you want to use the operating system shell to start the process
                    rhinoComputeProcess.StartInfo.RedirectStandardOutput = true;

                    rhinoComputeProcess.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data != null && e.Data.Contains("Loading Completed"))
                        {
                            ServerLoaded();
                        }

                    };

                    // Start the process
                    bool started = rhinoComputeProcess.Start();
                    rhinoComputeProcess.BeginOutputReadLine();

                    // Optionally, wait for the process to exit
                    // process.WaitForExit(); // You can also provide a timeout in milliseconds
                }
                catch (Exception ex)
                {
                    // Handle exceptions (e.g., file not found, no permissions, etc.)
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
#endif

            return base.OnLoad(ref errorMessage);
        }

        public string GetDataFolderPath()
        {
            string definitionPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            definitionPath = Path.GetDirectoryName(definitionPath);
            return definitionPath + "\\data\\";
        }

        protected override void OnShutdown()
        {

            if (rhinoComputeProcess != null)
            {
                rhinoComputeProcess.Kill();
                rhinoComputeProcess.WaitForExit(2000);
                rhinoComputeProcess.Close();

            }

            string definitionPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            definitionPath = Path.GetDirectoryName(definitionPath);
            string savingFolder = definitionPath + "\\data";
            if (File.Exists(savingFolder + "\\current.json"))
            {
                File.Delete(savingFolder + "\\current.json");
            }

            Instance.PanelUserControl?.Dispose();
            base.OnShutdown();

        }

    }
}