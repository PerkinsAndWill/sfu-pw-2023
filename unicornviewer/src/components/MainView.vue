<template>
  <div>
    <v-container
      fluid
      title="Modeling"
      class="pa-1"
    >
      <v-tabs
        v-model="modelling_tab"
        color="deep-purple-accent-4"
        align-tabs="center"
      >
        <v-tab>Setup</v-tab>
        <v-tab>Explore</v-tab>
        <v-tab>Compare</v-tab>
        <v-tab>Analyze</v-tab>
      </v-tabs>

      <div class="loading-element">
        <VueElementLoading
          :active="showSpinnerSingleRun"
          spinner="spinner"
        />
      </div>

      <v-tabs-items v-model="modelling_tab">
        <v-tab-item>
          <v-container
            style="
              display: flex;
              flex-wrap: nowrap;
              justify-content: space-evenly;
              align-items: center;
            "
          >
            <div style="display: flex; align-items: center">
              <v-tooltip
                bottom
                open-delay="200"
              >
                <template #activator="{ on, attrs }">
                  <v-btn
                    class="text-none"
                    v-bind="attrs"
                    style="width: 120px"
                    :style="[
                      isZoneSet
                        ? { 'background-color': 'white' }
                        : { 'background-color': 'lightgray' },
                    ]"
                    v-on="on"
                    @click="setZone"
                  >
                    <svg
                      v-show="isZoneSet"
                      width="25px"
                      style="fill: green; margin-right: 10px"
                      xmlns="http://www.w3.org/2000/svg"
                      viewBox="0 0 24 24"
                    >
                      <title>check-outline</title>
                      <path
                        d="M19.78,2.2L24,6.42L8.44,22L0,13.55L4.22,9.33L8.44,13.55L19.78,2.2M19.78,5L8.44,16.36L4.22,12.19L2.81,13.55L8.44,19.17L21.19,6.42L19.78,5Z"
                      />
                    </svg>
                    Footprint
                  </v-btn>
                </template>
                <span>
                  The outline of the footprint. Exterior walls will be created
                  based on this.
                </span>
              </v-tooltip>
              <v-switch
                v-show="isZoneSet"
                v-model="isClipping"
                style="padding-left: 10px"
                label="Clip"
                @change="clipByFootprintInRhino"
              />
            </div>
            <v-tooltip bottom>
              <template #activator="{ on, attrs }">
                <v-btn
                  v-bind="attrs"
                  :style="[
                    isContextSet
                      ? { 'background-color': 'white' }
                      : { 'background-color': 'lightgray' },
                  ]"
                  class="text-none"
                  v-on="on"
                  @click="setContext"
                  @mouseenter="onHoverElements('Context', true)"
                  @mouseleave="onHoverElements('Context', false)"
                >
                  <svg
                    v-show="isContextSet"
                    width="25px"
                    style="fill: green; margin-right: 10px"
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                  >
                    <title>check-outline</title>
                    <path
                      d="M19.78,2.2L24,6.42L8.44,22L0,13.55L4.22,9.33L8.44,13.55L19.78,2.2M19.78,5L8.44,16.36L4.22,12.19L2.81,13.55L8.44,19.17L21.19,6.42L19.78,5Z"
                    />
                  </svg>

                  Context
                </v-btn>
              </template>
              <span>
                The buildings in the surrounding context. These will be included
                in the performance calculations.
              </span>
            </v-tooltip>
            <v-tooltip bottom>
              <template #activator="{ on, attrs }">
                <v-btn
                  class="text-none"
                  v-bind="attrs"
                  :style="[
                    isInteriorWallsSet
                      ? { 'background-color': 'white' }
                      : { 'background-color': 'lightgray' },
                  ]"
                  v-on="on"
                  @click="setInteriorWalls"
                >
                  <svg
                    v-show="isInteriorWallsSet"
                    width="25px"
                    style="fill: green; margin-right: 10px"
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                  >
                    <title>check-outline</title>
                    <path
                      d="M19.78,2.2L24,6.42L8.44,22L0,13.55L4.22,9.33L8.44,13.55L19.78,2.2M19.78,5L8.44,16.36L4.22,12.19L2.81,13.55L8.44,19.17L21.19,6.42L19.78,5Z"
                    />
                  </svg>

                  Interior Walls
                </v-btn>
              </template>
              <span> The interior walls.</span>
            </v-tooltip>
          </v-container>

          <v-expansion-panels
            v-model="setup_tab_expansion"
            multiple
          >
            <!--Weather File Panel -->
            <v-expansion-panel>
              <v-expansion-panel-header>
                Weather File & Precision
              </v-expansion-panel-header>
              <v-expansion-panel-content>
                <v-container>
                  <v-row>
                    <v-btn
                      :style="[
                        isWeatherFileSet
                          ? { 'background-color': 'white' }
                          : { 'background-color': 'lightgray' },
                      ]"
                      class="text-none"
                      @click="setWeatherFile"
                    >
                      <svg
                        v-show="isWeatherFileSet"
                        width="25px"
                        style="fill: green; margin-right: 10px"
                        xmlns="http://www.w3.org/2000/svg"
                        viewBox="0 0 24 24"
                      >
                        <title>check-outline</title>
                        <path
                          d="M19.78,2.2L24,6.42L8.44,22L0,13.55L4.22,9.33L8.44,13.55L19.78,2.2M19.78,5L8.44,16.36L4.22,12.19L2.81,13.55L8.44,19.17L21.19,6.42L19.78,5Z"
                        />
                      </svg>
                      Pick Weather File
                    </v-btn>
                    <span
                      v-if="isWeatherFileSet"
                      style="font-size: medium; margin-left: 5px;"
                    >
                      <em> {{ weatherFileLocation }} </em>
                    </span>
                  </v-row>
                  <v-row class="mt-10">
                    <v-select
                      v-model="gridSizeSelected"
                      label="Daylighting Grid Size"
                      density="compact"
                      :items="gridSizeData"
                      item-text="name"
                      return-object
                      outlined
                      @change="
                        updateBackendData(
                          'grid_size',
                          gridSizeSelected.index,
                          false
                        )
                      "
                    />
                  </v-row>
                </v-container>
              </v-expansion-panel-content>
            </v-expansion-panel>

            <!--Energy Analysis Setup Panel -->
            <v-expansion-panel>
              <v-expansion-panel-header>
                Building Inputs
              </v-expansion-panel-header>
              <v-expansion-panel-content>
                <v-container>
                  <v-select
                    v-model="buildingTypeSelected"
                    label="Building Type"
                    density="compact"
                    :items="buildingTypeData"
                    item-text="name"
                    return-object
                    outlined
                    @change="
                      updateBackendData(
                        'building_type',
                        buildingTypeSelected.index,
                        true
                      )
                    "
                  />
                  <v-select
                    v-model="terrainSelected"
                    label="Terrain"
                    density="compact"
                    :items="terrainData"
                    item-text="name"
                    return-object
                    outlined
                    @change="
                      updateBackendData('terrain', terrainSelected.index, true)
                    "
                  />
                  <v-slider
                    v-model="inputs.floor_to_floor"
                    :min="0"
                    :max="10"
                    :step="0.25"
                    show-ticks="always"
                    tick-size="4"
                    label="Floor to Floor [m]"
                    hide-details
                    @end="
                      updateBackendData(
                        'floor_to_floor',
                        inputs.floor_to_floor,
                        false
                      )
                    "
                  >
                    <template #append>
                      <v-text-field
                        v-model="inputs.floor_to_floor"
                        type="number"
                        style="width: 80px"
                        density="compact"
                        hide-details
                        variant="outlined"
                      />
                    </template>
                  </v-slider>
                  <v-slider
                    v-model="inputs.floor_to_floor"
                    :min="0"
                    :max="100"
                    :step="1"
                    show-ticks="always"
                    tick-size="4"
                    label="Numbert of Floors"
                    hide-details
                    @end="
                      updateBackendData(
                        'number_of_floors',
                        inputs.number_of_floors,
                        false
                      )
                    "
                  >
                    <template #append>
                      <v-text-field
                        v-model="inputs.number_of_floors"
                        type="number"
                        style="width: 80px"
                        density="compact"
                        hide-details
                        variant="outlined"
                      />
                    </template>
                  </v-slider>
                  <v-slider
                    v-model="inputs.footprint_offset"
                    :min="0"
                    :max="100"
                    :step="1"
                    show-ticks="always"
                    tick-size="8"
                    label="Footprint V. Offset from Ground [m]"
                    hide-details
                    @end="
                      updateBackendData(
                        'footprint_offset',
                        inputs.footprint_offset,
                        false
                      )
                    "
                  >
                    <template #append>
                      <v-text-field
                        v-model="inputs.footprint_offset"
                        type="number"
                        style="width: 80px"
                        density="compact"
                        hide-details
                        variant="outlined"
                      />
                    </template>
                  </v-slider>
                </v-container>
              </v-expansion-panel-content>
            </v-expansion-panel>

            <!-- Material properties (Energy) Panel-->
            <v-expansion-panel>
              <v-expansion-panel-header>
                Material Properties (Energy)
              </v-expansion-panel-header>
              <v-expansion-panel-content>
                <v-container>
                  <ParamSlider
                    model-name="wall_r_val"
                    :model-value="inputs.wall_r_val"
                    label="Wall R Value [IP]"
                    :min="0"
                    :max="40"
                    :step="1"
                    tooltip="The R-Value of the exterior walls"
                  />
                  <ParamSlider
                    model-name="roof_r_val"
                    :model-value="inputs.roof_r_val"
                    label="Roof R Value [IP]"
                    :min="0"
                    :max="40"
                    :step="1"
                    tooltip="The R-Value of the Roof"
                  />
                  <ParamSlider
                    model-name="ground_r_val"
                    :model-value="inputs.ground_r_val"
                    label="Ground R Value [IP]"
                    :min="0"
                    :max="40"
                    :step="0.1"
                  />
                  <ParamSlider
                    model-name="win_u_val"
                    :model-value="inputs.win_u_val"
                    label="Win U-Value [SI]"
                    :min="0"
                    :max="1"
                    :step="0.1"
                    tooltip="The U-Value of the glazing system"
                  />
                  <ParamSlider
                    model-name="win_shgc"
                    :model-value="inputs.win_shgc"
                    label="Win SHGC"
                    :min="0"
                    :max="1"
                    :step="0.1"
                    tooltip="The Solar Heat Gain Coefficient of the glazing system"
                  />
                </v-container>
              </v-expansion-panel-content>
            </v-expansion-panel>

            <!-- Material properties (Daylighting) Panel -->
            <v-expansion-panel>
              <v-expansion-panel-header>
                Material Properties (Daylighting)
              </v-expansion-panel-header>
              <v-expansion-panel-content>
                <v-container>
                  <ParamSlider
                    model-name="ceiling_reflectance"
                    :model-value="inputs.ceiling_reflectance"
                    label="Ceiling Reflectance [%]"
                    :min="0"
                    :max="1"
                    :step="0.1"
                  />
                  <ParamSlider
                    model-name="floor_reflectance"
                    :model-value="inputs.floor_reflectance"
                    label="Floor Reflectance [%]"
                    :min="0"
                    :max="1"
                    :step="0.1"
                  />
                  <ParamSlider
                    model-name="wall_reflectance"
                    :model-value="inputs.wall_reflectance"
                    label="Wall Reflectance [%]"
                    :min="0"
                    :max="1"
                    :step="0.1"
                  />
                  <ParamSlider
                    model-name="glazing_transparency"
                    :model-value="inputs.glazing_transparency"
                    label="Glazing VLT [%]"
                    :min="0"
                    :max="1"
                    :step="0.1"
                    tooltip="Glazing transparency"
                  />
                </v-container>
              </v-expansion-panel-content>
            </v-expansion-panel>
          </v-expansion-panels>
        </v-tab-item>
        <v-tab-item>
          <v-container class="panels_container">
            <!-- Walls Table-->
            <v-container>
              <v-container class="wwr_inputs_container">
                Select By Direction:
                <v-btn
                  small
                  class="analyze_button"
                  @click="selectWindowsInDirection(0)"
                >
                  S
                </v-btn>
                <v-btn
                  small
                  class="analyze_button"
                  @click="selectWindowsInDirection(1)"
                >
                  E
                </v-btn>
                <v-btn
                  small
                  class="analyze_button"
                  @click="selectWindowsInDirection(3)"
                >
                  W
                </v-btn>
                <v-btn
                  small
                  class="analyze_button"
                  @click="selectWindowsInDirection(2)"
                >
                  N
                </v-btn>
                <v-row
                  style="align-items: center; border-bottom: 1px gray solid"
                >
                  <v-col>
                    <v-checkbox
                      v-model="selectAllCheckbox"
                      density="compact"
                      hide-details
                      class="walls_checkboxes shrink mr-1 mt-1"
                      @change="onSelectAllCheckboxChange()"
                    />
                  </v-col>
                  <v-col> # Wall </v-col>

                  <v-col> WWR </v-col>
                  <v-col>Num. V.Shd.</v-col>
                  <v-col>V.Shd. Depth[m]</v-col>
                  <v-col>Num. H.Shd.</v-col>
                  <v-col>H.Shd. Depth[m]</v-col>
                  <v-col>Overhangs Depth[m]</v-col>
                  <v-col>Overhangs Offset[m]</v-col>
                </v-row>
                <v-container
                  :class="`${
                    numWalls <= 4
                      ? 'data_table_container'
                      : 'data_table_container large'
                  }`"
                >
                  <v-row
                    v-for="(v, i) in numWalls"
                    :key="i"
                    style="align-items: center; margin-top: 0"
                  >
                    <v-col>
                      <v-checkbox
                        :key="i"
                        v-model="selectedWallsCheckboxes[i]"
                        hide-details
                        class="walls_checkboxes shrink mr-0 mt-0"
                        @change="onWallCheckboxChanged(i, $event)"
                      />
                    </v-col>
                    <v-col style="display: flex; justify-content: space-around">
                      <div>Wall {{ v }}</div>
                    </v-col>
                    <v-col>
                      {{ inputs.WWR_per_wall[i] }}
                    </v-col>
                    <v-col>
                      {{
                        inputs.verticalShadings_multiplier[i] == 0
                          ? "-"
                          : inputs.verticalShadings_multiplier[i]
                      }}
                    </v-col>
                    <v-col>
                      {{
                        inputs.verticalShadings_multiplier[i] == 0
                          ? "-"
                          : inputs.verticalShadings_depth[i]
                      }}
                    </v-col>
                    <v-col>
                      {{
                        inputs.horizontalShadings_multiplier[i] == 0
                          ? "-"
                          : inputs.horizontalShadings_multiplier[i]
                      }}
                    </v-col>
                    <v-col>
                      {{
                        inputs.horizontalShadings_multiplier[i] == 0
                          ? "-"
                          : inputs.horizontalShadings_depth[i]
                      }}
                    </v-col>
                    <v-col>
                      {{
                        inputs.overhangs_depth[i] == 0
                          ? "-"
                          : inputs.overhangs_depth[i]
                      }}
                    </v-col>
                    <v-col>
                      {{ inputs.overhangs_offset[i] }}
                    </v-col>
                  </v-row>
                </v-container>
              </v-container>
            </v-container>
            <!-- Controls -->
            <v-container>
              <div style="padding: 10px">
                <v-btn
                  class="text-none"
                  @click="updateWWRShading"
                >
                  Apply to Selected Walls
                </v-btn>
                <v-slider
                  v-model="WWRInput"
                  :min="0"
                  :max="1"
                  :step="0.05"
                  show-ticks="always"
                  tick-size="4"
                  label="WWR"
                  hide-details
                >
                  <template #append>
                    <v-text-field
                      v-model="WWRInput"
                      type="number"
                      style="width: 40px"
                      class="ma-0 pa-0"
                      density="compact"
                      hide-details
                      variant="outlined"
                    />
                  </template>
                </v-slider>
                <!-- vShadingCountInput -->
                <v-expansion-panels>
                  <v-expansion-panel>
                    <v-expansion-panel-header>
                      <v-switch
                        v-model="enable_shading"
                        style="display: inline-block"
                        color="primary"
                        hide-details
                        class="ma-0 ml-4"
                        label="Shading Devices"
                      />
                    </v-expansion-panel-header>
                    <v-expansion-panel-content>
                      <v-radio-group
                        v-model="shadingDeviceType"
                        hide-details
                        row
                        mandatory
                      >
                        <template #label>
                          <div style="font-size: medium">
                            Shading Devices Type
                          </div>
                        </template>
                        <v-radio
                          label="Vertical"
                          value="Vertical"
                        />
                        <v-radio
                          label="Horizontal"
                          value="Horizontal"
                        />
                      </v-radio-group>
                      <v-slider
                        v-model="vShadingCountInput"
                        :disabled="
                          !enable_shading || shadingDeviceType != 'Vertical'
                        "
                        :min="0"
                        :max="10"
                        :step="1"
                        show-ticks="always"
                        tick-size="4"
                        label="Vertical Shading Count"
                        hide-details
                      >
                        <template #append>
                          <v-text-field
                            v-model="vShadingCountInput"
                            :disabled="
                              !enable_shading || shadingDeviceType != 'Vertical'
                            "
                            type="number"
                            style="width: 45px"
                            class="ma-0 pa-0"
                            density="compact"
                            hide-details
                            variant="outlined"
                          />
                        </template>
                      </v-slider>
                      <v-slider
                        v-model="vShadingDepthInput"
                        :disabled="
                          !enable_shading || shadingDeviceType != 'Vertical'
                        "
                        :min="0.1"
                        :max="3"
                        :step="0.05"
                        show-ticks="always"
                        tick-size="4"
                        label="Vertical Shading [m]"
                        hide-details
                      >
                        <template #append>
                          <v-text-field
                            v-model="vShadingDepthInput"
                            :disabled="
                              !enable_shading || shadingDeviceType != 'Vertical'
                            "
                            type="number"
                            style="width: 45px"
                            class="ma-0 pa-0"
                            density="compact"
                            hide-details
                            variant="outlined"
                          />
                        </template>
                      </v-slider>
                      <v-slider
                        v-model="hShadingCountInput"
                        :disabled="
                          !enable_shading || shadingDeviceType != 'Horizontal'
                        "
                        :min="0"
                        :max="10"
                        :step="1"
                        show-ticks="always"
                        tick-size="4"
                        label="Horizontal Shading Count"
                        hide-details
                      >
                        <template #append>
                          <v-text-field
                            v-model="hShadingCountInput"
                            :disabled="
                              !enable_shading ||
                                shadingDeviceType != 'Horizontal'
                            "
                            type="number"
                            style="width: 45px"
                            class="ma-0 pa-0"
                            density="compact"
                            hide-details
                            variant="outlined"
                          />
                        </template>
                      </v-slider>
                      <v-slider
                        v-model="hShadingDepthInput"
                        :disabled="
                          !enable_shading || shadingDeviceType != 'Horizontal'
                        "
                        :min="0.1"
                        :max="3"
                        :step="0.05"
                        show-ticks="always"
                        tick-size="4"
                        label="Horizontal Shading Depth [m]"
                        hide-details
                      >
                        <template #append>
                          <v-text-field
                            v-model="hShadingDepthInput"
                            :disabled="
                              !enable_shading ||
                                shadingDeviceType != 'Horizontal'
                            "
                            type="number"
                            style="width: 45px"
                            class="ma-0 pa-0"
                            density="compact"
                            hide-details
                            variant="outlined"
                          />
                        </template>
                      </v-slider>
                    </v-expansion-panel-content>
                  </v-expansion-panel>
                  <v-expansion-panel>
                    <v-expansion-panel-header>
                      <v-switch
                        v-model="enable_overhangs"
                        style="display: inline-block"
                        color="primary"
                        hide-details
                        class="ma-0 ml-4"
                        label="Overhangs"
                      />
                    </v-expansion-panel-header>
                    <v-expansion-panel-content>
                      <v-slider
                        v-model="overhangsOffsetInput"
                        :disabled="!enable_overhangs"
                        :min="0"
                        :max="2"
                        :step="0.05"
                        show-ticks="always"
                        tick-size="4"
                        label="Overhangs Offset [m]"
                        hide-details
                      >
                        <template #append>
                          <v-text-field
                            v-model="overhangsOffsetInput"
                            :disabled="!enable_overhangs"
                            type="number"
                            style="width: 45px"
                            class="ma-0 pa-0"
                            density="compact"
                            hide-details
                            variant="outlined"
                          />
                        </template>
                      </v-slider>
                      <v-slider
                        v-model="overhangsDepthInput"
                        :disabled="!enable_overhangs"
                        :min="0"
                        :max="1"
                        :step="0.05"
                        show-ticks="always"
                        tick-size="4"
                        label="Overhangs Depth [m]"
                        hide-details
                      >
                        <template #append>
                          <v-text-field
                            v-model="overhangsDepthInput"
                            :disabled="!enable_overhangs"
                            type="number"
                            style="width: 45px"
                            class="ma-0 pa-0"
                            density="compact"
                            hide-details
                            variant="outlined"
                          />
                        </template>
                      </v-slider>
                    </v-expansion-panel-content>
                  </v-expansion-panel>
                </v-expansion-panels>
              </div>
            </v-container>
            <!-- Daylighting Outputs-->
            <v-container>
              <h3 class="ma-0">
                <v-switch
                  v-model="inputs.enable_daylight"
                  style="display: inline-block"
                  color="primary"
                  hide-details
                  class="ma-0 ml-4"
                  @change="
                    updateBackendData(
                      'enable_daylight',
                      inputs.enable_daylight,
                      !inputs.enable_daylight
                    )
                  "
                />
                Daylighting
              </h3>

              <v-card
                v-if="da_all_metrics"
                style="display: flex; justify-content: space-around"
                flat
              >
                <v-tooltip
                  v-for="(title, i) in metricsTitles.slice(0, -2)"
                  :key="i"
                  open-delay="600"
                  bottom
                >
                  <template #activator="{ on, attrs }">
                    <v-btn
                      class="text-none"
                      v-bind="attrs"
                      v-on="on"
                      @click="switchDaylightMesh(i)"
                    >
                      {{ title }}: {{ da_all_metrics[i] }}
                      {{ metrics_units[title] }}
                    </v-btn>
                  </template>
                  <span v-html="daylightMetricsTooltip[i]" />
                </v-tooltip>
              </v-card>
              <v-card
                v-else
                style="padding: 15px"
                outlined
                center
                height="100px"
              >
                Daylighting not computed yet!
              </v-card>
            </v-container>
            <!-- Energy Outputs-->
            <v-container style="display: flex; flex-direction: column">
              <h3 class="ma-0">
                <v-switch
                  v-model="inputs.enable_energy"
                  style="display: inline-block"
                  color="primary"
                  hide-details
                  class="ma-0 ml-4"
                  @change="
                    updateBackendData(
                      'enable_energy',
                      inputs.enable_energy,
                      !inputs.enable_energy
                    )
                  "
                />Energy Needs
              </h3>

              <v-card
                v-if="energy_loads"
                flat
                height="100px"
                style="
                  display: flex;
                  flex-direction: row;
                  justify-content: space-around;
                "
              >
                <v-tooltip
                  bottom
                  open-delay="500"
                >
                  <template #activator="{ on, attrs }">
                    <div
                      v-bind="attrs"
                      style="
                        display: flex;
                        flex-direction: column;
                        padding: 15px;
                      "
                      v-on="on"
                    >
                      <div>{{ metricsTitles[4] }}:</div>
                      <div style="text-align: center">
                        {{ round2(energy_loads[0]) }} kWh/m2
                      </div>
                    </div>
                  </template>
                  <span v-html="daylightMetricsTooltip[4]" />
                </v-tooltip>
                <v-tooltip bottom>
                  <template #activator="{ on, attrs }">
                    <div
                      v-bind="attrs"
                      style="
                        display: flex;
                        flex-direction: column;
                        padding: 15px;
                      "
                      v-on="on"
                    >
                      <div>{{ metricsTitles[5] }}:</div>
                      <div style="text-align: center">
                        {{ round2(energy_loads[1]) }} kWh/m2
                      </div>
                    </div>
                  </template>
                  <span v-html="daylightMetricsTooltip[5]" />
                </v-tooltip>
                <div
                  style="display: flex; flex-direction: column; padding: 15px"
                >
                  <div>Total:</div>
                  <div style="text-align: center">
                    {{ round2(energy_loads[0] + energy_loads[1]) }} kWh/m2
                  </div>
                </div>
              </v-card>
              <v-card
                v-else
                outlined
                center
                height="100px"
              >
                Energy loads not computed yet!
              </v-card>
              <v-card
                outlined
                center
              >
                <StackedBarChart
                  :key="stacked_bar_key"
                  :raw-data="energy_total_loads_metrics_tree"
                  :categories="energy_categories"
                  :outer-width="600"
                  :outer-height="400"
                  :category-colors="energy_categories_colors"
                />
              </v-card>
            </v-container>
          </v-container>
        </v-tab-item>
        <v-tab-item class="comparision_tab">
          <div style="display: flex; justify-content: center">
            <v-select
              v-model="sortCompareAltsByMetric"
              label="Sort By:"
              density="compact"
              :items="['timestamp', ...metricsTitles]"
              item-text="name"
              style="margin-top: 10px; width: 200px; flex-grow: inherit"
              outlined
              @change="updateAlts"
            />
          </div>

          <v-container
            style="
              margin-left: 0px;
              margin-right: 0px;
              width: 100%;
              max-width: 100%;
            "
          >
            <v-radio-group
              key="comparision_alts_key"
              v-model="selected_option_comparison"
              class="comparision_radio_group"
            >
              <div>
                <AltCard
                  v-if="currentAlt != null && isZoneSet"
                  :alt="currentAlt"
                  :benchmark-diffs="benchmark_diffs"
                  :comparision-tab-params-expansion="
                    comparision_tab_params_expansion
                  "
                  :metrics-units="metrics_units"
                  :selected-benchmark="selected_benchmark"
                  :selected-option-comparison="selected_option_comparison"
                  :is-current="true"
                  @on-save-alt="updateAlts"
                />
              </div>
              <div
                v-for="alt in otherAlts"
                :key="alt.data.num"
              >
                <AltCard
                  :alt="alt"
                  :benchmark-diffs="benchmark_diffs"
                  :comparision-tab-params-expansion="
                    comparision_tab_params_expansion
                  "
                  :metrics-units="metrics_units"
                  :selected-benchmark="selected_benchmark"
                  :selected-option-comparison="selected_option_comparison"
                  :is-current="false"
                  @on-delete-alt="updateAlts"
                />
              </div>
            </v-radio-group>
          </v-container>
        </v-tab-item>
        <v-tab-item
          style="display: flex; flex-direction: column; align-items: center"
        >
          <div
            style="display: flex; flex-direction: column; align-items: center"
          >
            <p style="margin-top: 15px; font-size: medium; text-align: center">
              On this panel, you can run parametric analysis studies to
              understand the impact that different parameters have on the
              performance of your design.<br>
              1. First pick the parameters you which to analyze. The values for
              the unselected parameters may be based on the current alternative or custom picked values. The values set here will be applied to all walls.
            </p>
            <v-expansion-panels
              v-model="analyze_tab_expansion"
              style="padding: 10px"
              multiple
            >
              <v-expansion-panel
                v-for="paramType in ['Geometry', 'Material']"
                :key="paramType"
                style="text-align: center"
              >
                <v-expansion-panel-header>
                  {{ paramType }} Parameters
                </v-expansion-panel-header>
                <v-expansion-panel-content
                  class="align-center"
                  style="text-align: -webkit-center"
                >
                  <table>
                    <tr>
                      <td
                        colspan="2"
                        style="font-size:small"
                      >
                        Check the parameters to include:
                      </td>
                      <td />

                      <td style="font-size: small; text-align: center">
                        <v-tooltip bottom>
                          <template #activator="{ on, attrs }">
                            <div
                              style="
                                display: flex;
                                flex-direction: row;
                                align-items: center;
                              "
                            >
                              <svg
                                v-bind="attrs"
                                width="20"
                                height="20"
                                fill="none"
                                viewBox="0 0 24 24"
                                xmlns="http://www.w3.org/2000/svg"
                                v-on="on"
                              >
                                <path
                                  d="M23 12C23 18.0751 18.0751 23 12 23C5.92487 23 1 18.0751 1 12C1 5.92487 5.92487 1 12 1C18.0751 1 23 5.92487 23 12ZM3.00683 12C3.00683 16.9668 7.03321 20.9932 12 20.9932C16.9668 20.9932 20.9932 16.9668 20.9932 12C20.9932 7.03321 16.9668 3.00683 12 3.00683C7.03321 3.00683 3.00683 7.03321 3.00683 12Z"
                                  fill="#777777"
                                />
                                <path
                                  d="M13.5 18C13.5 18.8284 12.8284 19.5 12 19.5C11.1716 19.5 10.5 18.8284 10.5 18C10.5 17.1716 11.1716 16.5 12 16.5C12.8284 16.5 13.5 17.1716 13.5 18Z"
                                  fill="#777777"
                                />
                                <path
                                  d="M11 12V14C11 14 11 15 12 15C13 15 13 14 13 14V12C13 12 13.4792 11.8629 13.6629 11.7883C13.6629 11.7883 13.9969 11.6691 14.2307 11.4896C14.4646 11.3102 14.6761 11.097 14.8654 10.8503C15.0658 10.6035 15.2217 10.3175 15.333 9.99221C15.4443 9.66693 15.5 9.4038 15.5 9C15.5 8.32701 15.3497 7.63675 15.0491 7.132C14.7596 6.61604 14.3476 6.21786 13.8132 5.93745C13.2788 5.64582 12.6553 5.5 11.9427 5.5C11.4974 5.5 11.1021 5.55608 10.757 5.66825C10.4118 5.7692 10.1057 5.9094 9.83844 6.08887C9.58236 6.25712 9.36525 6.4478 9.18711 6.66091C9.02011 6.86281 8.8865 7.0591 8.78629 7.24978C8.68609 7.44046 8.61929 7.6087 8.58589 7.75452C8.51908 7.96763 8.49125 8.14149 8.50238 8.27609C8.52465 8.41069 8.59145 8.52285 8.70279 8.61258C8.81413 8.70231 8.9867 8.79765 9.22051 8.8986C9.46546 8.97712 9.65473 9.00516 9.78834 8.98273C9.93308 8.96029 10.05 8.89299 10.1391 8.78083C10.1391 8.78083 10.6138 8.10569 10.7474 7.97109C10.8922 7.82528 11.0703 7.71312 11.2819 7.6346C11.4934 7.54487 11.7328 7.5 12 7.5C12.579 7.5 13.0076 7.64021 13.286 7.92062C13.5754 8.18982 13.6629 8.41629 13.6629 8.93225C13.6629 9.27996 13.6017 9.56038 13.4792 9.77349C13.3567 9.9866 13.1953 10.1605 12.9949 10.2951C12.9949 10.2951 12.7227 10.3991 12.5 10.5C12.2885 10.5897 11.9001 10.7381 11.6997 10.8503C11.5104 10.9512 11.4043 11.0573 11.2819 11.2144C11.1594 11.3714 11 11.7308 11 12Z"
                                  fill="#777777"
                                />
                              </svg>
                              <div style="margin-left: 4px">
                                Num Steps
                              </div>
                            </div>
                          </template>
                          <span>
                            Num steps includes the min + max + steps in between.
                            <br>
                            So 2 steps means only min and max, and 3 steps adds
                            a point in the middle to that.
                          </span>
                        </v-tooltip>
                      </td>
                    </tr>
                    <tr
                      v-for="param in parametricAnalysisData.filter(
                        (x) => x.type === paramType
                      )"
                      :key="param.name"
                    >
                      <td>
                        <input
                          :id="param.name"
                          v-model="param.checked"
                          type="checkbox"
                          @change="updateParametricAnalysisData()"
                        >
                      </td>
                      <td style="font-size: small">
                        {{ param.title }}
                      </td>
                      <td
                        v-if="param.checked"
                        style="width: 180px; display: flex; flex-direction: row"
                      >
                        <v-text-field
                          v-model.number="param.range[0]"
                          type="number"
                          label="min"
                          hide-details
                        />

                        <v-text-field
                          v-model.number="param.range[1]"
                          type="number"
                          label="max"
                          hide-details
                        />
                      </td>
                      <td
                        v-if="param.checked"
                        style="font-size: small"
                      >
                        <v-select
                          v-show="param.checked"
                          v-model="param.size"
                          style="width: 70px; margin-left: 5px"
                          density="compact"
                          hide-details
                          :items="[2, 3, 4, 5]"
                          outlined
                          @change="updateParametricAnalysisData()"
                        />
                      </td>
                      <td 
                        v-else
                        colspan="2"
                        style="width: 180px; color: grey; font-size: x-small"
                      >
                        <v-radio-group
                          v-model="paramAnalysisDefaultValueOption"
                          class="paramAnalysisDefaultRadioGroup"
                          hide-details
                          row
                          mandatory
                        >
                          <template #label>
                            <div style="font-size: small">
                              Values to use:
                            </div>
                          </template>
                          <v-radio
                            label="Same as the current alt."
                            :value="true"
                            style="font-size: x-small"
                          />
                          <v-radio
                            label="Override default value"
                            :value="false"
                            style="font-size: x-small"
                          />
                          <v-text-field
                            v-show="!paramAnalysisDefaultValueOption"
                            
                            label="New Value"
                            type="number"
                            style="width: 40px;"
                          />
                        </v-radio-group>
                      </td>
                    </tr>
                  </table>
                </v-expansion-panel-content>
              </v-expansion-panel>
            </v-expansion-panels>
            <v-divider style="max-height: 10px; width:100%; margin-top: 5px; margin-bottom: 10px;" />
            <p style="font-size: medium">
              2. Then run the analysis. You can run an estimate first to get a
              sense of how long the whole process will take.
            </p>
            <div
              style="display: flex; flex-direction: row; align-items: baseline"
            >
              <div
                style="
                  display: flex;
                  flex-direction: column;
                  align-items: center;
                "
              >
                <v-radio-group
                  v-model="isUsingFullFactorialSampling"
                  row
                  mandatory
                  hide-details
                >
                  <template #label>
                    <div>Sampling Type</div>
                  </template>
                  <v-radio
                    label="Factorial"
                    :value="true"
                  />
                  <v-radio
                    label="LHS"
                    :value="false"
                  />
                </v-radio-group>
                <div style="font-size: medium">
                  Total Size: {{ parametricAnalysisTotalNumSamples }}
                </div>
                <v-progress-linear
                  v-if="parametricAnalysisProgress != -1"
                  style="width: 200px"
                  stream
                  :value="parametricAnalysisProgress"
                />
                <div
                  v-if="parametricAnalysisProgress != -1"
                  style="font-size: medium"
                >
                  Time to finish:
                  {{
                    parametricAnalysisProgress == 0
                      ? "..."
                      : parametricAnalysisTimeEstimate > 60
                        ? parametricAnalysisTimeEstimate / 60 + " mins"
                        : parametricAnalysisTimeEstimate + " secs"
                  }}
                </div>
                <div
                  v-show="!isUsingFullFactorialSampling"
                  style="
                    display: flex;
                    flex-direction: row;
                    align-items: baseline;
                  "
                >
                  <v-text-field
                    v-model="parametricAnalysisNumSamples"
                    label="Num Samples"
                    type="number"
                  />
                  <div>
                    ({{
                      round2(
                        (100 * parametricAnalysisNumSamples) /
                          parametricAnalysisTotalNumSamples
                      )
                    }}%)
                  </div>
                </div>
              </div>
              <div
                style="
                  display: flex;
                  flex-direction: column;
                  align-items: center;
                  padding-left: 10px;
                  border-left: black solid 1px;
                "
              >
                <div
                  style="
                  display: flex;
                  flex-direction: row;
                  align-items: center;"
                >
                  <v-tooltip bottom>
                    <template #activator="{ on, attrs }">
                      <v-btn
                        class="text-none"
                        v-bind="attrs"
                        v-on="on"
                        @click="runParametricAnalysis(true)"
                      >
                        Estimate
                        <VueElementLoading
                          :active="showSpinnerParametricAnalysisEstimate"
                          spinner="spinner"
                        />
                      </v-btn>
                    </template>
                    <span>
                      Runs a single sample to get an estimate for how long will
                      all samples take
                    </span>
                  </v-tooltip>
                  <v-tooltip bottom>
                    <template #activator="{ on, attrs }">
                      <v-btn
                        v-bind="attrs"
                        :disabled="
                          parametricAnalysisProgress != -1 ||
                            parametricAnalysisDataSelected.length <= 1
                        "
                        style="width: 200px"
                        class="analyze_button text-none"
                        variant="plain"
                        v-on="on"
                        @click="runParametricAnalysis()"
                      >
                        <span v-show="!showSpinnerParametricAnalysis">Run Analysis</span>
                        <VueElementLoading
                          :active="showSpinnerParametricAnalysis"
                          spinner="spinner"
                        />
                      </v-btn>
                    </template>
                    <span>
                      Runs the full analysis.
                    </span>
                  </v-tooltip>
                </div>
                <span style="font-size: medium">
                  Single Sample Time:
                  {{
                    parametricAnalysisLatestDiff > 0
                      ? round2(parametricAnalysisLatestDiff / 1000)
                      : "--"
                  }}
                  Secs
                </span>

                <div style="font-size: medium">
                  Total time needed:
                  {{
                    parametricAnalysisLatestDiff > 0
                      ? Math.round(
                        (((isUsingFullFactorialSampling
                          ? parametricAnalysisTotalNumSamples
                          : parametricAnalysisNumSamples) *
                          (parametricAnalysisLatestDiff / 1000)) /
                          60 +
                          Number.EPSILON) *
                          100
                      ) / 100
                      : "--"
                  }}
                  mins
                </div>
              </div>
            </div>
            <v-divider style="max-height: 10px; width:100%; margin-top: 5px; margin-bottom: 10px;" />
            <div
              style="
                display: flex;
                flex-direction: column;
                align-items: center;
                padding: 10px;
              "
            >
              <div
                style="
                  display: flex;
                  flex-direction: column;
                  align-items: center;
                  padding: 10px;
                "
              >
                <p style="font-size: medium">
                  3. When the analysis is completed, charts will be shown below. <br>
                  The bar charts indicate which parameters have the highest impact on
                  each performance metric. The Design Explorer can be used to explore and filter the design space.
                </p>
                <div>
                  <v-select
                    v-model="currentAnalysisName"
                    label="Select Analysis"
                    :items="sortedAnalysisFolders"
                    sort
                  />
                  <v-btn
                    style="width: 200px"
                    class="analyze_button text-none"
                    variant="plain"
                    @click="visualizeAnalysis()"
                  >
                    Visualize
                  </v-btn>
                  <v-btn
                    style="width: 200px"
                    class="analyze_button text-none"
                    variant="plain"
                    @click="openAnalysisFolder()"
                  >
                    Analysis Folder
                  </v-btn>
                </div>
              </div>
              <div
                style="
                  display: flex;
                  flex-direction: row;
                  flex-wrap: wrap;
                  margin-top: 20px;
                "
              >
                <DivergingBarChart
                  v-for="o in parametricAnalysisSortedFilteredSensitivites"
                  :key="o.id"
                  :title="o.name"
                  :data="o.children"
                />
              </div>
            </div>
          </div>
          <iframe
            v-show="showAnalysisVisualization"
            id="visualize_iframe"
            src="DesignExplorer-gh-pages\index.html"
            width="1200"
            height="800"
          />
          <div v-if="debug">
            <div>{{ parametricAnalysisSamples }}</div>
            <div>{{ parametricAnalysisModelsSlopes }}</div>
            <div>{{ parametricAnalysisModelsIntercepts }}</div>
            <div>{{ parametricAnalysisModelsCorrelations }}</div>
            <div>{{ parametricAnalysisSortedFilteredSensitivites }}</div>
            <div
              v-for="param in Object.keys(parametricAnalysisModelsSlopes)"
              :key="param"
              style="display: flex; flex-direction: row; align-items: center"
            >
              <div style="width: 85px; font-size: small">
                {{ param }}
              </div>
            </div>
            <v-slider
              v-model="notableSlopeThreshold"
              :min="0"
              :max="1"
              :step="0.05"
              show-ticks="always"
              tick-size="4"
              label="Slope Threhold"
              hide-details
              style="width: 300px"
              @end="updateMaxParametricAnalysisRegression"
            >
              <template #append>
                <v-text-field
                  v-model="notableSlopeThreshold"
                  type="number"
                  style="width: 40px"
                  class="ma-0 pa-0"
                  density="compact"
                  hide-details
                  variant="outlined"
                />
              </template>
            </v-slider>
          </div>
        </v-tab-item>
      </v-tabs-items>
    </v-container>
  </div>
</template>

<script>
import VueElementLoading from "vue-element-loading";
import ParamSlider from "./ParamSlider";
import StackedBarChart from "./StackedBarChart";
import DivergingBarChart from "./DivergingBarChart";
import Vue from "vue";
import AltCard from "./AltCard.vue";

export default {
  components: {
    VueElementLoading,
    ParamSlider,
    StackedBarChart,
    DivergingBarChart,
    AltCard,
  },
  data() {
    return {
      debug: false,
      //Inputs
      inputs: {
        enable_energy: true,
        enable_daylight: true,
        WWR_per_wall: [0.5, 0, 0, 0],

        verticalShadings_multiplier: [0, 0, 0, 0],
        verticalShadings_depth: [0.0, 0.0, 0.0, 0.0],

        horizontalShadings_multiplier: [0, 0, 0, 0],
        horizontalShadings_depth: [0.0, 0.0, 0.0, 0.0],

        overhangs_offset: [0, 0, 0, 0],
        overhangs_depth: [0.0, 0.0, 0.0, 0.0],

        verticalFnOnOff: [0, 0, 0, 0],
        horizontalFnOnOff: [0, 0, 0, 0],
        overhangsOnOff: [0, 0, 0, 0],

        height: 3.2,

        ceiling_reflectance: 0.7,
        floor_reflectance: 0.2,
        wall_reflectance: 0.4,
        glazing_transparency: 0.3,
        grid_size: 2,
        floor_num: 0,

        terrain: 0,
        heat_capacity: 0,
        heat_source: 0,
        hot_water_source: 0,
        building_type: 7,
        floor_to_floor: 3.2,
        footprint_offset: 0,
        number_of_floors: 20,

        wall_r_val: 30,
        roof_r_val: 20,
        ground_r_val: 20,
        win_u_val: 2.2,
        win_shgc: 0.3,
      },
      // Support Data
      metrics_units: {
        sDA: "%",
        ASE: "%",
        UDI: "",
        UDIa: "%",
        MI: "lux",
        Cooling: "kwh",
        Heating: "kwh",
        Lighting: "kwh",
        Equipment: "kwh",
      },
      floorNumData: [
        { index: 0, floorName: "Single Floor" },
        { index: 1, floorName: "Ground Floor" },
        { index: 2, floorName: "Mid Floor" },
        { index: 3, floorName: "Top Floor" },
      ],
      floorNumSelected: { index: 0, name: "Single Floor" },

      heatingSourceData: [
        {
          index: 0,
          name: "Electricity",
        },
        {
          index: 1,
          name: "Natural Gas",
        },
        {
          index: 2,
          name: "Fuel",
        },
      ],
      heatingSourceSelected: {
        index: 0,
        name: "Electricity",
      },
      hotWaterSourceSelected: {
        index: 0,
        name: "Electricity",
      },

      heatCapacitySelected: { index: 0, name: "Very Light: 80,000 * Af" },
      heatCapacityData: [
        {
          index: 0,
          name: "Very Light: 80,000 * Af",
        },
        {
          index: 1,
          name: "Light : 110,000 * Af",
        },
        {
          index: 2,
          name: "Medium: 165,000 * Af",
        },
        {
          index: 3,
          name: "Heavy: 260,000 * Af",
        },
        {
          index: 4,
          name: "Very heavy: 370,000 * Af",
        },
      ],

      terrainSelected: { index: 0, name: "Open terrain" },
      terrainData: [
        {
          index: 0,
          name: "Open terrain",
        },
        {
          index: 1,
          name: "Country",
        },
        {
          index: 2,
          name: "Urban / City",
        },
      ],

      gridSizeSelected: { index: 2, name: "Large" },
      gridSizeData: [
        {
          index: 0,
          name: "Small",
        },
        {
          index: 1,
          name: "Medium",
        },
        {
          index: 2,
          name: "Large",
        },
      ],

      buildingTypeData: [
        {
          index: 0,
          name: "LargeOffice",
        },
        {
          index: 1,
          name: "MediumOffice",
        },
        {
          index: 2,
          name: "SmallOffice",
        },
        {
          index: 3,
          name: "MidriseApartment",
        },
        {
          index: 4,
          name: "HighriseApartment",
        },
        {
          index: 5,
          name: "PrimarySchool",
        },
        {
          index: 6,
          name: "SecondarySchool",
        },
        {
          index: 7,
          name: "SmallHotel",
        },
        {
          index: 8,
          name: "LargeHotel",
        },
      ],
      buildingTypeSelected: {
        index: 5,
        name: "PrimarySchool",
      },

      // UI
      modelling_tab: null,
      comparision_tab_params_expansion: [0],
      setup_tab_expansion: [0],
      analyze_tab_expansion: [0],
      paramOpenPanels: [0],
      showSpinnerSingleRun: true,
      showSpinnerParametricAnalysis: false,
      showSpinnerParametricAnalysisEstimate: false,

      isClipping: false,

      areWallsSelected: false,
      numWindowsInput: 0,
      WWRInput: 0.5,
      vShadingCountInput: 0,
      hShadingCountInput: 0,
      vShadingDepthInput: 0.65,
      hShadingDepthInput: 0.65,
      overhangsOffsetInput: 0,
      overhangsDepthInput: 0,
      enable_shading: false,
      enable_overhangs: false,
      shadingDeviceType: "Vertical",

      wallDirections: [],
      selectedWallsNums: [],
      selectedWallsNumsSorted: [],
      isContextSet: false,
      isZoneSet: false,
      isInteriorWallsSet: false,
      isWeatherFileSet: true,
      weatherFileLocation: "Vancouver.Harbour.CS, BC, CAN",
      numWalls: 4,
      selectedWallsCheckboxes: [false, false, false, false],
      selectAllCheckbox: false,
      selected_option_comparison: "",

      analysisFolders: [],
      currentAnalysisName: null,
      showAnalysisVisualization: false,

      // Outputs
      da_metrics: [],
      da_all_metrics: [],
      da_average: null,
      energy_loads: [0, 0, 0, 0],
      metricsTitles: ["sDA", "ASE", "UDIa", "MI", "Heating", "Cooling"],
      daylightMetricsTooltip: [
        `Spatial Daylight Autonomy in % <br/>
                                Percentage of space that receives more than 300 lux for at least 50% of the time.<br/>
                                If a sensor in space receives more than 300 lux for at least 50 % of time, the sensor color will be light green.<br/>
                                For LEED® projects, scoring varies by building type; higher scores equals more credits.<br/>
                                sDA300/50% (300 lux for 50% of annual hours)<br/>
                                40% = 1 credit<br/>
                                55% = 2 credits<br/>
                                75% = 3 credits`,
        `Aunual Sunlight Exposure in %<br/>
                                the percentage of space that receives too much direct sunlight (1000 Lux or more for at least 250 occupied hours per year), which can cause glare or increased cooling loads.<br/>
                                If a sensor in space receives more than 1000 lux for at least 250 hours per year, the sensor color will be yellow.<br/>
                                If ASE is > 10%, glare conditions must be addressed, or FAIL score for all LEED daylighting credits.<br/>
                                LEED Score (0-1)`,
        `Useful Daylight Index (UDI) metrics.<br/>
                                UDIf (Failing) : percentage of time that the sensor receives below 100 lux.<br/>
                                UDIs (Supplementary) : percentage of time that the sensor receives between 100 lux and 300 lux.<br/>
                                UDIa (Acceptable) : percentage of time that the sensor receives between 300 lux and 3000 lux.<br/>
                                UDIe (Excessive): percentage of time that the sensor receives above 3000 lux.`,
        `Mean Illuminance in lux<br/>
                                The quantity of light emitted by a source that falls on a surface.<br/>
                                Illuminance is measured in lux (lumens per m2)`,
        "Heating Needs",
        "Cooling Needs",
      ],
      energy_categories: ["Heating", "Cooling"], //, "Lighting", "Equipment"],
      energy_categories_colors: ["#e04c24", "#0492d0", "#f9ef83", "#8bc56e"],
      energy_total_loads_metrics_tree: [
        [0, 0, 0, 0],
        [0, 0, 0, 0],
        [0, 0, 0, 0],
        [0, 0, 0, 0],
        [0, 0, 0, 0],
        [0, 0, 0, 0],
        [0, 0, 0, 0],
        [0, 0, 0, 0],
        [0, 0, 0, 0],
        [0, 0, 0, 0],
        [0, 0, 0, 0],
        [0, 0, 0, 0],
      ],
      wwr_metric: [],
      alts: [],
      currentAlt: null,
      otherAlts: [],
      sortCompareAltsByMetric: "sDA",
      selected_benchmark: null,
      benchmark_diffs: {},
      parametricAnalysisProgress: -1,
      parametricAnalysisLastTime: 0,
      parametricAnalysisLatestDiff: 0,
      parametricAnalysisTimeEstimate: 0,
      maxParametricAnalysisRegressionIndices: {},
      parametricAnalysisSortedSensitivites: [],
      parametricAnalysisSensitivitesLimit: 4,
      notableSlopeThreshold: 0.1,
      parametricAnalysisModelsSlopes: {
        WWR_per_wall: [
          43.778824110350776, 0, 8.907560748323837, -1961.4087574928974,
          4194587.054981265, 3928199.9284903575,
        ],
        floor_to_floor: [
          -0.35119755299255656, 0, 0.8368516134685127, -1580.7065360638483,
          137490.97353562262, 128806.42509904772,
        ],
        wall_r_val: [
          -20.142288821643625, 0, 8.70861589824194, -15416.234490775505,
          -4267987.1961912, -3995851.144375143,
        ],
        win_shgc: [
          33.736629496884476, 0, 4.473093549338037, 17950.222903054542,
          -50153.389862019685, -45612.1631591147,
        ],
      },
      parametricAnalysisModelsIntercepts: {
        WWR_per_wall: [
          69.80032717305006, 0, 36.685114306014675, 8882.63666213406,
          -1312855.6851038332, -1228933.7496189694,
        ],
        floor_to_floor: [
          93.16575452851657, 0, 36.91551671042453, 15786.643544188268,
          73520.28803454456, 69162.3817541264,
        ],
        wall_r_val: [
          101.70300707410411, 0, 36.63598435526281, 15786.98773368455,
          2937865.779718896, 2751280.284209901,
        ],
        win_shgc: [
          74.42062284672588, 0, 38.82985113582263, -1128.9054951861845,
          783540.4789240203, 733642.6471931795,
        ],
      },

      isUsingFullFactorialSampling: true,
      parametricAnalysisModelsCorrelations: {},
      // parametricAnalysisModelsSlopes: {},
      // parametricAnalysisModelsIntercepts: {},
      /*regressionModelsSlopes: { WWR_per_wall: { 0: [0, 0, 0, 0, 0, 0], 1: [0, 0, 0, 0, 0, 0] } },
      regressionModelsIntercepts: { WWR_per_wall: { 0: [0, 0, 0, 0, 0, 0], 1: [0, 0, 0, 0, 0, 0] } },*/
      regressionModelsSlopes: {
        WWR_per_wall: {
          0: [0, 18, 18, 0, -8.6, 499],
          1: [0, 30, 38, 0, 0, 0],
          2: [0, 0, 24, 0, 0, 0],
          3: [0, 30, 38, 0, 0, 0],
        },
      },
      regressionModelsIntercepts: {
        WWR_per_wall: {
          0: [0, 7, 17, 0, 47, 47],
          1: [0, 0.33, 15.3, 0, 56, 60.5],
          2: [0, 0, 10, 0, 56, 60.5],
          3: [0, 0.33, 16.3, 0, 56, 60.5],
        },
      },
      sparklines_key: 0,
      stacked_bar_key: 333,
      parametric_analysis_sparklines_key: 666,
      comparision_alts_key: 1234,
      maxRegressionIndices: [[0, 0, 0, 0, 0, 0]],
      parametricAnalysisParamType: "Geometry",

      parametricAnalysisTotalNumSamples: 0,

      parametricAnalysisNumSamples: 0,
      parametricAnalysisMinNumSamples: 16,
      parametricAnalysisMaxNumSamples: 150,
      paramAnalysisDefaultValueOption: true,
      parametricAnalysisDataSelected: [],
      parametricAnalysisData: [
        //Geometry Params
        {
          title: "Window-to-Wall Ratio",
          name: "WWR_per_wall",
          range: [0.3, 0.7],
          type: "Geometry",
          focus: 0,
          checked: true,
          size: 2,
        },
        /*
        {
          title: "Window-to-Wall Ratio (S)",
          name: "WWR_per_wall (S)",
          range: [0, 1],
          type: "Geometry",
          focus: 0,
          checked: false,
          size: 2,
        },
        {
          title: "Window-to-Wall Ratio (E)",
          name: "WWR_per_wall (E)",
          range: [0, 1],
          type: "Geometry",
          focus: 0,
          checked: false,
          size: 2,
        },
        {
          title: "Window-to-Wall Ratio (N)",
          name: "WWR_per_wall (N)",
          range: [0, 1],
          type: "Geometry",
          focus: 0,
          checked: false,
          size: 2,
        },
        {
          title: "Window-to-Wall Ratio (W)",
          name: "WWR_per_wall (W)",
          range: [0, 1],
          type: "Geometry",
          focus: 0,
          checked: false,
          size: 2,
        },
        */
        {
          title: "Floor to Floor [m]",
          name: "floor_to_floor",
          range: [2.4, 5],
          type: "Geometry",
          focus: 0,
          checked: true,
          size: 2,
        },

        {
          title: "Vertical Shading Count",
          name: "verticalShadings_multiplier",
          range: [0, 10],
          type: "Geometry",
          focus: 0,
          checked: false,
          size: 2,
          isInt: true,
        },
        {
          title: "Vertical Shading Depth [m]",
          name: "verticalShadings_depth",
          range: [0, 1],
          type: "Geometry",
          focus: 0,
          checked: false,
          size: 2,
        },
        {
          title: "Horizontal Shading Count",
          name: "horizontalShadings_multiplier",
          range: [0, 10],
          type: "Geometry",
          focus: 0,
          checked: false,
          size: 2,
          isInt: true,
        },
        {
          title: "Horizontal Shading Depth [m]",
          name: "horizontalShadings_depth",
          range: [0, 1],
          type: "Geometry",
          focus: 0,
          checked: false,
          size: 2,
        },
        {
          title: "Overhangs Offset",
          name: "overhangs_offset",
          range: [0, 2],
          type: "Geometry",
          focus: 0,
          checked: false,
          size: 2,
        },
        {
          title: "Overhangs Depth [m]",
          name: "overhangs_depth",
          range: [0, 1],
          type: "Geometry",
          focus: 0,
          checked: false,
          size: 2,
        },
        //Material Params
        {
          title: "Wall R-Value [IP]",
          name: "wall_r_val",
          range: [5, 40],
          type: "Material",
          focus: 1,
          checked: false,
          size: 2,
        },
        {
          title: "Roof R-Value [IP]",
          name: "roof_r_val",
          range: [20, 50],
          type: "Material",
          checked: false,
          focus: 1,
          size: 2,
        },
        {
          title: "Window U-Value [SI]",
          name: "win_u_val",
          range: [0.85, 2.55],
          type: "Material",
          focus: 1,
          checked: false,
          size: 2,
        },
        {
          title: "Window SHGC [%]",
          name: "win_shgc",
          range: [0.15, 0.55],
          type: "Material",
          focus: 1,
          checked: false,
          size: 2,
        },
        /* This value is fixed in the GH defintion so varying it here will have no impact
       {
          title: "Ground R-Value",
          name: "ground_r_val",
          range: [0, 1],
          type: "Material",
          focus: 1,
          checked: false,
          size: 2,
        },*/
        {
          title: "Ceiling Refl. [%]",
          name: "ceiling_reflectance",
          range: [0.6, 0.9],
          type: "Material",
          focus: 2,
          checked: false,
          size: 2,
        },
        {
          title: "Floor Refl. [%]",
          name: "floor_reflectance",
          range: [0.2, 0.4],
          type: "Material",
          focus: 2,
          checked: false,
          size: 2,
        },
        {
          title: "Wall Refl. [%]",
          name: "wall_reflectance",
          range: [0.4, 0.7],
          type: "Material",
          focus: 2,
          checked: false,
          size: 2,
        },
        {
          title: "Glazing VTL [%]",
          name: "glazing_transparency",
          range: [0.4, 0.7],
          type: "Material",
          focus: 2,
          checked: false,
          size: 2,
        },
      ],
    };
  },
  computed: {
    parametricAnalysisSamples() {
      var combinedSamples = [];

      if (this.parametricAnalysisDataSelected.length <= 1)
        return combinedSamples;

      if (this.isUsingFullFactorialSampling) {
        const samplesMatrix = this.parametricAnalysisDataSelected.map(
          (pData) => {
            const range = pData.range[1] - pData.range[0];
            const stepSize = range / (pData.size - 2 + 1);
            const samples = [];
            samples.push(pData.range[0]);
            for (var i = 0; i < pData.size - 2; i++) {
              var val = pData.range[0] + (i + 1) * stepSize;
              if (pData.isInt) {
                val = Math.trunc(val);
              }

              samples.push(val);
            }
            samples.push(pData.range[1]);
            return samples;
          }
        );

        const cartesian = (...a) =>
          a.reduce((a, b) => a.flatMap((d) => b.map((e) => [d, e].flat())));

        combinedSamples = cartesian(...samplesMatrix).map((arr) => {
          const dict = {};
          arr.forEach((d, i) => {
            dict[this.parametricAnalysisDataSelected[i].name] = d;
          });
          return dict;
        });
        console.log(
          "parametricAnalysisSamples",
          samplesMatrix,
          combinedSamples
        );
      } else {
        let numSamples = this.parametricAnalysisNumSamples;

        const normalizedSamples = this.randomLHS(
          numSamples,
          this.parametricAnalysisDataSelected.length
        );

        const samples = normalizedSamples.map((arr) =>
          arr.map((s, i) => {
            var input = {};
            input[this.parametricAnalysisDataSelected[i].name] =
              this.parametricAnalysisDataSelected[i].range[0] +
              s *
                (this.parametricAnalysisDataSelected[i].range[1] -
                  this.parametricAnalysisDataSelected[i].range[0]);
            return input;
          })
        );

        combinedSamples = samples.map((arr) => {
          var obj = {};
          arr.forEach((sam) => {
            var key = Object.keys(sam)[0];
            var value = sam[key];
            obj[key] = value;
          });
          return obj;
        });
        console.log(normalizedSamples, samples, combinedSamples);
      }

      // the constant values that will not be part of the parametric analysis, taken by defaults from the current alt values, i.e. this.inputs
      /*
      var currentAltValues = {};
      var allKeys = Object.keys(this.inputs)
      var selectedKeys = this.parametricAnalysisDataSelected.map(d => d.name)
      var unselectedKeys = allKeys.filter(k => !selectedKeys.includes(k))

      unselectedKeys.forEach(uk => {
        currentAltValues[uk]  = this.inputs[uk]
      });

      var appendedSamples = [];
      combinedSamples.forEach(sample => {
          var newSample = {...sample, ...currentAltValues};
          appendedSamples.push(newSample)
        })
      */

      return combinedSamples;
    },
    sortedAnalysisFolders() {
        return [...this.analysisFolders].sort().reverse();
    },
    parametricAnalysisSortedFilteredSensitivites() {
      let copy = JSON.parse(
        JSON.stringify(this.parametricAnalysisSortedSensitivites)
      );
      copy.forEach((d) => {
        d.children = d.children.slice(
          0,
          this.parametricAnalysisSensitivitesLimit
        );
      });

      return copy;
    },
  },
  watch: {
    sortedAnalysisFolders: {
      handler: function(newValue) {
        if(newValue && newValue.length > 0)
        {
          this.currentAnalysisName = newValue[0]
        }
        
      }
    },
    parametricAnalysisData: {
      handler: function () {
        this.updateParametricAnalysisData();
      },
      deep: true,
    },
    WWRInput: {
      handler: function () {},
    },
    vShadingCountInput: {
      handler: function () {},
    },
    vShadingDepthInput: {
      handler: function () {},
    },
    hShadingCountInput: {
      handler: function () {},
    },
    hShadingDepthInput: {
      handler: function () {},
    },
    overhangsOffsetInput: {
      handler: function () {},
    },
    overhangsDepthInput: {
      handler: function () {},
    },
    selected_option_comparison: {
      handler: function (newValue) {
        if (this.alts && this.alts.length > 0) {
          this.selected_benchmark = this.alts.find(
            (alt) => alt.data.num === newValue
          );

          var benchmarks = {};

          this.alts.forEach((alt) => {
            benchmarks[alt.data.num] = {};
            Object.keys(alt.metrics).forEach((k) => {
              if (this.selected_benchmark.data.num === alt.data.num) {
                //     benchmarks[alt.data.num][k] = 0
              } else {
                var v = alt.metrics[k][0];
                var benV = this.selected_benchmark.metrics[k][0];
                var sign = v - benV > 0;
                var percDiff = Math.round(
                  ((v - benV) / (benV == 0 ? 1 : benV)) * 100
                );
                benchmarks[alt.data.num][k] =
                  percDiff == 0
                    ? "-"
                    : percDiff + "% " + (sign ? "(↑)" : "(↓)");
                // console.log(alt.data.num,k,v, benV,benchmarks[alt.data.num][k]);
              }
            });
          });
          this.benchmark_diffs = benchmarks;
        }
      },
    },
    selectAllCheckbox: {
      handler: function (newValue) {},
    },
    selectedWallsCheckboxes: {
      handler: function (newValue) {},
    },
    selectedWallsNums: {
      handler: function (newValue) {
        this.selectedWallsNumsSorted = [...newValue].sort((a, b) => a - b);
      },
    },
    alts: {
      handler: function (newAlts) {
        this.currentAlt = newAlts.filter((obj) => obj.data.isCurrent)[0];
        this.otherAlts = newAlts.filter((obj) => !obj.data.isCurrent);
        console.log("alts handler", newAlts, this.sortCompareAltsByMetric);
        this.otherAlts.sort((x, y) => {
          if (this.sortCompareAltsByMetric === "timestamp") {
            return y.timestamp - x.timestamp;
          } else {
            return (
              y.metrics[this.sortCompareAltsByMetric][0] -
              x.metrics[this.sortCompareAltsByMetric][0]
            );
          }
        });
        this.$forceUpdate();
      },
      deep: true,
    },
    modelling_tab: {
      async handler(newVal) {
        if (newVal == 2) {
          // comparision tab
          this.updateAlts();
        }
        else if (newVal == 3) {
          // comparision tab
          this.updateAnalysisFolder();
        }

      },
    },
  },
  mounted() {
    //To make a JS function available to the C# plugin we register it on the window object. From there, the plugin can call it directly.
    window.recieveData = this.recieveData;
    window.recieveDataTrees = this.recieveDataTrees;
    window.updateUIData = this.updateUIData;
    window.updateParametricAnalysisModels = this.updateParametricAnalysisModels;
    window.updateMaxRegression = this.updateMaxRegression;
    window.setInputsData = this.setInputsData;
    window.setServerLoaded = this.setServerLoaded;

    window.showLoader = this.showLoader;
    window.updateParametricAnalysisProgress =
      this.updateParametricAnalysisProgress;
    window.setWWRShadingPerWall = this.setWWRShadingPerWall;
    window.setNumWalls = this.setNumWalls;

    window.setIsClipping = this.setIsClipping;
    window.updateBackendData = this.updateBackendData;
    window.onWallSelectedInRhino = this.onWallSelectedInRhino;
    window.updateAlts = this.updateAlts;
    window.updateCurrentAlt = this.updateCurrentAlt;

    window.visualizeAnalysisData = this.visualizeAnalysisData;

    this.updateParametricAnalysisData();
  },
  methods: {
    updateSelectedWalls() {
      var selected = [];
      this.selectedWallsCheckboxes.forEach((c, i) => {
        if (c) {
          selected.push(i);
        }
      });
      this.selectedWallsNums = selected;

      if (window.Interop) {
        var s = JSON.stringify(this.selectedWallsNums);
        console.log("selectWallsInRhino", s);
        window.Interop.selectWallsInRhino(s);
      }

      this.selectAllCheckbox = this.selectedWallsCheckboxes.every(
        (b) => b === true
      );
    },

    onWallCheckboxChanged(i, newValue) {
      //Vue.set(this.selectedWallsCheckboxes, i, newValue);
      console.log(
        "newValue",
        i,
        newValue,
        this.numWalls,
        this.selectedWallsCheckboxes
      );
      this.updateSelectedWalls();
    },
    switchDaylightMesh(daylightMeshIndex) {
      if (window.Interop) {
        window.Interop.switchDaylightMesh(daylightMeshIndex);
      }
    },
    updateParametricAnalysisData() {
      this.parametricAnalysisDataSelected = this.parametricAnalysisData.filter(
        (x) => x.checked
      );
      console.log("UPDATING", this.inputs, this.parametricAnalysisDataSelected);
      this.parametricAnalysisTotalNumSamples =
        this.parametricAnalysisDataSelected
          .map((x) => x.size)
          .reduce((x, y) => x * y, 1);

      this.$forceUpdate();
      //let numSamples =    Math.round(
      //  this.parametricAnalysisTotalNumSamples *
      //    this.parametricAnalysisDesignSpaceSize
      // );

      // to keep within the [min,max] range
      //this.parametricAnalysisNumSamples = Math.max(
      //  Math.min(numSamples, this.parametricAnalysisMaxNumSamples),
      //  this.parametricAnalysisMinNumSamples
      //);
    },
    visualizeAnalysis() {
      if (window.Interop && this.currentAnalysisName != null) {
        window.Interop.visualizeAnalysis(this.currentAnalysisName);
        this.showAnalysisVisualization = true;
      }
    },
    csvJSON(csv) {
      var lines = csv.split("\n");

      var result = [];

      // NOTE: If your columns contain commas in their values, you'll need
      // to deal with those before doing the next step
      // (you might convert them to &&& or something, then covert them back later)
      // jsfiddle showing the issue https://jsfiddle.net/
      var headers = lines[0].split(",");

      for (var i = 1; i < lines.length; i++) {
        var obj = {};
        var currentline = lines[i].split(",");

        for (var j = 0; j < headers.length; j++) {
          obj[headers[j]] = currentline[j];
        }

        result.push(obj);
      }

      //return result; //JavaScript object
      return result; //JSON
    },
    visualizeAnalysisData(data) {
      var visFrame = document.getElementById("visualize_iframe");

      const filteredData = this.csvJSON(data).filter((obj) => {
        return Object.values(obj).every((value) => value !== undefined);
      });
      console.log(
        visFrame,
        visFrame.contentWindow,
        data,
        this.csvJSON(data),
        filteredData
      );
      document.addEventListener
      if (visFrame != null) {
        visFrame.contentWindow.unloadPageContent();
        visFrame.contentWindow.loadDataToDesignExplorer(filteredData);
        visFrame.contentWindow.loadSetting();
        visFrame.contentWindow.addEventListener("loadToRhinoEvent", (e)=> {
          var args = e.detail.split("/");
          var altFullName = args[args.length - 1];
       
          var altName = altFullName.substring(0, altFullName.lastIndexOf('.'))

          console.log("load alt to rhino", e.detail, altName)
          if (window.Interop) {
            window.Interop.loadAlt(altName, "analysis\\"+this.currentAnalysisName);
          }
        })
      }
    },
    openAnalysisFolder() {
      if (window.Interop) {
        window.Interop.openAnalysisFolder();
      }
    },
    runParametricAnalysis(isEstimate) {
      let focusDict = {};
      this.parametricAnalysisDataSelected.forEach((x) => {
        focusDict[x.name] = x.focus;
      });

      if (window.Interop) {
        console.log(this.parametricAnalysisSamples);
        this.parametricAnalysisLastTime = Date.now();

        if (isEstimate) {
          const estimateSample = [
            {
              WWR_per_wall: 0.4,
              floor_to_floor: 3.2,
              verticalShadings_multiplier: 4,
            },
          ];

          window.Interop.runParametricAnalysisEstimate(
            JSON.stringify(estimateSample)
          );
        } else {
          window.Interop.runParametricAnalysis(
            JSON.stringify(this.parametricAnalysisSamples),
            JSON.stringify(focusDict)
          );
        }
      }
      if (!isEstimate) {
        this.updateMaxParametricAnalysisRegression();
      }
    },
    permutations(choices, callback, prefix) {
      if (!choices.length) {
        return callback(prefix);
      }
      for (var c = 0; c < choices[0].length; c++) {
        this.permutations(
          choices.slice(1),
          callback,
          (prefix || []).concat(choices[0][c])
        );
      }
    },

    round2(num) {
      return Math.round((num + Number.EPSILON) * 100) / 100;
    },
    round3(num) {
      return Math.round((num + Number.EPSILON) * 1000) / 1000;
    },
    clipByFootprintInRhino() {
      if (window.Interop) {
        window.Interop.clipByFootprint(JSON.stringify(this.isClipping));
      }
    },
    getInputCurrentValue(param) {
      if (param === "WWR_per_wall") {
        return this.WWRInput;
      } else if (param === "verticalShadings_multiplier") {
        return this.vShadingCountInput;
      } else if (param === "verticalShadings_depth") {
        return this.vShadingDepthInput;
      } else if (param === "horizontalShadings_multiplier") {
        return this.hShadingCountInput;
      } else if (param === "horizontalShadings_depth") {
        return this.hShadingDepthInput;
      } else if (param === "overhangs_offset") {
        return this.overhangsOffsetInput;
      } else if (param === "overhangs_depth") {
        return this.overhangsDepthInput;
      }
    },
    async updateAlts() {
      console.log("updateAlts");
      if (window.Interop) {
        this.alts = JSON.parse(await window.Interop.getAlts());
        this.comparision_alts_key += 1;
        this.$forceUpdate();
      }
    },
    async updateAnalysisFolder()
    {
      if (window.Interop) {
        console.log("update analysis folder");
        this.analysisFolders = JSON.parse(await window.Interop.getAnalysisFolders());

        
        this.$forceUpdate();
      }
    },
    async updateCurrentAlt() {
      if (window.Interop) {
        this.updateAlts();
        //var currentStoredAlt = JSON.parse(await window.Interop.getCurrentAlt());
        //const index = this.alts.indexOf((x) => x.data.isCurrent);
        //this.alts.splice(index, 1, currentStoredAlt);
      }
    },
    onSelectAllCheckboxChange() {
      console.log("onSelectAllCheckboxChange", this.selectAllCheckbox);
      this.selectAllWindows(this.selectAllCheckbox);
    },
    onWallSelectedInRhino(selectedWallsNums) {
      if (selectedWallsNums != null) {
        this.selectedWallsNums = JSON.parse(selectedWallsNums);
        this.selectedWallsCheckboxes.forEach((v, i) => {
          Vue.set(this.selectedWallsCheckboxes, i, false);
        });
        this.selectedWallsNums.forEach((v) => {
          Vue.set(this.selectedWallsCheckboxes, v, true);
        });

        if (this.selectedWallsNums.length > 0) {
          this.areWallsSelected = true;
          //Setting the values of the controls to that of the last selected wall.
          var lastWallNum =
            this.selectedWallsNums[this.selectedWallsNums.length - 1];
          this.WWRInput = this.inputs["WWR_per_wall"][lastWallNum];
          this.vShadingCountInput =
            this.inputs["verticalShadings_multiplier"][lastWallNum];
          this.vShadingDepthInput =
            this.inputs["verticalShadings_depth"][lastWallNum];
          this.hShadingCountInput =
            this.inputs["horizontalShadings_multiplier"][lastWallNum];
          this.hShadingDepthInput =
            this.inputs["horizontalShadings_depth"][lastWallNum];
          this.overhangsOffsetInput =
            this.inputs["overhangs_offset"][lastWallNum];
          this.overhangsDepthInput =
            this.inputs["overhangs_depth"][lastWallNum];

          console.log(
            "done setting",
            this.vShadingCountInput,
            this.vShadingDepthInput
          );
        } else {
          console.log("opps");
          this.areWallsSelected = false;
        }
      }
    },
    onHoverElements(element, hasEntered) {
      if (element == "Context") {
        console.log(element, hasEntered);
        if (window.Interop) {
          window.Interop.onContextHovered(hasEntered);
        }
      }
    },
    selectWindowsInDirection(direction) {

      var allWallsInDirectionSelected = this.selectedWallsCheckboxes.filter((w,i) => this.wallDirections[i] == direction).every(w => w)
      console.log(allWallsInDirectionSelected, this.selectedWallsCheckboxes, this.selectedWallsCheckboxes.filter((w,i) => this.wallDirections[i] == direction))
      this.selectedWallsCheckboxes.forEach((v, i) => {
        if(this.wallDirections[i] == direction)
        {
          Vue.set(this.selectedWallsCheckboxes, i, allWallsInDirectionSelected?false:true);
        }
      });

      this.updateSelectedWalls();
    },
    selectAllWindows(selectAll) {
      this.selectedWallsCheckboxes.forEach((v, i) => {
        Vue.set(this.selectedWallsCheckboxes, i, selectAll);
      });
      this.updateSelectedWalls();
    },
    updateNumWindows() {
      console.log(
        "updateing walls",
        this.selectedWallsNums,
        this.inputs["windows_per_wall"]
      );
      this.selectedWallsNums.forEach((i, v) => {
        console.log("looping", v, i);
        this.$set(
          this.inputs["windows_per_wall"],
          i,
          parseInt(this.numWindowsInput)
        );
        if (window.Interop) {
          window.Interop.unselectAllWalls();

          this.updateBackendData(
            "windows_per_wall",
            this.inputs["windows_per_wall"]
          );
        }
      });
    },
    updateWWRShading() {
      console.log(
        "UPDATE WWR PREEEEE",
        this.selectedWallsNums,
        this.overhangsOffsetInput,
        this.overhangsDepthInput,
        this.vShadingCountInput
      );

      this.selectedWallsNums.forEach((i, v) => {
        this.$set(this.inputs["WWR_per_wall"], i, this.WWRInput);

        if (this.enable_shading) {
          //
          if (
            this.shadingDeviceType === "Vertical" &&
            this.vShadingCountInput > 0
          ) {
            this.$set(this.inputs["verticalFnOnOff"], i, 1);
            this.$set(this.inputs["horizontalFnOnOff"], i, 0);
          } else if (
            this.shadingDeviceType === "Horizontal" &&
            this.hShadingCountInput > 0
          ) {
            this.$set(this.inputs["verticalFnOnOff"], i, 0);
            this.$set(this.inputs["horizontalFnOnOff"], i, 1);
          }
        } else {
          this.$set(this.inputs["verticalFnOnOff"], i, 0);
          this.$set(this.inputs["horizontalFnOnOff"], i, 0);
        }

        if (this.enable_overhangs && this.overhangsDepthInput > 0) {
          this.$set(this.inputs["overhangsOnOff"], i, 1);
        } else {
          this.$set(this.inputs["overhangsOnOff"], i, 0);
        }

        this.$set(
          this.inputs["verticalShadings_multiplier"],
          i,
          this.vShadingCountInput
        );
        this.$set(
          this.inputs["verticalShadings_depth"],
          i,
          this.vShadingDepthInput
        );
        this.$set(
          this.inputs["horizontalShadings_multiplier"],
          i,
          this.hShadingCountInput
        );
        this.$set(
          this.inputs["horizontalShadings_depth"],
          i,
          this.hShadingDepthInput
        );
        this.$set(
          this.inputs["overhangs_offset"],
          i,
          this.overhangsOffsetInput
        );
        this.$set(this.inputs["overhangs_depth"], i, this.overhangsDepthInput);
      });

      if (this.selectedWallsNums.length > 0) {
        if (window.Interop) {
          this.updateBackendData(
            "verticalFnOnOff",
            this.inputs["verticalFnOnOff"],
            true
          );
          this.updateBackendData(
            "horizontalFnOnOff",
            this.inputs["horizontalFnOnOff"],
            true
          );
          this.updateBackendData(
            "overhangsOnOff",
            this.inputs["overhangsOnOff"],
            true
          );

          this.updateBackendData(
            "verticalShadings_multiplier",
            this.inputs["verticalShadings_multiplier"],
            true
          );
          this.updateBackendData(
            "verticalShadings_depth",
            this.inputs["verticalShadings_depth"],
            true
          );
          this.updateBackendData(
            "horizontalShadings_multiplier",
            this.inputs["horizontalShadings_multiplier"],
            true
          );
          this.updateBackendData(
            "horizontalShadings_depth",
            this.inputs["horizontalShadings_depth"],
            true
          );

          this.updateBackendData(
            "overhangs_offset",
            this.inputs["overhangs_offset"],
            true
          );

          this.updateBackendData(
            "overhangs_depth",
            this.inputs["overhangs_depth"],
            true
          );

          this.updateBackendData("WWR_per_wall", this.inputs["WWR_per_wall"]);
          // window.Interop.unselectAllWalls();
          console.log("INPUTS", this.inputs);
        }
      }
    },
    wallNumsToMask(selectedWallsNums, numWalls) {
      // Initialize an array of zeros with length equal to numWalls
      var mask = new Array(numWalls).fill(0);

      // Iterate over selectedWallsNums and set the corresponding index in mask to 1
      selectedWallsNums.forEach(function (wallNum) {
        if (wallNum < numWalls) {
          // Ensure the wall number is within the valid range
          mask[wallNum] = 1;
        }
      });

      return mask;
    },
    updateBackendData(key, value, silent = false) {
      console.log(key, value, silent);
      if (window.Interop) {
        console.log("ubd", key, value);
        window.Interop.updateBackendData(key, JSON.stringify(value), silent);
      }
    },
    setZone() {
      console.log("Set Zone");
      if (window.Interop) {
        window.Interop.setZone();
      }
    },
    setContext() {
      console.log("Set Context");
      if (window.Interop) {
        window.Interop.setContext();
      }
    },
    setInteriorWalls() {
      console.log("Set InteriorWalls");
      if (window.Interop) {
        window.Interop.setInteriorWalls();
      }
    },
    setWeatherFile() {
      console.log("setWeatherFile");
      if (window.Interop) {
        window.Interop.setWeatherFile();
      }
    },
    runAnalysis() {
      console.log("Run Analysis!");
      if (window.Interop) {
        //window.Interop.runAnalysis();
      }
    },
    genHexString(len) {
      const hex = "0123456789ABCDEF";
      let output = "";
      for (let i = 0; i < len; ++i) {
        output += hex.charAt(Math.floor(Math.random() * hex.length));
      }
      return output;
    },

    updateParametricAnalysisProgress(
      analysisProgress,
      samplesLeft,
      isEstimateRun
    ) {
      const isEstimate = Boolean(isEstimateRun).valueOf();
      const progress = parseInt(analysisProgress);

      if (isEstimate) {
        this.showSpinnerParametricAnalysisEstimate = true;
      } else {
        this.showSpinnerParametricAnalysis = true;
      }

      this.parametricAnalysisLatestDiff =
        Date.now() - this.parametricAnalysisLastTime;

      //TODO maybe should do a moving average instead
      this.parametricAnalysisTimeEstimate = Math.round(
        (this.parametricAnalysisLatestDiff / 1000) * samplesLeft
      );

      this.parametricAnalysisLastTime = Date.now();

      console.log(
        "UpdateParametricAnalysisProgress",
        progress,
        isEstimate,
        this.parametricAnalysisTimeEstimate,
        samplesLeft
      );
      this.parametricAnalysisProgress = progress;
      if (!isEstimate) {
        console.log("not estimate");
      }
      if (progress == 100 || samplesLeft == 0) {
        this.parametricAnalysisProgress = -1;
        if (isEstimate) {
          this.showSpinnerParametricAnalysisEstimate = false;
        } else {
          this.showSpinnerParametricAnalysis = false;
        }
      } else {
      }

      this.$forceUpdate();
    },

    showLoader(show) {
      this.showSpinnerSingleRun = show;
    },

    showParametricAnalysisLoader(show) {
      this.showSpinnerParametricAnalysis = show;
    },

    setWWRShadingPerWall(
      wwrPerWall,
      verticalShadingCounts,
      verticalShadingDepths,
      horizontalShadingCounts,
      horizontalShadingDepths,
      overhangsOffset,
      overhangsDepth,
      verticalFnOnOff,
      horizontalFnOnOff,
      overhangsOnOff
    ) {
      console.log(
        "setWWRShadingPerWall",
        wwrPerWall,
        verticalShadingCounts,
        verticalShadingDepths
      );
      this.inputs["WWR_per_wall"] = JSON.parse(wwrPerWall);

      this.inputs["verticalShadings_multiplier"] = JSON.parse(
        verticalShadingCounts
      );
      this.inputs["verticalShadings_depth"] = JSON.parse(verticalShadingDepths);

      console.log(
        "setWWRShadingPerWall",
        wwrPerWall,
        verticalShadingCounts,
        this.inputs["verticalShadings_multiplier"]
      );

      this.inputs["horizontalShadings_multiplier"] = JSON.parse(
        horizontalShadingCounts
      );
      this.inputs["horizontalShadings_depth"] = JSON.parse(
        horizontalShadingDepths
      );

      this.inputs["overhangs_offset"] = JSON.parse(overhangsOffset);
      this.inputs["overhangs_depth"] = JSON.parse(overhangsDepth);

      this.inputs["verticalFnOnOff"] = JSON.parse(verticalFnOnOff);
      this.inputs["horizontalFnOnOff"] = JSON.parse(horizontalFnOnOff);
      this.inputs["overhangsOnOff"] = JSON.parse(overhangsOnOff);
    },

    setNumWalls(numWalls) {
      this.numWalls = JSON.parse(numWalls);
      this.selectedWallsCheckboxes = new Array(numWalls).fill(false);
      var arr = [];
      for (var i = 0; i < numWalls; i++) {
        arr.push(false);
      }
      this.selectedWallsCheckboxes = arr;
      //this.maxRegressionIndices = new Array(numWalls).fill(new Array(this.metricsTitles.length).fill(0));
      console.log(
        "reg",
        this.maxRegressionIndices,
        numWalls,
        this.numWalls,
        this.selectedWallsCheckboxes,
        arr,
        new Array(numWalls).fill(false)
      );
    },
    setInputsData(newInputsData) {
      console.log(newInputsData);
      for (var key in newInputsData) {
        this.inputs[key] = newInputsData[key];
      }
    },
    async setServerLoaded() {
      this.showSpinnerSingleRun = false;
    },
    log(payload) {
      console.log("LOG:: " + payload);
    },
    recieveDataTrees(dataTrees) {
      if (
        dataTrees["RH_OUT:energy_total_loads_metrics_tree"] &&
        dataTrees["RH_OUT:energy_total_loads_metrics_tree"].length > 0
      ) {
        this.energy_total_loads_metrics_tree =
          dataTrees["RH_OUT:energy_total_loads_metrics_tree"];
        console.log(
          "Energy Total Loads 1",
          dataTrees["RH_OUT:energy_total_loads_metrics_tree"]
        );
      }
      this.stacked_bar_key += 1;
      this.$forceUpdate();
    },
    updateParametricAnalysisModels(param, slopes, intercepts, correlations) {
      console.log("updateParametricAnalysisModels", param, slopes, intercepts);
      this.parametricAnalysisModelsSlopes[param] = slopes;
      this.parametricAnalysisModelsIntercepts[param] = intercepts;
      this.parametricAnalysisModelsCorrelations[param] = correlations;

      this.updateMaxParametricAnalysisRegression();
    },

    updateMaxParametricAnalysisRegression() {
      var numOutputs = this.metricsTitles.length;
      this.maxParametricAnalysisRegressionIndices = {};

      const res = [];
      for (var io = 0; io < numOutputs; io++) {
        const d = { id: io, name: this.metricsTitles[io], children: [] };

        Object.keys(this.parametricAnalysisModelsCorrelations).forEach(
          (param, ip) => {
            const c = this.parametricAnalysisModelsCorrelations[param][io];
            const child = {};
            child.id = ip;
            child.name = param + ": " + c;
            child.label = param;
            child.value = this.round3(c);
            child.title = this.parametricAnalysisData.find(
              (x) => x.name == param
            ).title;
            d.children.push(child);
          }
        );

        //d.children.sort((x, y) => Math.abs(y.value) - Math.abs(x.value));
        res.push(d);
      }

      this.parametricAnalysisSortedSensitivites = res;
      console.log("dict", res);

      Object.keys(this.parametricAnalysisModelsCorrelations).forEach(
        (param) => {
          for (var i = 0; i < numOutputs; i++) {
            this.maxParametricAnalysisRegressionIndices[param] =
              this.parametricAnalysisModelsCorrelations[param].map((s) =>
                Math.abs(s) > this.notableSlopeThreshold ? 1 : 0
              );
          }
        }
      );
      console.log(
        "updateMaxParametricAnalysisRegression",
        this.maxParametricAnalysisRegressionIndices
      );
      this.parametric_analysis_sparklines_key += 1;
      this.updateAnalysisFolder();
    },
    updateMaxRegression(param) {
      var maxIndices = [];
      var maxIndices2 = [];
      var numOutputs = this.metricsTitles.length;
      let arrs = [];
      let arrs2 = [];
      for (var i = 0; i < numOutputs; i++) {
        let arr = Object.keys(this.regressionModelsSlopes[param]).map(
          (wallNum) => {
            return (
              0.5 * this.regressionModelsSlopes[param][wallNum][i] +
              this.regressionModelsIntercepts[param][wallNum][0]
            );
          }
        );
        let arr2 = Object.keys(this.regressionModelsSlopes[param]).map(
          (wallNum) => {
            return this.regressionModelsSlopes[param][wallNum][i];
          }
        );
        maxIndices2[i] = arr2.indexOf(Math.max(...arr2));
        maxIndices[i] = arr.indexOf(Math.max(...arr));
        arrs.push(arr);
        arrs2.push(arr2);
      }
      console.log(maxIndices2, maxIndices, arrs, arrs2);
      for (var w = 0; w < this.numWalls; w++) {
        this.maxRegressionIndices[w] = maxIndices.map((maxIndex) =>
          maxIndex === w ? 1 : 0
        );
      }
    },
    updateUIData(key, value) {
      console.log("Updating", key, value);
      this[key] = value;
    },

    /**
     * Source: https://observablehq.com/@chrispahm/latin-hypercube-sampling-in-javascript
     *  Creates a random latin hypercube design
     *  Adapted to JavaScript from randomLHS.R (by Doug Mooney, Rob Carnell, Christoph Pahmeyer)
     *  N is the number of partitions (simulations or design points)
     *  K is the number of replication (variables)
     *  PRNG (pseudorandom number generator) is a function for generating random numbers. Defaults to Math.random
     */
    randomLHS(n, k, PRNG = Math.random) {
      const tres = [];

      for (let j = 0; j < k; j++) {
        const col = randomPerm(n);
        for (let i = 0; i < n; i++) {
          col[i] += PRNG();
          col[i] /= n;
        }
        tres[j] = col;
      }

      const res = [];

      for (let i = 0; i < n; i++) {
        res[i] = [];
        for (let j = 0; j < k; j++) {
          res[i][j] = tres[j][i];
        }
      }

      return res;

      function randomPerm(k) {
        const res = [];
        for (let i = 0; i < k; i++) {
          res[i] = i;
        }

        for (let i = k - 1; i >= 0; --i) {
          const pos = Math.floor(i * PRNG());
          const tmp = res[i];
          res[i] = res[pos];
          res[pos] = tmp;
        }

        return res;
      }
    },
    recieveData(data) {
      //
      console.log(data, data["RH_OUT:DA_metric"]); //DA_all_metrics

      if (
        data["RH_OUT:DA_all_metrics"] &&
        data["RH_OUT:DA_all_metrics"].length > 0
      ) {
        this.da_all_metrics = data["RH_OUT:DA_all_metrics"];
      }

      if (data["RH_OUT:DA_metric"] && data["RH_OUT:DA_metric"].length > 0) {
        this.da_metrics = data["RH_OUT:DA_metric"];
        this.da_average =
          this.da_metrics.reduce((a, b) => a + b) / this.da_metrics.length;
        console.log(this.da_average);
      }
      if (
        data["RH_OUT:energy_loads_metric"] &&
        data["RH_OUT:energy_loads_metric"].length > 0
      ) {
        this.energy_loads = data["RH_OUT:energy_loads_metric"];
      }

      if (data["RH_OUT:wwr_metric"] && data["RH_OUT:wwr_metric"].length > 0) {
        this.wwr_metric = data["RH_OUT:wwr_metric"];
      }
    },
  },
};
</script>
<style scoped>
.container {
  padding: 10px;
}
.panels_container {
  display: flex;
  flex-wrap: wrap;
  flex-direction: column;
}
.data_table_container.large {
  padding: 5px;
  max-height: 220px;
  overflow-y: scroll;
  overflow-x: hidden;
}
.wwr_inputs_container .col:not(.analysis-col) {
  padding: 5px;
  max-width: 120px;
}

.wwr_inputs_container .col.analysis-col {
  /*padding: 0;
  */
  min-width: 450px;
  text-align: center;
  padding: 6px;
}

>>> .comparision_radio_group .v-input--radio-group__input {
  display: flex;
  flex-direction: row;
  flex-wrap: wrap;
  max-width: fit-content;
}

/* For mobile phones: */
.panels_container .wwr_inputs_container {
  font-size: medium;
}

@media only screen and (max-width: 780px) {
  /* For mobile phones: */
  .panels_container .wwr_inputs_container {
    font-size: small;
  }
}

.panels_container
  .wwr_inputs_container
  > .row
  > .col
  > .walls_checkboxes
  > .v-input__control
  > div.v-input__slot {
  margin: 0;
}
.v-input--selection-controls.v-input .v-input__slot {
  margin: 0 !important;
}
.comparision_tab .v-expansion-panel-header::v-deep {
  min-height: fit-content;
}
.loading-element {
  position: absolute !important;
  top: 50px;
  right: 50px;
}

.prediction_element {
  border: 1px black solid;
}

.analyze_button {
  margin: 5px;
}

>>> .paramAnalysisDefaultRadioGroup .v-label {
  font-size: small;
}
</style>
