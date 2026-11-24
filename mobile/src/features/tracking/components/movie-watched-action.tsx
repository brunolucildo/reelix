import { IconAction } from '@/components/ui/icon-action';
import { useMovieWatchedToggle } from '@/features/tracking/hooks/use-tracking';
import CheckIcon from '@/assets/brand/navigation-icons/check.svg';
import EyeIcon from '@/assets/brand/navigation-icons/eye.svg';
import { dimensions } from '@/theme/tokens';

export function MovieWatchedAction({ titleId, title }: { titleId: string; title: string }) {
  const { status, isWatched, isBusy, toggle } = useMovieWatchedToggle(titleId);

  if (status.isError) {
    return (
      <IconAction label="Tentar carregar estado do filme" onPress={() => void status.refetch()}>
        <EyeIcon width={dimensions.iconSize} height={dimensions.iconSize} />
      </IconAction>
    );
  }

  return (
    <IconAction
      label={isWatched ? `Desmarcar ${title} como assistido` : `Marcar ${title} como assistido`}
      selected={isWatched}
      disabled={isBusy || status.isFetching}
      loading={status.isPending || isBusy}
      onPress={toggle}
    >
      {isWatched
        ? <CheckIcon width={dimensions.iconSize} height={dimensions.iconSize} />
        : <EyeIcon width={dimensions.iconSize} height={dimensions.iconSize} />}
    </IconAction>
  );
}
