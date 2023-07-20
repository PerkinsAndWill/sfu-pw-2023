
import Vue from "vue";
import Vuex from "vuex";
Vue.use(Vuex);

function builder() {
  return new Vuex.Store({
    state: {},
    getters: {},
    mutations: {},
    actions: {},
    modules: {},
  });
}
export default builder;
