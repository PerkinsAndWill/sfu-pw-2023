import {
  Raycaster,
  Vector3,
  Mesh,
  Material,
  DoubleSide,
  Object3D,
  BufferGeometry,
  BufferAttribute,
  PointsMaterial,
  Points,
} from "three";
import { raycasting } from "./raycasting.js";

import {
  sessions,
  viewports,
  createViewport,
  createSession,
  IViewportApi,
  MaterialEngine,
  addListener,
  EVENTTYPE,
  ThreejsData,
  TreeNode,
  sceneTree,
} from "@shapediver/viewer";

import * as tf from '@tensorflow/tfjs';

// ------------ Model specific variables ------------------
const mapNames = [
  "depth map",
  "transparency map",
  "reflectance map",
  "shading devices map",
  "context reflectance map",
  "context depth map",
];

const geomNames = [
  "walls geom",
  "floor geom",
  "ceiling geom",
  "glazing geom",
  "fin geom",
  "glazing Output",
  "glazing gltf",
];

// ------------ Global variables ------------------
var shapediverDiv;
var shapediverViewport;
var shapediverSession;
var isLoading = false;
var geomDict = {};
var daModel;


(async () => {

  // ------------ Load surrogate models -------------------
    tf.loadLayersModel("da_model/model.json").then((model) => {
      daModel = model;
    })

  //  -------  Setting up ShapeDiver -----------
  await createSession({
    ticket:
      "30b267f1fae22edae4ba3b534777166e6e96f648fd7165cf048cd5d6a8e6e4662118ed377dfaf0e79d4678c4cdf940f81304169e7751dfdf0ac628b071c37e87aa29f1e7bead3271312643c26ca51281d5045397a4961ef4608a7fd3a153af23431f03aa246ee6-37c8214b26644476a3301cc14c876c5a",
    //  "98fe8ed861d69330041c9661da3d61086606a6617fb3380fc21e0537ab66ead0002788fbc3c612ef6ed746d60d4ab961e6c284969f9b88dab326438326bb92b45710b415f5598c32fa7c32ae3f994fb1605e0c4bb59ed20af6c896cec5814e5e38e96ec231c7d0-eed48907401cc45c0ca457d3cbed7972",
    //'2670fa61e598730ba7fde7c216458a947cfbf5b376d22b5099b0b5606921c5f0e196d9e2fb24d472baea7fbaeae3b7026225e6f30dfbcefaa1d1e37f297eb61ffd7ff9f10b1da445ca1dc3735e8ca45912448b6dfe706186e1bd676b57af7866dcfbe33dbc6fb5-ed10e8c5299adbb0bd084362d9a7bffa',
    //"1979a96ce6ca6a66d198df520d461be05d68e189af9fcc233e55b7d7c61d8f22b970db686730416d3729e96005317af274923a30760263f963e803e84871cbf05759b16de94a55f05cdde554918ab1cc3f0c80fd43be49488f7f55109e620e0c3652fbd4c6bbe3-3214df684b196a9264bd5cd0b4d78025",
    //'6ed7e05204e02d49e8be6172a4af9e41c40c2435c62afd3f0a1c31fdf1076f62945e8681209be384b7a3ba6ea0b868fc5bf29eb5df26eec35b128269e2fa13f26b3039d2d149e92c07005357791484f7cf85047561e6a23e1d59e1c0e841a60a4309a85ca03ce3-dac913e8b63871ac1e10c3435c5755b6',
    //'bc493c780f21368c43503eb4c28de9d554d6751102039c1458722fa0e32632203ae346a241169e3f4f82c1a8cf60f23ce7f02fba95cdac6eed3ac96c9b299237cb6ed5c25501bb3488aa466368bcd0d5c3f094debf79cabf3d589f5e7f0afd0205a501e752dbfe-35ccfe96a38ebed05510d5865ef7bd72',//'70888c4994249bb647fe7cff0baa10e7fd29183b75153af2a0e5f550d08d088cec41c0ef35ae4555cdee8a18280a850dc6e0111fbde54c6578a944eed09edd7509e38522086c01c9ad4d9c0969c4c58546461475c5533bfa0b3e9243a79da8b13bead63b1f5f52-e2f2b2b4a5ea58604a58e46fa5993410',
    modelViewUrl: "https://sdeuc1.eu-central-1.shapediver.com",
    id: "shapediver-session",
  }).then((session) => {
    shapediverSession = session;
    console.log("creted session", session);
  });

  await createViewport({
    canvas: document.getElementById("shapediver-canvas"),
    id: "shapediver-viewport",
  }).then((viewport) => {
    shapediverViewport = viewport;
  });

  var shapediverDiv = document.getElementById("shapediver-container");
  var mapsDiv = document.getElementById("maps-container");

  createParametersSliders(
    shapediverSession,
    findChildByID(shapediverDiv, "shapediver-parameters")
  );

  initialize();

  addListener(EVENTTYPE.SESSION.SESSION_CUSTOMIZED, (e) => {
    initialize();
  });

  // ---------------Functions -----------

  /** Prepares a dictionary with the updates geometry, recompuates raycasting and reproduces the daylighting maps */
  function initialize() {
    const startingTime = window.performance.now();

    shapediverViewport.update();

    Object.values(shapediverSession.outputs).forEach((o) => {
      geomNames.forEach((gn) => {
        if (gn === o.name) {
          console.log(gn, o);
          if (o.format[0] === "glb") {
            //console.log(gn, o);
            //geomDict[gn] = o.node.threeJsObject['shapediver-viewport']

            var primitveNodes = [];
            o.node.traverse((n) => {
              if (n.data.length > 0 && n.children.length === 0) {
                n.data.forEach((pn) => {
                  primitveNodes.push(pn.threeJsObject["shapediver-viewport"]);
                });
              }
            });
            geomDict[gn] = primitveNodes;
          }
        }
      });
    });

    console.log(geomDict);
    // =--------------- RAY CASTING -------------
    let sensorsPos = Object.values(shapediverSession.outputs)
      .find((o) => o.name === "sensorsPos")
      .content[0].data.map((a) => new Vector3(a[0], a[1], a[2]));

    // Uncomment to see sensor points on the floor
    // visualizeSensorPos(sensorsPos, shapediverViewport, true)

    // Raycasting parameters
    let simNumber = 0;
    let resHeight = 32;
    let maxDistance = 100;
    let ceiling = geomDict["ceiling geom"];
    let floor = geomDict["floor geom"];
    let wall = geomDict["walls geom"];
    let glazing = geomDict["glazing gltf"];
    let shading = geomDict["glazing gltf"];
    let surroundings = [];
    let ground = geomDict["floor geom"][0]; //TODO fetch real ground
    let wallsReflectance = 0.8;
    let floorReflectance = 0.5;
    let ceilingReflectance = 0.9;
    let windowsTransparency = 0.7;
    let finDepth = Object.values(shapediverSession.outputs).find(
      (o) => o.name === "finDepth"
    ).content[0].data;
    let finSpacing = Object.values(shapediverSession.outputs).find(
      (o) => o.name === "finSpacing"
    ).content[0].data;

    const inputsReadyTime = window.performance.now();

    const output = raycasting(
      simNumber,
      resHeight,
      sensorsPos,
      ceiling,
      floor,
      wall,
      glazing,
      shading,
      surroundings,
      ground,
      wallsReflectance,
      floorReflectance,
      ceilingReflectance,
      windowsTransparency,
      finDepth,
      finSpacing,
      maxDistance
    );
    const images = convert3DArrayToBitmaps(output);
    updateMapsFromImageData(images, mapsDiv, mapNames);
    console.log(
      "Inputs read and prep took: " +
        (inputsReadyTime - startingTime) / 1000.0 +
        " seconds."
    );
    console.log(
      "Raycasting took: " +
        (window.performance.now() - inputsReadyTime) / 1000.0 +
        " seconds."
    );
    console.log(
      "Sensor views update took: " +
        (window.performance.now() - startingTime) / 1000.0 +
        " seconds."
    );

    const input_data = tf.tensor(output.map((d) => {return d.flat()}))
    const predictions = daModel.predict(input_data)
    console.log(predictions)
  
  }

  function visualizeSensorPos(sensorsPos, viewport, hideCeiling) {
    if (hideCeiling) {
      geomDict["ceiling geom"].visible = false;
    }

    // create a node that contains our data
    const threejsNode = new TreeNode();

    // create an Object3D and add it to the node as a data item
    const scene = new Object3D();
    threejsNode.data.push(new ThreejsData(scene));

    // add any kind of three js items to that object
    sensorsPos.forEach((pos) => {
      const dotGeometry = new BufferGeometry();
      dotGeometry.setAttribute(
        "position",
        new BufferAttribute(new Float32Array([pos.x, pos.y, pos.z]), 3)
      );
      const dotMaterial = new PointsMaterial({ size: 0.1, color: 0xff0000 });
      const dot = new Points(dotGeometry, dotMaterial);
      scene.add(dot);
    });

    // add the node to the scene tree and update
    sceneTree.root.addChild(threejsNode);
    sceneTree.root.updateVersion();
    viewport.update();
  }

  function convert3DArrayToBitmaps(array) {
    const width = array.length;
    const height = array[0].length;
    const depth = array[0][0].length;

    const bitmaps = new Array(depth);

    for (let d = 0; d < depth; d++) {
      const bitmap = new ImageData(width, height);

      for (let y = 0; y < height; y++) {
        for (let x = 0; x < width; x++) {
          const val = Math.min(255, Math.floor(array[x][y][d] * 255));
          const b = val & 0x000000ff;
          const g = (val & 0x0000ff00) >> 8;
          const r = (val & 0x00ff0000) >> 16;

          const color = (r << 16) | (g << 8) | b;
          const index = (y * width + x) * 4;
          bitmap.data[index] = r;
          bitmap.data[index + 1] = g;
          bitmap.data[index + 2] = b;
          bitmap.data[index + 3] = 255; // Alpha value (255 for opaque)
        }
      }

      bitmaps[d] = bitmap;
    }

    return bitmaps;
  }
  function createImageElementFromImageData(imageData) {
    const canvas = document.createElement("canvas");
    canvas.width = imageData.width;
    canvas.height = imageData.height;

    const ctx = canvas.getContext("2d");
    ctx.putImageData(imageData, 0, 0);

    const img = document.createElement("img");
    img.src = canvas.toDataURL();

    return img;
  }
  async function updateMapsFromImageData(mapsImageData, mapsDiv, mapNames) {
    mapsDiv.replaceChildren();

    for (var i = 0; i < mapsImageData.length; i++) {
      const image = createImageElementFromImageData(mapsImageData[i]);

      const mDiv = document.createElement("div");
      mDiv.setAttribute("class", "map-image-div");
      const title = document.createElement("h3");
      title.innerHTML = mapNames[i];
      mDiv.appendChild(title);
      mDiv.appendChild(image);
      image.setAttribute("class", "map-image");
      mapsDiv.appendChild(mDiv);
    }
  }

  /** Used to create daylightingmaps based on image urls
   * (e.g., in case the maps were created in Grasshopper as bitmaps) */
  async function updateMaps(session, mapsDiv, mapNames) {
    const materialEngine = MaterialEngine.instance;
    mapsDiv.replaceChildren();
    const materialOutputs = Object.values(session.outputs).filter(
      (o) => o.material === undefined
    );
    const materialMaps = materialOutputs.filter((o) =>
      mapNames.includes(o.name)
    );
    console.log(materialMaps);

    for (var i = 0; i < materialMaps.length; i++) {
      const matMap = materialMaps[i];
      if (matMap.content.length > 0) {
        console.log(matMap.content[0]);
        const colorMap = await materialEngine.loadMap(
          matMap.content[0].data.bitmaptexture
        );
        const mDiv = document.createElement("div");
        mDiv.setAttribute("class", "map-image-div");
        const title = document.createElement("h3");
        title.innerHTML = mapNames[i];
        mDiv.appendChild(title);
        mDiv.appendChild(colorMap.image);
        colorMap.image.setAttribute("class", "map-image");
        mapsDiv.appendChild(mDiv);
      } else {
        console.log("Empty content! @", i);
      }
    }
  }

  async function createParametersSliders(session, parameterDiv) {
    console.log(session);
    /*  for (var o in session.outputs) {
            console.log(session.outputs[o].name)
        } */
    // session.outputs.forEach((o) => );
    let viewerInit = false;
    let parameters = {};
    let thisVue = this;
    if (!viewerInit) {
      var globalDiv = parameterDiv;
      globalDiv.replaceChildren(); // to clear up the div before adding to it.
      parameters.data = Object.values(session.parameters);
      parameters.data.sort(function (a, b) {
        return a.order - b.order;
      });

      for (let i = 0; i < parameters.data.length; i++) {
        let paramInput = null;
        let paramDiv = document.createElement("div");
        let param = parameters.data[i];
        let label = document.createElement("label");
        label.setAttribute("for", param.id);
        label.innerHTML = param.name;
        if (
          param.type == "Int" ||
          param.type == "Float" ||
          param.type == "Even" ||
          param.type == "Odd"
        ) {
          paramInput = document.createElement("input");
          paramInput.setAttribute("id", param.id);
          paramInput.setAttribute("type", "range");
          paramInput.setAttribute("min", param.min);
          paramInput.setAttribute("max", param.max);
          paramInput.setAttribute("value", param.value);
          if (param.type == "Int") paramInput.setAttribute("step", 1);
          else if (param.type == "Even" || param.type == "Odd")
            paramInput.setAttribute("step", 2);
          else
            paramInput.setAttribute(
              "step",
              1 / Math.pow(10, param.decimalplaces)
            );
          paramInput.onchange = async function () {
            session.parameters[param.id].value = this.value;
            isLoading = true;
            await session.customize();
            isLoading = false;
            //thisVue.createOutputsViz(session, globalDiv);
          };
        } else if (param.type == "Bool") {
          paramInput = document.createElement("input");
          paramInput.setAttribute("id", param.id);
          paramInput.setAttribute("type", "checkbox");
          paramInput.setAttribute("checked", param.value);
          paramInput.onchange = async function () {
            session.parameters[param.id].value = this.checked;
            isLoading = true;
            await session.customize();
            isLoading = false;
          };
        } else if (param.type == "String") {
          paramInput = document.createElement("input");
          paramInput.setAttribute("id", param.id);
          paramInput.setAttribute("type", "text");
          paramInput.setAttribute("value", param.value);
          paramInput.onchange = async function () {
            session.parameters[param.id].value = this.value;
            isLoading = true;
            await session.customize();
            isLoading = false;
          };
        } else if (param.type == "Color") {
          paramInput = document.createElement("input");
          paramInput.setAttribute("id", param.id);
          paramInput.setAttribute("type", "color");
          paramInput.setAttribute("value", param.value);
          paramInput.onchange = async function () {
            session.parameters[param.id].value = this.value;
            isLoading = true;
            await session.customize();
            isLoading = false;
          };
        } else if (param.type == "StringList") {
          paramInput = document.createElement("select");
          paramInput.setAttribute("id", param.id);
          for (let j = 0; j < param.choices.length; j++) {
            let option = document.createElement("option");
            option.setAttribute("value", j);
            option.setAttribute("name", param.choices[j]);
            option.innerHTML = param.choices[j];
            if (param.value == j) option.setAttribute("selected", "");
            paramInput.appendChild(option);
          }
          paramInput.onchange = async function () {
            session.parameters[param.id].value = this.value;
            isLoading = true;
            await session.customize();
            isLoading = false;
          };
        }
        if (param.hidden) paramDiv.setAttribute("hidden", "");
        paramDiv.appendChild(label);
        paramDiv.appendChild(paramInput);
        globalDiv.appendChild(paramDiv);
      }
      const outputDivContainer = document.createElement("div");
      outputDivContainer.setAttribute("id", "outputDivContainer");
      globalDiv.appendChild(outputDivContainer);
      viewerInit = true;
      isLoading = false;
    }
  }
  function findChildByID(element, id) {
    let children = element.children;
    for (let i = 0; i < children.length; i++) {
      if (children[i].id == id) {
        return children[i];
      }
    }
  }
})();
