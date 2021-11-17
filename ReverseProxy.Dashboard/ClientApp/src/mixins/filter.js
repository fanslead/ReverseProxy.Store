// import config from '@/api/config';
import { formatDate, formatTime, formatPrice, validDate } from '@/assets/js/utils'
export default {
  data() {
    return {}
  },
  filters: {
    getImgUrl(val) {
      if (val) {
        if (val.indexOf('http') > -1) {
          return val;
        }
        return apiConfig.upfileBaseUrl + val + '_0x0';
      }
      return '';
    },
    validDate,
    formatDate,
    formatTime,
    formatPrice
  }
}
