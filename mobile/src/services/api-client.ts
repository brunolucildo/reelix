// Temporary barrel: keeps the pre-0017 import path working while the screens move to
// their feature modules (spec 0018). New code imports from `@/features/<module>/api/*`.
export * from '@/features/account/api/account-api';
export * from '@/features/onboarding/api/onboarding-api';
export * from '@/features/catalog/api/catalog-api';
export * from '@/features/profile/api/profile-api';
export * from '@/features/social/api/social-api';
export * from '@/features/notifications/api/notifications-api';
export * from '@/features/gamification/api/gamification-api';
export * from '@/features/ratings/api/ratings-api';
