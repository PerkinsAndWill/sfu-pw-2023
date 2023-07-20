<template>
  <v-app>
    <v-main>
      <v-container v-if="!designModelTicket">
        <v-toolbar dense dark color="blue darken-3" class="mb-1">
        Choose a Design Model:</v-toolbar>
        <v-row justify="center">
          <v-col cols="5">
            <v-img
            style="border: 1px black solid;"
            :width="300"
            cover
            src="./assets/spyros_room.png"
            @click="designModelTicket = designModels['Spyros_Latest']"
          >
          </v-img>
          </v-col>
          <v-col cols="5">
            <v-img
            style="border: 1px black solid;"
            :width="300"
            cover
            src="./assets/esmaeil_room.png"
            @click="designModelTicket = designModels['Esmaeil_Latest']"
          >
          </v-img>
          </v-col>
 
        </v-row>
      </v-container>
      <v-container v-else>
        <MainComponent :designModelTicket="designModelTicket" />
      </v-container>
    </v-main>
  </v-app>
</template>

<script>
import MainComponent from "./components/MainComponent.vue";
import Vue from "vue";

export default Vue.extend({
  name: "App",
  data: function () {
    return {
      designModelTicket: null,
      designModels: {
        Spyros_Latest:
          "98fe8ed861d69330041c9661da3d61086606a6617fb3380fc21e0537ab66ead0002788fbc3c612ef6ed746d60d4ab961e6c284969f9b88dab326438326bb92b45710b415f5598c32fa7c32ae3f994fb1605e0c4bb59ed20af6c896cec5814e5e38e96ec231c7d0-eed48907401cc45c0ca457d3cbed7972",
        Esmaeil_Latest:
          "8465d13da7c2a145ea719192fd42b29a0fe66c8425c9ed801bbc07eba80def31ab792c641929e92f0b49fe3920a59f1a50cbfbdb2eec565ad686f7935ba118b261f4367831e9be034df9d9f50692070c3fd50923c59768821cfa9e34afd9bcba0aae896a2fc649-3a4c6e49c8204340a9894e3731a54dc0",
      },
    };
  },
  components: {
    MainComponent,
  },
});

// "8465d13da7c2a145ea719192fd42b29a0fe66c8425c9ed801bbc07eba80def31ab792c641929e92f0b49fe3920a59f1a50cbfbdb2eec565ad686f7935ba118b261f4367831e9be034df9d9f50692070c3fd50923c59768821cfa9e34afd9bcba0aae896a2fc649-3a4c6e49c8204340a9894e3731a54dc0",
// "98fe8ed861d69330041c9661da3d61086606a6617fb3380fc21e0537ab66ead0002788fbc3c612ef6ed746d60d4ab961e6c284969f9b88dab326438326bb92b45710b415f5598c32fa7c32ae3f994fb1605e0c4bb59ed20af6c896cec5814e5e38e96ec231c7d0-eed48907401cc45c0ca457d3cbed7972",
// "30b267f1fae22edae4ba3b534777166e6e96f648fd7165cf048cd5d6a8e6e4662118ed377dfaf0e79d4678c4cdf940f81304169e7751dfdf0ac628b071c37e87aa29f1e7bead3271312643c26ca51281d5045397a4961ef4608a7fd3a153af23431f03aa246ee6-37c8214b26644476a3301cc14c876c5a",
//'2670fa61e598730ba7fde7c216458a947cfbf5b376d22b5099b0b5606921c5f0e196d9e2fb24d472baea7fbaeae3b7026225e6f30dfbcefaa1d1e37f297eb61ffd7ff9f10b1da445ca1dc3735e8ca45912448b6dfe706186e1bd676b57af7866dcfbe33dbc6fb5-ed10e8c5299adbb0bd084362d9a7bffa',
//"1979a96ce6ca6a66d198df520d461be05d68e189af9fcc233e55b7d7c61d8f22b970db686730416d3729e96005317af274923a30760263f963e803e84871cbf05759b16de94a55f05cdde554918ab1cc3f0c80fd43be49488f7f55109e620e0c3652fbd4c6bbe3-3214df684b196a9264bd5cd0b4d78025",
//'6ed7e05204e02d49e8be6172a4af9e41c40c2435c62afd3f0a1c31fdf1076f62945e8681209be384b7a3ba6ea0b868fc5bf29eb5df26eec35b128269e2fa13f26b3039d2d149e92c07005357791484f7cf85047561e6a23e1d59e1c0e841a60a4309a85ca03ce3-dac913e8b63871ac1e10c3435c5755b6',
//'bc493c780f21368c43503eb4c28de9d554d6751102039c1458722fa0e32632203ae346a241169e3f4f82c1a8cf60f23ce7f02fba95cdac6eed3ac96c9b299237cb6ed5c25501bb3488aa466368bcd0d5c3f094debf79cabf3d589f5e7f0afd0205a501e752dbfe-35ccfe96a38ebed05510d5865ef7bd72',//'70888c4994249bb647fe7cff0baa10e7fd29183b75153af2a0e5f550d08d088cec41c0ef35ae4555cdee8a18280a850dc6e0111fbde54c6578a944eed09edd7509e38522086c01c9ad4d9c0969c4c58546461475c5533bfa0b3e9243a79da8b13bead63b1f5f52-e2f2b2b4a5ea58604a58e46fa5993410',
</script>

<style lang="scss"></style>
