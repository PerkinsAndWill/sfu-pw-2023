import Vue from 'vue';
import Vuetify from 'vuetify/lib/framework';
import '@mdi/font/css/materialdesignicons.css';

Vue.use(Vuetify);

const opts = {
    icons: {
        iconfont: 'mdi',
        defaultSet: 'mdi', // This is already the default value - only for display purposes
    },
  };

export default new Vuetify(opts);
