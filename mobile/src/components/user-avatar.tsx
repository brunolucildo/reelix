import { Image } from 'expo-image';
import { StyleSheet, Text, View } from 'react-native';

import Noir from '../../assets/brand/avatars/noir.svg';
import Cosmic from '../../assets/brand/avatars/cosmic.svg';
import Phantom from '../../assets/brand/avatars/phantom.svg';
import Comedy from '../../assets/brand/avatars/comedy.svg';
import Animation from '../../assets/brand/avatars/animation.svg';
import Adventure from '../../assets/brand/avatars/adventure.svg';
import Robot from '../../assets/brand/avatars/robot.svg';
import Vampire from '../../assets/brand/avatars/vampire.svg';
import Maestro from '../../assets/brand/avatars/maestro.svg';
import { colors, dimensions, fontFamily, radii, typography } from '@/theme/tokens';

export const avatarOptions = [
  { key: 'noir', label: 'Detetive Neblina', category: 'Mistério', Graphic: Noir },
  { key: 'cosmic', label: 'Órbita', category: 'Ficção científica', Graphic: Cosmic },
  { key: 'phantom', label: 'Espectro', category: 'Terror', Graphic: Phantom },
  { key: 'comedy', label: 'Riso em Cena', category: 'Comédia', Graphic: Comedy },
  { key: 'animation', label: 'Pipoca', category: 'Animação', Graphic: Animation },
  { key: 'adventure', label: 'Exploradora Lumi', category: 'Aventura', Graphic: Adventure },
  { key: 'robot', label: 'Robô Pixel', category: 'Ficção científica', Graphic: Robot },
  { key: 'vampire', label: 'Vampiro Noturno', category: 'Terror', Graphic: Vampire },
  { key: 'maestro', label: 'Maestro Jazz', category: 'Musical', Graphic: Maestro },
] as const;

export function UserAvatar({ name, avatarKey, uri, size = dimensions.minimumTouchTarget }: { name: string; avatarKey?: string | null; uri?: string | null; size?: number }) {
  const option = avatarOptions.find((item) => item.key === avatarKey);
  const Graphic = option?.Graphic;
  return <View accessibilityRole="image" accessibilityLabel={`Avatar de ${name}${option ? `, ${option.label}` : ''}`} style={[styles.avatar, { width: size, height: size }]}>
    {Graphic ? <Graphic width={size} height={size} /> : uri ? <Image source={{ uri }} contentFit="cover" style={{ width: size, height: size }} /> : <Text style={styles.initial}>{name.trim().slice(0, 1).toLocaleUpperCase('pt-BR') || '?'}</Text>}
  </View>;
}

const styles = StyleSheet.create({
  avatar: { borderRadius: radii.pill, overflow: 'hidden', alignItems: 'center', justifyContent: 'center', backgroundColor: colors.surfaceElevated },
  initial: { fontFamily: fontFamily.bold, fontSize: typography.section, color: colors.textPrimary },
});