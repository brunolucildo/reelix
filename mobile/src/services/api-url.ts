const apiUrl = process.env.EXPO_PUBLIC_API_URL?.trim();

if (!apiUrl) {
  throw new Error('Configure EXPO_PUBLIC_API_URL in mobile/.env before starting the app.');
}

const parsedApiUrl = new URL(apiUrl);
const isLoopbackOrEmulator = ['localhost', '127.0.0.1', '10.0.2.2'].includes(parsedApiUrl.hostname);
const isDevelopmentHttpUrl =
  __DEV__ &&
  parsedApiUrl.protocol === 'http:' &&
  (isLoopbackOrEmulator || isPrivateIpv4(parsedApiUrl.hostname));

if (parsedApiUrl.protocol !== 'https:' && !isDevelopmentHttpUrl) {
  throw new Error('EXPO_PUBLIC_API_URL must use HTTPS except for development on a trusted local network.');
}

function isPrivateIpv4(hostname: string): boolean {
  const octets = hostname.split('.').map(Number);
  if (octets.length !== 4 || octets.some((octet) => !Number.isInteger(octet) || octet < 0 || octet > 255)) {
    return false;
  }

  const [first, second] = octets;
  return first === 10 || (first === 172 && second >= 16 && second <= 31) || (first === 192 && second === 168);
}

export { apiUrl };
