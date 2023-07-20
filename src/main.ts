import Vue from 'vue';
import App from './App.vue';
import store from "./store";
import vuetify from '@/plugins/vuetify'

const app = new Vue({
    vuetify,
    store: store(),
    render: h => h(App),
  }).$mount('#app');