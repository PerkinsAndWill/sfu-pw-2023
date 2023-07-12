import { Raycaster, Vector3, Mesh, BufferGeometry,  } from "three";


import {MeshBVH , acceleratedRaycast } from 'three-mesh-bvh';
//Mesh.prototype.raycast = acceleratedRaycast;
// Ideas for optimizing
// important thread https://github.com/mrdoob/three.js/issues/12857
// Octree? https://github.com/gkjohnson/three-mesh-bvh OR https://github.com/gkjohnson/threejs-octree
// Multithreading?

export function raycasting(
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
) {
    console.log(ceiling, floor, glazing)
  if (!finSpacing) {
    console.log(`Skipping iteration ${simNumber} because fin spacing is null`);
  } else {
    let shadingFactors = [];
    for (let i = 0; i < finSpacing.length; ++i) {
      let f;
      if (finSpacing[i] > 0) {
        f = finDepth[i] / finSpacing[i];
      } else {
        f = 0;
      }
      shadingFactors.push(f);
    }

    // will cast rays spherically around sensor
    let n_lat = resHeight;
    let n_long = 2 * resHeight;

    let step_lat = Math.PI / n_lat;
    let step_long = (2 * Math.PI) / n_long;

    let rays = [];
    let base_v = new Vector3(0, 0, 1);

    for (let i = 0; i < n_lat; ++i) {
      let ray = base_v.clone();
      // point to the correct latitude
      let angle_lat = -0.5 * step_lat + i * step_lat; // absolute angle
      ray.applyAxisAngle(new Vector3(1, 0, 0), angle_lat);
      for (let j = 0; j < n_long; ++j) {
        // relative rotation to previously rotated vector
        let angle_long = j === 0 ? 0.5 * step_long : step_long;
        ray.applyAxisAngle(new Vector3(0, 0, 1), angle_long);
        rays.push(ray.clone());
      }
    }

    // do raycasting with geometry

    // first join by type/material
    //let walls = wall[0]; //BufferGeometryUtils.mergeBufferGeometries(wall)

    //let windows = glazing[0]; //BufferGeometryUtils.mergeBufferGeometries(glazing)

    // list of all meshes
    let geometries = [...ceiling, ...floor, ...wall, ...glazing];
    console.log(geometries)
    geometries.forEach((geom) => {
        geom.geometry.boundsTree = new MeshBVH(  geom.geometry );
       // geom.geometry.computeBoundsTree();
      /*   geom.geometry.normalizeNormals () 
            geom.geometry.computeVertexNormals ()  //TODO is this needeed? computeVertexNormals?
            geom.geometry.computeBoundingBox()  */
    });

    // all outputs
    let sensorsResult = new Array(sensorsPos.length)
      .fill(0)
      .map(() =>
        new Array(n_lat * n_long).fill(0).map(() => new Array(6).fill(0))
      );

    // compute individual depthmaps for each geometry type, per sensor
    let sensorCount = 0;

    sensorsPos.forEach((sensorPos) => {
      let depthMaps = geometries.map(() => []);
      let raycaster = new Raycaster();
      raycaster.firstHitOnly = true;
      for (let i = 0; i < geometries.length; ++i) {
        for (let j = 0; j < rays.length; ++j) {
          raycaster.set(sensorPos, rays[j]);

          let intersections = [];
          let result = new Vector3();
          let bb = geometries[i].geometry.boundingBox;
          //if(!bb || bb && raycaster.ray.intersectBox(bb, result) !== null)
          {
            intersections = raycaster.intersectObject(geometries[i]);
          }

          let d = intersections.length > 0 ? intersections[0].distance : -1;
          depthMaps[i].push(d);
        }
      }

      // create separate shading map
      let shadingMap = new Array(rays.length).fill(-1);
      for (let i = 0; i < shading.length; ++i) {
        for (let j = 0; j < rays.length; ++j) {
          raycaster.set(sensorPos, rays[j]);
          
          let intersections = [];
          let result = new Vector3();
          let bb = geometries[i].geometry.boundingBox;
          //if(!bb || bb && raycaster.ray.intersectBox(bb, result) !== null)
          {
            intersections = raycaster.intersectObject(shading[i]);
          }
         
          let d = intersections.length > 0 ? intersections[0].distance : -1;
          if (d > 0) {
            shadingMap[j] = i;
          }
        }
      }

      // create ground plane / obstructions map
      let contextReflValues = [0.3, 0.4];
      let surrounding = new Mesh();
      surroundings = surrounding[0]; // BufferGeometryUtils.mergeBufferGeometries(surrounding)

      let contextMeshes = [ground, surrounding];

      let contextRefl = new Array(rays.length).fill(0);
      let contextDepth = new Array(rays.length).fill(0);

      for (let i = 0; i < contextMeshes.length; ++i) {
        for (let j = 0; j < rays.length; ++j) {
          let face_ii = [];
          raycaster.set(sensorPos, rays[j])

          let intersections = [];
          let result = new Vector3();
          let bb = geometries[i].geometry.boundingBox;
          //if(!bb || bb && raycaster.ray.intersectBox(bb, result) !== null)
          {
            intersections = raycaster.intersectObject(contextMeshes[i]);
          }

          let d = intersections.length > 0 ? intersections[0].distance : -1;
          if (d > 0) {
            contextRefl[j] = contextReflValues[i];
            contextDepth[j] = d;
          }
        }
      }

      // next compute summed up depthMap
      let depthMap = depthMaps[0].slice();
      let geomType = new Array(depthMaps[0].length).fill(0);
      for (let i = 0; i < depthMap.length; ++i) {
        for (let j = 1; j < depthMaps.length; ++j) {
          if (
            (depthMap[i] > 0 &&
              depthMaps[j][i] >= 0 &&
              depthMaps[j][i] < depthMap[i]) ||
            (depthMap[i] < 0 && depthMaps[j][i] >= 0)
          ) {
            depthMap[i] = depthMaps[j][i];
            geomType[i] = j;
          }
        }
      }

      let windowsReflectance = 0.0;
      let reflectanceValues = [
        ceilingReflectance,
        floorReflectance,
        wallsReflectance,
        windowsReflectance,
      ];

      // find maxDepth
      let maxDepth = 0.0;
      let maxContextDepth = 0.0;
      for (let i= 0; i < n_lat; ++i)
      {
          for (let j = 0; j < n_long; ++j)
          {
              let k = i * n_long + j;
              if (depthMap[k] > maxDepth) maxDepth = depthMap[k];
              if (contextDepth[k] > maxContextDepth) maxContextDepth = contextDepth[k];
          }
      }
      if (maxDepth == 0.0) alert("Invalid max depth (0.0), please check all input geometries and sensor positions.");


      for (let i = 0; i < n_lat; ++i) {
        for (let j = 0; j < n_long; ++j) {
          let k = i * n_long + j;

          // depthmap
          let d = depthMap[k] > 0 ? depthMap[k]/maxDepth : 0.0;   //  just in case, but should not be needed for closed geometries - properly modeled interior spaces

          // transparency: windows are geometries[3]
          let win = geomType[k] === 3 ? windowsTransparency : 0.0;

          // reflectance
          let refl = reflectanceValues[geomType[k]];

          // shading devices mask
          let shade = shadingMap[k] >= 0 ? shadingFactors[shadingMap[k]] : 0;

          // context depth
          let cd = maxContextDepth > 0.0 ? contextDepth[k] / maxContextDepth : contextDepth[k];

          sensorsResult[sensorCount][k][0] = d;  // [0-1]
          sensorsResult[sensorCount][k][1] = win;  // [0-1]
          sensorsResult[sensorCount][k][2] = refl;  // [0-1]
          sensorsResult[sensorCount][k][3] = shade;  // [0 - f], f ~< 4.0 based on training dataset parametrization, may vary if extreme proportions are used
          sensorsResult[sensorCount][k][4] = contextRefl[k];  // in theory [0-1] but currently {0.3, 0.4}
          sensorsResult[sensorCount][k][5] = cd;  // [0-1]
        }
      }

      sensorCount++;
    });

    // sensorsResult has shape [nSensors, nRays, nMaps]
    return sensorsResult;
  }
}
