<template>
  <v-container id="main-container">
    <v-toolbar dense dark color="blue darken-3" class="mb-1">
      <v-checkbox
        :ripple="false"
        dense
        v-model="isShowingMaps"
        icon="large"
        label="Toggle Daylight Masks"
      />
      <v-spacer></v-spacer>

      <v-progress-circular
        v-show="isLoading"
        indeterminate
        :width="3"
        :size="25"
        color="orange"
      ></v-progress-circular>
    </v-toolbar>
    <v-row id="shapediver-container" no-gutters>
      <v-col :cols="3">
        <div id="shapediver-parameters"></div>
      </v-col>
      <v-col :cols="9" style="display: block; width: 100%; height: 100%">
        <canvas id="shapediver-canvas"></canvas>
      </v-col>
    </v-row>
    <v-row no-gutters v-show="isShowingMaps">
      <div id="maps-container"></div
    ></v-row>
  </v-container>
</template>

<!-- eslint-disable no-unused-vars -->
<script>
import { defineComponent } from "vue";
import {
  Vector3,
  Mesh,
  DoubleSide,
  Object3D,
  BufferGeometry,
  BufferAttribute,
  PointsMaterial,
  Points,
  MeshBasicMaterial,
  BoxGeometry,
  Color,
} from "three";
import { raycasting } from "./raycasting.js";

import {
  createViewport,
  createSession,
  MaterialEngine,
  addListener,
  EVENTTYPE,
  ThreejsData,
  TreeNode,
  sceneTree,
} from "@shapediver/viewer";

import * as tf from "@tensorflow/tfjs";

var shapediverViewport = null;
var shapediverSession = null;
var geomDict = {};
var daModel = null;
var vizScene = null;
var mapsDiv = null;
export default defineComponent({
  name: "MainComponent",
  props: {
    designModelTicket: null,
  },
  data: function () {
    return {
      isLoading: false,
      publicPath: process.env.BASE_URL,
      mapNames: [],
      geomNames: [],
      daylightColorMap: [],
      isShowingMaps: false,
    };
  },
  async created() {
    // ------------ Model specific variables ------------------
    this.mapNames = [
      "depth map",
      "transparency map",
      "reflectance map",
      "shading devices map",
      "context reflectance map",
      "context depth map",
    ];

    this.geomNames = [
      "walls geom",
      "floor geom",
      "ceiling geom",
      "glazing geom",
      "fin geom",
      "glazing Output",
      "glazing gltf",
    ];

    this.daylightColorMap = [
      new Color(76 / 255, 108 / 255, 169 / 255),
      new Color(108 / 255, 139 / 255, 196 / 255),
      new Color(163 / 255, 194 / 255, 241 / 255),
      new Color(182 / 255, 207 / 255, 232 / 255),
      new Color(225 / 255, 229 / 255, 145 / 255),
      new Color(254 / 255, 244 / 255, 85 / 255),
      new Color(246 / 255, 202 / 255, 54 / 255),
      new Color(236 / 255, 138 / 255, 9 / 255),
      new Color(234 / 255, 116 / 255, 1 / 255),
      new Color(235 / 255, 68 / 255, 1 / 255),
      new Color(234 / 255, 38 / 255, 1 / 255),
    ];

    // ------------ Load surrogate models -------------------

    //  -------  Setting up ShapeDiver -----------

    let modelURL = this.publicPath + "da_model/model.json";
    tf.loadLayersModel(modelURL).then((model) => {
      daModel = model;
    });

    await createSession({
      ticket: this.designModelTicket,

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
    mapsDiv = document.getElementById("maps-container");
    console.log(mapsDiv, shapediverDiv);
    this.createParametersSliders(
      shapediverSession,
      document.getElementById("shapediver-parameters")
    );
    this.isLoading = true;
    this.initialize();
    this.isLoading = false;

    addListener(EVENTTYPE.SESSION.SESSION_CUSTOMIZED, () => {
      console.trace();
      console.log("Model just got customized");
      this.isLoading = true;
      this.initialize();
      this.isLoading = false;
    });
  },
  methods: {
    /** Prepares a dictionary with the updates geometry, recompuates raycasting and reproduces the daylighting maps */
    initialize() {
      const startingTime = window.performance.now();
      try {
        shapediverViewport.update();
      } catch (e) {
        console.error(e);
      }

      Object.values(shapediverSession.outputs).forEach((o) => {
        this.geomNames.forEach((gn) => {
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

      // Raycasting parameters
      let simNumber = 0;
      let resHeight = 32;
      let maxDistance = 100;
      let ceiling = geomDict["ceiling geom"];
      let floor = geomDict["floor geom"];
      let wall = geomDict["walls geom"];
      let glazing = geomDict["glazing geom"];
      let shading = geomDict["glazing geom"];
      let surroundings = [];
      let ground = geomDict["floor geom"][0]; //TODO fetch real ground
      let wallsReflectance = 0.8;
      let floorReflectance = 0.5;
      let ceilingReflectance = 0.9;
      let windowsTransparency = 0.7;
      let finDepth = shapediverSession.getOutputByName("finDepth")[0];
      console.log("outputs finDepth", finDepth);

      if (finDepth) {
        finDepth = finDepth.content[0].data;
      } else {
        finDepth = shapediverSession.getParameterByName("Fins Depth")[0];
      }

      let finSpacing = Object.values(shapediverSession.outputs).find(
        (o) => o.name === "finSpacing"
      );

      if (finSpacing) {
        finSpacing = finSpacing.content[0].data;
      } else {
        finSpacing = shapediverSession.getParameterByName("Fins width")[0];
      }

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

      const images = this.convert3DArrayToBitmaps(output);
      this.updateMapsFromImageData(images, mapsDiv, this.mapNames);
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

      const input_data = tf.tensor(
        output.map((d) => {
          return d.flat();
        })
      );
      const predictions = daModel.predict(input_data);
      // console.log(predictions);

      //  -------------- Visualization ------------------------

      shapediverViewport.shadows = false;

      let predictionsArray = predictions
        .arraySync()
        .map((x) => Math.max(0, Math.min(1, x[0]))); // ensuringt the values stay within [0, 1]
      // console.log(predictionsArray);

      let room2DWidth = Object.values(shapediverSession.parameters).find(
        (x) => x.name === "width" || x.name === "Romm Width"
      ).value;
      let room2DHeight = Object.values(shapediverSession.parameters).find(
        (x) => x.name === "depth" || x.name === "Room Length"
      ).value;

      this.visualizePredictions(
        sensorsPos,
        predictionsArray,
        shapediverViewport,
        true,
        room2DWidth,
        room2DHeight
      );
      // Uncomment to see sensor points on the floor
      // visualizeSensorPos(sensorsPos, predictionsArray, shapediverViewport, true)
    },
    visualizePredictions(
      sensorsPos,
      predictionsArray,
      viewport,
      hideCeiling,
      width,
      height
    ) {
      if (hideCeiling) {
        geomDict["ceiling geom"].forEach((g) => (g.visible = false));
      }

      if (vizScene) {
        vizScene.remove(...vizScene.children);
        vizScene = null;
      }

      const visualizationNode = new TreeNode();
      // create an Object3D and add it to the node as a data item
      const scene = new Object3D();
      vizScene = scene;
      visualizationNode.data.push(new ThreejsData(vizScene));

      console.log(width, height);
      // add any kind of three js items to that object
      sensorsPos.forEach((pos, index) => {
        const box = new BoxGeometry(0.6096, 0.6096, 0.2);

        const color = this.interpolateColors(
          this.daylightColorMap,
          predictionsArray[index]
        );

        const material = new MeshBasicMaterial({ color: color });
        material.side = DoubleSide;
        const tile = new Mesh(box, material);
        tile.position.set(pos.x, pos.y, pos.z);

        scene.add(tile);
      });
      // add the node to the scene tree and update

      sceneTree.root.addChild(visualizationNode);
      sceneTree.root.updateVersion();
      viewport.update();
    },
    visualizeSensorPos(sensorsPos, predictionsArray, viewport, hideCeiling) {
      if (hideCeiling) {
        geomDict["ceiling geom"].forEach((g) => (g.visible = false));
      }

      if (vizScene) {
        vizScene.remove(...vizScene.children);
        vizScene = null;
      }

      const visualizationNode = new TreeNode();
      // create an Object3D and add it to the node as a data item
      const scene = new Object3D();
      vizScene = scene;
      visualizationNode.data.push(new ThreejsData(vizScene));

      // add any kind of three js items to that object
      sensorsPos.forEach((pos, index) => {
        const dotGeometry = new BufferGeometry();
        dotGeometry.setAttribute(
          "position",
          new BufferAttribute(new Float32Array([pos.x, pos.y, pos.z + 0.5]), 3)
        );
        const color = new Color(0, Math.min(1, predictionsArray[index]), 0);
        console.log(index, color);
        const dotMaterial = new PointsMaterial({ size: 0.5, color: color }); //0xff0000
        const dot = new Points(dotGeometry, dotMaterial);
        scene.add(dot);
      });

      // add the node to the scene tree and update
      sceneTree.root.addChild(visualizationNode);
      sceneTree.root.updateVersion();
      viewport.update();
    },
    interpolateColors(colors, t) {
      // Clamp the value of t between 0 and 1
      t = Math.min(1, Math.max(0, t));
      if (t === 1) return colors[colors.length - 1];
      if (t === 0) return colors[0];

      // Determine the number of color segments
      const segmentCount = colors.length - 1;
      const segmentSize = 1 / segmentCount;

      // Determine the current segment index and normalized t value within the segment
      const segmentIndex = Math.floor(t / segmentSize);
      const segmentT = (t - segmentIndex * segmentSize) / segmentSize;

      // Retrieve the start and end colors of the current segment
      const startColor = colors[segmentIndex].clone().convertSRGBToLinear();
      const endColor = colors[segmentIndex + 1].clone().convertSRGBToLinear();

      // Interpolate the RGB values between the start and end colors
      const interpolatedColor = new Color();
      interpolatedColor.r =
        startColor.r + (endColor.r - startColor.r) * segmentT;
      interpolatedColor.g =
        startColor.g + (endColor.g - startColor.g) * segmentT;
      interpolatedColor.b =
        startColor.b + (endColor.b - startColor.b) * segmentT;

      return interpolatedColor.convertLinearToSRGB();
    },
    convert3DArrayToBitmaps(array) {
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
    },
    createImageElementFromImageData(imageData) {
      const canvas = document.createElement("canvas");
      canvas.width = imageData.width;
      canvas.height = imageData.height;

      const ctx = canvas.getContext("2d");
      ctx.putImageData(imageData, 0, 0);

      const img = document.createElement("img");
      img.src = canvas.toDataURL();

      return img;
    },
    async updateMapsFromImageData(mapsImageData, mapsDiv, mapNames) {
      mapsDiv.replaceChildren();

      for (var i = 0; i < mapsImageData.length; i++) {
        const image = this.createImageElementFromImageData(mapsImageData[i]);

        const mDiv = document.createElement("div");
        mDiv.setAttribute("class", "map-image-div");
        const title = document.createElement("h3");
        title.innerHTML = mapNames[i];
        mDiv.appendChild(title);
        mDiv.appendChild(image);
        image.setAttribute("class", "map-image");
        mapsDiv.appendChild(mDiv);
      }
    },
    componentToHex(c) {
      var hex = c.toString(16);
      return hex.length == 1 ? "0" + hex : hex;
    },
    rgbToHex(r, g, b) {
      return (
        "#" +
        this.componentToHex(r) +
        this.componentToHex(g) +
        this.componentToHex(b)
      );
    },
    /** Used to create daylightingmaps based on image urls
     * (e.g., in case the maps were created in Grasshopper as bitmaps) */
    async updateMaps(session, mapsDiv, mapNames) {
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
    },
    map(value, istart, istop, ostart, ostop) {
      return ostart + (ostop - ostart) * ((value - istart) / (istop - istart));
    },
    async createParametersSliders(session, parameterDiv) {
      console.log(session);
      /*  for (var o in session.outputs) {
            console.log(session.outputs[o].name)
        } */
      // session.outputs.forEach((o) => );
      let viewerInit = false;
      let parameters = {};
      let thisVue = this;
      console.log(thisVue);
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
              console.log(session.parameters[param.id].value ,"vs.",this.value)

              if (session.parameters[param.id].value !== this.value) {
                session.parameters[param.id].value = this.value;
                thisVue.isLoading = true;
                await session.customize();
                thisVue.isLoading = false;
                //thisVue.createOutputsViz(session, globalDiv);
              }
            };
          } else if (param.type == "Bool") {
            paramInput = document.createElement("input");
            paramInput.setAttribute("id", param.id);
            paramInput.setAttribute("type", "checkbox");
            paramInput.setAttribute("checked", param.value);
            paramInput.onchange = async function () {
              if (session.parameters[param.id].value !== this.checked) {
                session.parameters[param.id].value = this.checked;
                thisVue.isLoading = true;
                await session.customize();
                thisVue.isLoading = false;
              }
            };
          } else if (param.type == "String") {
            paramInput = document.createElement("input");
            paramInput.setAttribute("id", param.id);
            paramInput.setAttribute("type", "text");
            paramInput.setAttribute("value", param.value);
            paramInput.onchange = async function () {
              if (session.parameters[param.id].value !== this.value) {
                session.parameters[param.id].value = this.value;
                thisVue.isLoading = true;
                await session.customize();
                thisVue.isLoading = false;
              }
            };
          } else if (param.type == "Color") {
            paramInput = document.createElement("input");
            paramInput.setAttribute("id", param.id);
            paramInput.setAttribute("type", "color");
            paramInput.setAttribute("value", param.value);
            paramInput.onchange = async function () {
              if (session.parameters[param.id].value !== this.value) {
                session.parameters[param.id].value = this.value;
                thisVue.isLoading = true;
                await session.customize();
                thisVue.isLoading = false;
              }
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
              if (session.parameters[param.id].value !== this.value) {
                session.parameters[param.id].value = this.value;
                thisVue.isLoading = true;
                await session.customize();
                thisVue.isLoading = false;
              }
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
        thisVue.isLoading = false;
      }
    },
    findChildByID(element, id) {
      let children = element.children;
      for (let i = 0; i < children.length; i++) {
        if (children[i].id == id) {
          return children[i];
        }
      }
    },
  },
});
</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->
<style scoped>
body {
  /*  overflow: hidden; */
  margin: 20px;
}

#main-container {
  display: flex;
  flex-direction: column;
}

#shapediver-container {
  position: relative;
  width: 100%;
  height: 60vh;
  top: 5vh;
}
#shapediver-container > canvas {
  position: absolute;
}
#shapediver-parameters {
  position: absolute;
  padding: 15px;
  height: 95%;
  overflow: auto;
  background-color: rgba(255, 255, 255, 0.5);
}
#maps-container {
  display: flex;
  flex-wrap: wrap;
  flex-direction: row;

  margin-top: 40px;
}
.map-image {
  width: 100px;
  height: auto;
}
.map-image-div {
  display: flex;
  flex-direction: column;
  padding: 20px;
}
</style>
