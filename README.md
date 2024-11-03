## Setup for development
1. Clone this repository.
2. Download Visual Studio (Community)
3. Install Node.js (to be able to run the Vue web app). Version 15.14.0 worked on my setup. You can install [NVM](https://dev.to/skaytech/how-to-install-node-version-manager-nvm-for-windows-10-4nbi) to help you choose the version of node that your want
4. Follow the steps on [this site](https://developer.rhino3d.com/guides/rhinocommon/installing-tools-windows/): 

## How to Run (for development)
### 1. Run the web app: 
    1. open the command line in the folder unicornviewer
    2. run `npm install`
    3. run `npm run serve`
    4. test that it works by opening the url http://localhost:7070/ in your browser
### 2. Run the Rhino Compute Server
    Go to the Server folder and run the compute.geometry.exe
### 3. Open the project in Visual Studio then go to Debug > Start Debugging
### 4. Setup Plugin Loading (done only once)
    From within Rhino, navigate to Tools > Options. Navigate to the Plugins page under Rhino Options and install your plugin (you can find it under \bin\Debug\net48\Unicord.rhp) 
    Right-click on any of the panels on the right and scroll to Unicorn and enable it
![image](https://github.com/abuzreq/Unicorn/assets/6095126/8ebafad6-e266-458c-bbaf-e2119b14cd69)
The GH file which Rhino compute will use is found under Definitions. As a convention the latest file should be named ParametricRoom_Latest.gh and the code expects this naming. Visual Studio automatically copies this file into the binaries when building.

## How to Run (for production):
We need to prepare two packages and share them with potential users. 

The first package will be placed in Grasshopper's libraries folder.
C:\Users\[USER]\AppData\Roaming\Grasshopper\Libraries\
Create a folder there named 'MITACS folder' and make sure the following files are in it:
1.  Server folder
2.  the default Vancouver weather file CAN_BC_Vancouver.Harbour.CS.712010_TMYx.2004-2018.epw (hard coded, don't change the name)
3.  The EPC Excel file EPC_PW_1.0.xlsx (hard coded, don't change the name)
4.  The Vue web app found under unicornviewer should be built with npm run build and the result should be placed in a folder called 'app'. At the moment a small fix is needed first. Open index.html and make sure that all paths include a . at their start. For example "./js/chunk-vendors.js" instead of "/js/chunk-vendors.js"
5. the latest GH file ParametricRoom_Latest.gh, which should be under Definitions

The second package includes everything under Unicorn/bin/Release when you build the project in Release mode. Naming does not matter here.


## System Architecture
![system_arch](https://github.com/user-attachments/assets/abc5f7d5-f26d-4527-903d-8738bcbacee2)

Typical flows:
1) Updating the UI: call from UnicornViewModel --> UIInterop -- calls JS using Browser.EvaluateScriptAsync -->  runs code on the web app but the called functions need to be registered on the window object in MainView.vue in mounted() for example "window.recieveData = this.recieveData;" 
2) From UI to Rhino or ViewModel: call functions on the UIInterio through window.Interop.functionName . Even if the function name was CapsCase the function should be called as window.Interop.capsCase in JS, e.g. window.Interop.updateBackendData() corrosponds to UpdateBackendData()


