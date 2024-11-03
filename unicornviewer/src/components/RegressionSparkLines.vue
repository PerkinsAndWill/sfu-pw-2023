<template>
  <div style="display: flex; flex-direction: column">
    <div
    
      :id="id"
      style="display: flex"
    />
  </div>
</template>
<script>
import * as d3 from "d3";

export default {
  name: "RegressionSparkLines",
  props: {
    id: {
      type: String,
      required: true,
    },
    titles: {
      type: Array,
      required: true,
    },
    showTitles: {
      type: Boolean,
      required: false,
      default: false,
    },
    slopes: {
      type: Array,
      required: true,
      default: () => [],
    },
    intercepts: {
      type: Array,
      required: true,
      default: () => [],
    },
    outlined: {
      type: Array,
      required: false,
      default: () => [],
    }
  },
  data() {
    return {};
  },
  watch: {
    outlined: {
      handler: function() {
          this.$forceUpdate();
      }
    }
  },
  mounted() {
    if (
      this.slopes != undefined &&
      this.intercepts != undefined &&
      this.slopes.length > 0 &&
      this.intercepts.length > 0
    ) {
      console.log(this.intercepts, this.slopes);
      d3.select("#" + this.id)
        .select("svg")
        .remove();

      var margin = { top: 10, right: 25, bottom: 20, left: 25 },
        width = 85 - margin.left - margin.right,
        height = 85 - margin.top - margin.bottom;

      var xScale = d3.scaleLinear().domain([0, 1]).range([0, width]);

      var yScale = d3.scaleLinear().domain([0, 100]).range([height, 0]);

      var xAxis = d3.axisBottom(xScale).ticks(2).tickPadding(0);

      var yAxis = d3.axisLeft(yScale).ticks(2).tickPadding(0);
      console.log(this.outlined)
      this.titles.forEach((title, i) => {
        var svg = d3
          .select("#" + this.id)
          .append("svg")
          .attr("width", width + margin.left + margin.right)
          .attr("height", height + margin.top + margin.bottom)
          .style("margin", "5px")
          .style("background-color", (this.outlined.length >= this.titles.length) && this.outlined[i] === 1?"lightblue":"none")
          .append("g")
          .attr(
            "transform",
            "translate(" + margin.left + "," + margin.top + ")"
          );
        
       

        if(this.showTitles)
        {
          svg
          .append("text")
          .attr("class", "p")
          .attr("x", width / 2) //positions it at the middle of the width
          .attr("y", margin.top) //positions it from the top by the margin top
          .attr("font-family", "sans-serif")
          .style("font-size", "small")
          .attr("height", "25px")
          .attr("fill", "black")
          .attr("text-anchor", "middle")
          .text(title);
        }

        svg
          .append("g")
          .attr("class", "x axis")
          .attr("transform", "translate(0," + height + ")")
          .call(xAxis);

        svg.append("g").attr("class", "y axis").call(yAxis);

        var max = 1;
        var myLine = svg
          .append("svg:line")
          .attr("x1", xScale(0.25))
          .attr("y1", yScale(this.intercepts[i]))
          .attr("x2", xScale(0.75))
          .attr("y2", yScale(max * this.slopes[i] + this.intercepts[i]))
          .style("stroke", "black");
      });
    }
  },
  methods: {},
};
</script>

<style scoped></style>
