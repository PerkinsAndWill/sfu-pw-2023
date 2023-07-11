// Notes on CodeSandBox
// if you don't see a preview when you load this page for the first time, reload the browser tab to the right
// if you get the error "Api.createViewer: Viewer with this id (myViewer) already exists." reload the browser tab to the right
import {
  createSession,
  createViewport,
  GeometryData,
  IGeometryData,
  IOutputApi,
  ITreeNode,
  MaterialEngine,
  SDTFItemData
} from "@shapediver/viewer";

const materialEngine: MaterialEngine = MaterialEngine.instance;


(async () => {
  // create a viewport
  const viewport = await createViewport({
    canvas: document.getElementById("canvas") as HTMLCanvasElement,
    id: "myViewport"
  });
  // create a session
  const session = await createSession({
    ticket: "6ed7e05204e02d49e8be6172a4af9e41c40c2435c62afd3f0a1c31fdf1076f62945e8681209be384b7a3ba6ea0b868fc5bf29eb5df26eec35b128269e2fa13f26b3039d2d149e92c07005357791484f7cf85047561e6a23e1d59e1c0e841a60a4309a85ca03ce3-dac913e8b63871ac1e10c3435c5755b6",
     // "8a05d1b1ebbb3e5ed7af4fbbf22cf6d6c3e0d22a7ae3776d45ef5e8089547069cf9ca2d58898e22908c7628b0089df26a502511e134831aba80d68f0c49d0cd0cedba5bf9f8b93d1bcbc96f50b8492687c487acba1eeae7d0163676af7065a92eb2c6f4775c9c6-514e9408b2e0195d002c556c5e74af35",
    modelViewUrl: "https://sdeuc1.eu-central-1.shapediver.com",
    id: "mySession"
  });
  console.log(session)
  const attributeDataCollection: {
    node: ITreeNode;
    data: SDTFItemData;
    geometryData: IGeometryData[];
  }[] = [];

  const materialOutputs = Object.values(session.outputs).filter(
    (o: IOutputApi) => o.material === undefined
  );
  const materialColors = materialOutputs.find(
    (o: IOutputApi ) => o.name === "depth map"
  ) ;

    console.log(materialOutputs);
    console.log(materialColors);

    const content = materialColors?.content;
  const colorMap =
    (await materialEngine.loadMap(content?content[0].data.bitmaptexture: {})) || undefined;
    console.log(colorMap?.image);

    document.getElementById("maps")?.appendChild(colorMap?.image as Node);

  // helper function to get all geometry data in a node
  const getGeometryData = (node: ITreeNode, geometryData: IGeometryData[]) => {
    for (let i = 0; i < node.data.length; i++) {
      if (node.data[i] instanceof GeometryData) {
        const data = <IGeometryData>node.data[i];
        geometryData.push(data);
      }
    }

    for (let i = 0; i < node.children.length; i++)
      getGeometryData(node.children[i], geometryData);
  };

  // helper function to get the geometryData and parentNode for each SDTFItemData
  const getItemData = (node: ITreeNode) => {
    for (let i = 0; i < node.data.length; i++) {
      if (node.data[i] instanceof SDTFItemData) {
        const data = <SDTFItemData>node.data[i];
        console.log(data)
        const prom = Promise.resolve(data.value).then((c) => console.log(c))

        const geometryData: GeometryData[] = [];
        getGeometryData(node, geometryData);
        attributeDataCollection.push({ node, data, geometryData });
      }
    }

    for (let i = 0; i < node.children.length; i++)
      getItemData(node.children[i]);
  };
  getItemData(session.node);
  console.log(attributeDataCollection);
})();
