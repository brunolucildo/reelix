import { StyleSheet, View } from 'react-native';

import CollectorMedal from '../../../assets/brand/badges/collector.svg';
import CriticMedal from '../../../assets/brand/badges/critic.svg';
import ExplorerMedal from '../../../assets/brand/badges/explorer.svg';
import FirstFilmMedal from '../../../assets/brand/badges/first-film.svg';
import FriendMedal from '../../../assets/brand/badges/friend.svg';
import LegendMedal from '../../../assets/brand/badges/legend.svg';
import MarathonMedal from '../../../assets/brand/badges/marathon.svg';
import SeriesFanMedal from '../../../assets/brand/badges/series-fan.svg';

type Props = {
  code: string;
  title: string;
  earned: boolean;
  size?: number;
};

export function AchievementMedal({ code, title, earned, size = 64 }: Props) {
  const graphic = medalGraphic(code, size);
  if (!graphic) {
    return null;
  }

  return (
    <View
      accessibilityLabel={`Medalha ${title}, ${earned ? 'conquistada' : 'bloqueada'}`}
      accessibilityRole="image"
      style={[styles.medal, { width: size, height: size }, !earned && styles.locked]}
    >
      {graphic}
    </View>
  );
}

function medalGraphic(code: string, size: number) {
  switch (code) {
    case 'first-movie':
      return <FirstFilmMedal width={size} height={size} />;
    case 'cinephile':
      return <LegendMedal width={size} height={size} />;
    case 'first-marathon':
      return <MarathonMedal width={size} height={size} />;
    case 'series-in-focus':
      return <SeriesFanMedal width={size} height={size} />;
    case 'genre-explorer':
      return <ExplorerMedal width={size} height={size} />;
    case 'critic':
      return <CriticMedal width={size} height={size} />;
    case 'collector':
      return <CollectorMedal width={size} height={size} />;
    case 'social':
      return <FriendMedal width={size} height={size} />;
    default:
      return null;
  }
}

const styles = StyleSheet.create({
  medal: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  locked: {
    opacity: 0.35,
  },
});
