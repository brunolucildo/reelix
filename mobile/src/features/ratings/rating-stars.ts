export function formatRatingStars(score: number) {
  const normalizedScore = Math.round(Math.max(0, Math.min(5, score)) * 2) / 2;
  const wholeStars = Math.floor(normalizedScore);
  const halfStar = normalizedScore % 1 === 0.5;
  const emptyStars = Math.max(0, 5 - wholeStars - Number(halfStar));
  return `${'★'.repeat(wholeStars)}${halfStar ? '◐' : ''}${'☆'.repeat(emptyStars)}`;
}
