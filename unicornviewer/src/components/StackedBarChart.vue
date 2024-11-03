<template>
  <div
    id="chart"
    style="display: flex; flex-direction: row; font-size: small;"
  >
    <div>
      Legend
      <div
        v-for="(cat, i) in categories"
        :key="cat"
        style="display: flex; flex-direction: row; align-items: center;"
      >
        <div
          :style="[{'width':'15px'},
                   {'height':'15px'},
                   {'margin-right':'10px'},
                   {'background-color':
                     categoryColors[i]}]"
        />
        {{ cat }} 
      </div>
    </div>
  </div>
</template>

<script>
import * as d3 from "d3";


export default {
  name: "StackedBarChart",
  props: {
    rawData: {
      type: Array,
      required: true,
    },
    categories:{
      type: Array,
      required: true,
    },   
    outerWidth: {
      type: Number,
      required: true,
    }, 
    outerHeight: {
      type: Number,
      required: true,
    },
    categoryColors: {
      type: Array,
      required: false,
    }
  },
  data() {
    return {
      colorScale: this.categoryColors? d3.scaleOrdinal().range(this.categoryColors): d3.scaleOrdinal(d3.schemeCategory10),
      xAxis: null,
      yAxis: null,
      yScale: null,
      xScale: null,
      months: [
        "Jan",
        "Feb",
        "Mar",
        "Apr",
        "May",
        "Jun",
        "Jul",
        "Aug",
        "Sep",
        "Oct",
        "Nov",
        "Dec",
      ],
     // categories: ["Cooling", "Heating", "Lighting", "Equipment"],
      margin: { top: 20, right: 20, bottom: 30, left: 40 },
      svgChart: null,
    };
  },
  watch: {
    rawData: {
      handler(newVal, oldVal) {
        // watch it
        console.log("Prop changed: ", newVal, " | was: ", oldVal);
        this.createChart();
        this.updateChart(newVal);
      },
      deep: true,
    },
  },
  mounted() {
    //Negative values are expected in this format: [-1, -2, 1, 2] rather than [-2,-1,1,2]
    console.log(this.rawData)
    this.createChart();
    this.updateChart(this.rawData);
  },
  methods: {
    createChart() {
      /**/
      d3.select("#chart").select("svg").remove();
      // Set dimensions and margins for the graph

      this.width = this.outerWidth - this.margin.left - this.margin.right;
      this.height = this.outerHeight - this.margin.top - this.margin.bottom;

      // Set up the SVG container
      this.svgChart = d3
        .select("#chart")
        .append("svg").lower()
        .attr("width", this.width + this.margin.left + this.margin.right)
        .attr("height", this.height + this.margin.top + this.margin.bottom)
        .append("g")
        .attr("transform", `translate(${this.margin.left},${this.margin.top})`);

        

      // Add the X Axis
      this.xAxis = this.svgChart
        .append("g")
        .attr("transform", `translate(0,${this.height})`);

      // Add the Y Axis
      this.yAxis = this.svgChart.append("g");
    },
    updateChart(rawData) {
      const data = rawData.map((d, i) => {
        const obj = { month: this.months[i] };
        this.categories.forEach((category, index) => {
          obj[category] = d[index];
        });
        return obj;
      });

      // Using d3.stack() to handle negative values properly
      const stack = d3
        .stack()
        .keys(this.categories)
        .offset(d3.stackOffsetDiverging);
      const layers = stack(data);

      // Calculating maximum and minimum values for the y-axis scale
      const maxY = d3.max(layers, (layer) =>
        d3.max(layer, (segment) => segment[1])
      );
      const minY = d3.min(layers, (layer) =>
        d3.min(layer, (segment) => segment[0])
      );

      // Update the yScale to span from the most negative to the most positive
      this.yScale = d3
        .scaleLinear()
        .range([this.height, 0])
        .domain([minY, maxY]);

      // Update the xScale
      this.xScale = d3
        .scaleBand()
        .range([0, this.width])
        .padding(0.1)
        .domain(data.map((d) => d.month));

      // Update Axes
      this.xAxis.transition().call(d3.axisBottom(this.xScale));
      this.yAxis.transition().call(d3.axisLeft(this.yScale));

      this.svgChart
        .selectAll(".layer")
        .data(layers)
        .enter()
        .append("g")
        .attr("class", "layer")
        .style("fill", (d, i) => this.colorScale(i))
        .selectAll("rect")
        .data((d) => d)
        .enter()
        .append("rect")
        .attr("x", (d) => this.xScale(d.data.month))
        .attr("y", (d) => this.yScale(Math.max(d[0], d[1])))
        .attr("height", (d) => Math.abs(this.yScale(d[0]) - this.yScale(d[1])))
        .attr("width", this.xScale.bandwidth());

    },
    updateCharts(rawDatasets) {
      console.log("CHARTS")

      const datasets = rawDatasets.map(dataset => {
        return dataset.map((d, i) => {
          const obj = { month: this.months[i] };
          this.categories.forEach((category, index) => {
            obj[category] = d[index];
          });
          return obj;
        });
      });
   
      // Define the yScale
      const maxY = d3.max(datasets.flat(), d => d3.max(this.categories, key => d[key]));
      const minY = d3.min(datasets.flat(), d => d3.min(this.categories, key => d[key]));

      this.yScale = d3.scaleLinear()
        .range([this.height, 0])
        .domain([minY, maxY]);

      // Define the xScales
      this.xScale = d3.scaleBand()
        .range([0, this.width])
        .padding(0.1)
        .domain(this.months);

      this.monthScale = d3.scaleBand()
        .range([0, this.xScale.bandwidth()])
        .padding(0.05)
        .domain(datasets.map((_, i) => i));

      this.xAxis.call(d3.axisBottom(this.xScale));
      this.yAxis.call(d3.axisLeft(this.yScale));

      // Bind each dataset to a group
      const datasetGroups = this.svgChart.selectAll(".dataset-group")
        .data(datasets)
        .enter().append("g")
          .attr("class", "dataset-group")
          .attr("transform", (d, i) => `translate(${this.monthScale(i)}, 0)`);

      // Within each group, bind and draw the bars
      datasetGroups.each((dataset, i, nodes) => {
        const monthGroups = d3.select(nodes[i]).selectAll(".month-group")
          .data(dataset)
          .enter().append("g")
            .attr("class", "month-group")
            .attr("transform", d => `translate(${this.xScale(d.month)}, 0)`);

        const stack = d3.stack().keys(this.categories).offset(d3.stackOffsetDiverging);
        const layers = stack(dataset);

        monthGroups.selectAll(".bar")
          .data(layers)
          .enter().append("g")
          .attr("class", "bar")
          .style("fill", (d, idx) => this.colorScale(idx))
          .selectAll("rect")
          .data(d => d)
          .enter().append("rect")
            .attr("x", 0)
            .attr("y", d => this.yScale(Math.max(d[0], d[1])))
            .attr("width", this.monthScale.bandwidth())
            .attr("height", d => Math.abs(this.yScale(d[0]) - this.yScale(d[1])));
      });
    },
  },
};
</script>
<style scoped></style>
