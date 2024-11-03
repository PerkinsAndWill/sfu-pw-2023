using CefSharp;
using CefSharp.WinForms;
using Rhino;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Unicorn
{
    /// <summary>
    /// This is the user control that is buried in the tabbed, docking panel.
    /// </summary>
    [System.Runtime.InteropServices.Guid("4C10B3EF-7033-4461-9FC3-A0C902D16B88")]
    public partial class UnicornPanel : UserControl
    {
        /// <summary>
        /// Returns the ID of this panel.
        /// </summary>
        public static System.Guid PanelId => typeof(UnicornPanel).GUID;

        public static ChromiumWebBrowser Browser;

        private bool closing = false;

        /// <summary>
        /// Public constructor
        /// </summary>
        public UnicornPanel()
        {
            InitializeComponent();
            InitializeCef();
            InitializeChromium();

            UnicornPlugin.UnicornInterop = new UnicornInterop();
            UnicornPlugin.UIInterop = new UIInterop(Browser, UnicornPlugin.UnicornInterop);
            Browser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;

            if (!Browser.JavascriptObjectRepository.IsBound("Interop"))
            {
                Browser.JavascriptObjectRepository.Register("Interop", UnicornPlugin.UIInterop, isAsync: true, options: BindingOptions.DefaultBinder);
            }

            // Browser.RegisterAsyncJsObject("rhinoInterface", UnicornPlugin.Instance);
            // browser.JavascriptObjectRepository.LegacyBindingEnabled = true;
            //Browser.JavascriptObjectRepository.Register("rhinoInterface", this, isAsync: true, options: BindingOptions.DefaultBinder);

            Browser.LoadingStateChanged += (sender, args) =>
            {
                //Wait for the Page to finish loading
                if (args.IsLoading == false)
                {
#if DEBUG
                    UnicornPlugin.UIInterop.ShowDev();
#endif

                    Debug.WriteLine("Unicorn loaded to UI", "Unicorn");

                }
            };

            Browser.AddressChanged += (sender, args) =>
            {
                Debug.WriteLine("new address", "Unicorn");
            };

            this.Controls.Add(Browser);

            this.Disposed += UnicornPanel_Disposed;
            // Set the user control property on our plug-in
            UnicornPlugin.Instance.PanelUserControl = this;

            RhinoApp.Closing += RhinoApp_Closing;
        }

        public static void InitializeCef()
        {
            if (Cef.IsInitialized) return;

            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string assemblyPath = Path.GetDirectoryName(assemblyLocation);
            string pathSubprocess = Path.Combine(assemblyPath, "CefSharp.BrowserSubprocess.exe");

            CefSharpSettings.ConcurrentTaskExecution = true;
            CefSettings settings = new CefSettings
            {
                LogSeverity = LogSeverity.Verbose,
                LogFile = "ceflog.txt",
                BrowserSubprocessPath = pathSubprocess

            };

            settings.CefCommandLineArgs.Add("allow-universal-access-from-files", "1");
            settings.CefCommandLineArgs.Add("allow-file-access-from-files", "1");
            settings.CefCommandLineArgs.Add("disable-web-security", "1");
            Cef.Initialize(settings);
        }

        public static void InitializeChromium()
        {
            if (Browser != null && !Browser.IsDisposed) return;
#if DEBUG
            //use localhost
            Browser = new ChromiumWebBrowser(@"http://localhost:7070/");

            // var index = @"C:\dev\v4design\V4D4Rhino\V4D4RhinoViewer\v4d4rhinoviewer\public\";

#else
            //use app files

            string appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            string grasshopperLibrariesPath = Path.Combine(appDataPath, "Grasshopper", "Libraries", "MITACS folder");
            var path = grasshopperLibrariesPath;

            Debug.WriteLine(path, "Unicorn");

            var indexPath = string.Format(@"{0}\app\index.html", path);
            if (!File.Exists(indexPath))
                Debug.WriteLine("Unicorn: Error. The html file doesn't exists : {0}", "Unicorn");

            indexPath = indexPath.Replace("\\", "/");

            Browser = new ChromiumWebBrowser(indexPath);
            //index = indexPath;
#endif
            // Allow the use of local resources in the browser
            Browser.BrowserSettings = new BrowserSettings
            {
                //TODO: migrate to how it's now down in cefsharp the newer version
                // FileAccessFromFileUrls = CefState.Enabled,
                // UniversalAccessFromFileUrls = CefState.Enabled
            };
            Browser.Dock = System.Windows.Forms.DockStyle.Fill;
        }


        private void RhinoApp_Closing(object sender, EventArgs e)
        {
            closing = true;
            Dispose(closing);
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (!closing) return;
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            Browser.Dispose();
            Cef.Shutdown();
            base.Dispose(disposing);
        }

        private void OnDispose(object sender, EventArgs e)
        {
            Debug.WriteLine("Panel OnDisposed", "Unicorn");
        }

        private void UnicornPanel_Disposed(object sender, System.EventArgs e)
        {
            Debug.WriteLine("Panel Disposed", "Unicorn");
        }

    }
}
