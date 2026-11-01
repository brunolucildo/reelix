import Constants from 'expo-constants';
import * as Crypto from 'expo-crypto';
import * as Device from 'expo-device';
import * as Notifications from 'expo-notifications';
import { AndroidImportance } from 'expo-notifications';
import { Platform } from 'react-native';
import * as SecureStore from 'expo-secure-store';

import { deactivateNotificationDevice, registerNotificationDevice } from '@/services/api-client';

const installationIdKey = 'reelix.push.installation-id';
const deviceIdKeyPrefix = 'reelix.push.device-id.';

export async function registerCurrentPushDevice(userId: string): Promise<void> {
  if (!Device.isDevice) {
    throw new Error('Notificações push precisam ser configuradas em um dispositivo físico.');
  }

  if (Platform.OS !== 'ios' && Platform.OS !== 'android') {
    throw new Error('Notificações push estão disponíveis somente em iOS e Android.');
  }

  const projectId = Constants.expoConfig?.extra?.eas?.projectId ?? Constants.easConfig?.projectId;
  if (typeof projectId !== 'string' || !isUuid(projectId)) {
    throw new Error('O projeto ainda não configurou um projectId EAS para notificações.');
  }

  if (Platform.OS === 'android') {
    await Notifications.setNotificationChannelAsync('default', {
      name: 'Notificações',
      importance: AndroidImportance.HIGH,
      vibrationPattern: [0, 250, 250, 250],
      lightColor: '#27D17F',
    });
  }

  let permission = await Notifications.getPermissionsAsync();
  if (permission.status !== 'granted') {
    permission = await Notifications.requestPermissionsAsync();
  }
  if (permission.status !== 'granted') {
    throw new Error('Permissão de notificações negada. Altere-a nas configurações do dispositivo.');
  }

  const installationId = await getInstallationId();
  const pushToken = await Notifications.getExpoPushTokenAsync({ projectId });
  const registeredDevice = await registerNotificationDevice({
    installationId,
    expoPushToken: pushToken.data,
    platform: Platform.OS,
    projectId,
  });
  await SecureStore.setItemAsync(`${deviceIdKeyPrefix}${userId}`, registeredDevice.id);
}

export async function revokeRegisteredPushDevice(userId: string): Promise<void> {
  const key = `${deviceIdKeyPrefix}${userId}`;
  const deviceId = await SecureStore.getItemAsync(key);
  if (!deviceId) {
    return;
  }

  await deactivateNotificationDevice(deviceId);
  await SecureStore.deleteItemAsync(key);
}

async function getInstallationId(): Promise<string> {
  const existing = await SecureStore.getItemAsync(installationIdKey);
  if (existing && isUuid(existing)) {
    return existing;
  }

  const bytes = await Crypto.getRandomBytesAsync(16);
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = Array.from(bytes, (byte) => byte.toString(16).padStart(2, '0')).join('');
  const installationId = [
    hex.slice(0, 8),
    hex.slice(8, 12),
    hex.slice(12, 16),
    hex.slice(16, 20),
    hex.slice(20),
  ].join('-');
  await SecureStore.setItemAsync(installationIdKey, installationId);
  return installationId;
}

function isUuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
}
