<template>
  <v-card
    :key="alt.data.num"
    :style="[
      {
        display: 'flex',
        'flex-direction': 'column',
        margin: '10px',
        'background-color': '#d9d9d9x',
        
        border:
          alt.data.num === selectedOptionComparison ? 'red 2px solid' : 'none',
      },
    ]"
    width="280"
    height="492"
  >
    <div style="display: flex; flex-direction: row; justify-content: ">
      <v-radio :value="alt.data.num" />
      <span
        v-if="isCurrent"
        style="display: flex; flex-direction: row"
      >
        Current
        <span style="display: flex; position: absolute; right: 25px">
          <v-text-field
            v-model="save_alt_name"
            type="text"
            label="Name"
            style="width: 80px; flex-grow: 0; padding-top: 0; margin-left: 10px"
            density="compact"
            hide-details
            variant="outlined"
            clearable
          />
          <v-btn
            color="red"
            :icon="true"
            @click="saveAlt"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              width="2em"
              height="2em"
              viewBox="0 0 24 24"
            >
              <path
                fill="currentColor"
                d="M15 9H5V5h10m-3 14a3 3 0 0 1-3-3a3 3 0 0 1 3-3a3 3 0 0 1 3 3a3 3 0 0 1-3 3m5-16H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V7z"
              />
            </svg> </v-btn></span>
      </span>
      <span v-else>
        <span> {{ alt.data.num }} </span>
        <v-btn
          style="position: absolute; right: 50px"
          :icon="true"
          color="red"
          @click="deleteAlt(alt.data.num)"
        >
          <svg
            fill="#000000"
            width="1.5em"
            height="1.5em"
            version="1.1"
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 512 512"
            xmlns:xlink="http://www.w3.org/1999/xlink"
            enable-background="new 0 0 512 512"
          >
            <path
              fill="currentColor"
              d="M432.001,95.998h-48V32c0-17.672-14.328-32-32-32H159.999c-17.672,0-32,14.328-32,32v63.997h-48c-17.673,0-32,14.326-32,32  c0,17.673,14.327,32,32,32H80v320.007C79.999,497.675,94.324,512,111.994,512h288.012c17.67,0,31.994-14.324,31.994-31.994v-0.001  V159.998c17.673,0,32-14.327,32-32C464.001,110.324,449.674,95.998,432.001,95.998z M192,432c0,8.836-7.163,16-16,16  c-8.837,0-16-7.164-16-16V239.998c0-8.837,7.163-16,16-16c8.837,0,16,7.163,16,16V432z M192,64H320v31.997H192V64z M272,432  c0,8.836-7.163,16-16,16s-16-7.164-16-16V239.998c0-8.837,7.163-16,16-16s16,7.163,16,16V432z M352,432c0,8.836-7.163,16-16,16  c-8.837,0-16-7.164-16-16V239.998c0-8.837,7.163-16,16-16c8.837,0,16,7.163,16,16V432z"
            />
          </svg>
        </v-btn>
        <v-btn
          style="position: absolute; right: 25px"
          :icon="true"
          color="blue"
          @click="loadAlt(alt.data.num)"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            width="2em"
            height="2em"
            viewBox="0 0 24 24"
          >
            <path
              fill="currentColor"
              d="M12 18a6 6 0 0 1-6-6c0-1 .25-1.97.7-2.8L5.24 7.74A7.93 7.93 0 0 0 4 12a8 8 0 0 0 8 8v3l4-4l-4-4m0-11V1L8 5l4 4V6a6 6 0 0 1 6 6c0 1-.25 1.97-.7 2.8l1.46 1.46A7.93 7.93 0 0 0 20 12a8 8 0 0 0-8-8"
            />
          </svg>
        </v-btn>
      </span>
    </div>

    <v-img
      width="220"
      height="220"
      style="margin: 10px; flex-grow: 0; margin-top: 5px"
      :src="'data:image/jpeg;base64,' + alt.imageBytes"
    />
    <v-expansion-panels
      v-model="comparisionTabParamsExpansion"
      class="my-0 text-body-1"
      multiple
      style="overflow: auto"
    >
      <v-expansion-panel>
        <div style="text-align:center ;font-size: small;">
          {{ alt.timestamp }}
        </div>

        <v-expansion-panel-header style="padding-top: 5px; min-height: 30px !important;">
          Metrics
        </v-expansion-panel-header>
        <v-expansion-panel-content>
          <table style="width: -webkit-fill-available">
            <tr
              v-for="(v, k) in alt.metrics"
              :key="k"
              style="margin-bottom: 0px"
            >
              <td style="padding-right: 15px">
                {{ k }}:
              </td>
              <td style="padding-right: 15px">
                {{ v[0] }} {{ metricsUnits[k] }}
              </td>
              <td>
                {{
                  selectedBenchmark != null &&
                    alt.data.num !== selectedOptionComparison
                    ? benchmarkDiffs[alt.data.num][k]
                    : "..."
                }}
              </td>
            </tr>
          </table>
          <v-expansion-panel-content />
        </v-expansion-panel-content>
      </v-expansion-panel>
      <v-expansion-panel>
        <v-expansion-panel-header>Params</v-expansion-panel-header>
        <v-expansion-panel-content>
          <div style="margin: 10px; overflow: auto">
            <p
              v-for="(v, k) in alt.data"
              :key="k"
              style="margin-bottom: 0px"
            >
              {{ k }}: {{ v }}
            </p>
          </div>
        </v-expansion-panel-content>
      </v-expansion-panel>
    </v-expansion-panels>
  </v-card>
</template>
<script>
export default {
  name: "AltCard",
  props: {
    alt: {
      type: Object,
      required: true,
    },
    isCurrent: {
      type: Boolean,
      required: true,
      default: false,
    },
    metricsUnits: {
      type: Object,
      required: true,
    },
    selectedOptionComparison: {
      type: String,
      required: true,
    },
    selectedBenchmark: {
      type: Object,
      required: false,
      default: null,
    },
    benchmarkDiffs: {
      type: Object,
      required: true,
    },
  },
  data() {
    return {
      comparisionTabParamsExpansion: [0],
      save_alt_name: this.alt.data.num,
    };
  },
  watch: {
    comparisionTabParamsExpansion: {
      handler: function (newVal) {
        console.log("new comparisionTabParamsExpansion", newVal);
      },
    },
  },
  mounted() {},
  methods: {
    saveAlt() {
      if (window.Interop) {
        if (this.save_alt_name == null || this.save_alt_name == "") {
          this.save_alt_name = this.genHexString(5);
        }
        window.Interop.saveAlt(this.save_alt_name);

        this.$emit("on-save-alt");
      }
    },
    loadAlt(name) {
      if (window.Interop) {
        window.Interop.loadAlt(name);
      }
    },
    deleteAlt(name) {
      if (window.Interop) {
        window.Interop.deleteAlt(name);
        this.$emit("on-delete-alt");
      }
    },
  },
};
</script>

<style scoped></style>
