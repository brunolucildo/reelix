const { withAndroidManifest } = require('expo/config-plugins');

module.exports = function withDevelopmentCleartextTraffic(config) {
  if (process.env.APP_VARIANT !== 'development') {
    return config;
  }

  return withAndroidManifest(config, (modConfig) => {
    const application = modConfig.modResults.manifest.application?.[0];
    if (!application) {
      throw new Error('Android manifest is missing its application element.');
    }

    application.$ = application.$ ?? {};
    application.$['android:usesCleartextTraffic'] = 'true';
    return modConfig;
  });
};
