import http from 'k6/http';
import { check } from 'k6';

export const options = {
  scenarios: {
    constant_load: {
      executor: 'constant-arrival-rate',
      rate: 100,
      timeUnit: '1s',
      duration: '15m',       // long enough to outlast any deployment
      preAllocatedVUs: 20,
      maxVUs: 50,
    },
  },
};

export default function () {
  const res = http.get('https://cookbook.benhhome.duckdns.org', {
    timeout: '5s',
  });

  check(res, {
    'status is 2xx': (r) => r.status >= 200 && r.status < 300,
  });
}
