// pt-BR formatting for dates and numbers. Every function keeps the exact output the screens had
// before 0017 (same locale and options), so no visible string changes.
const LOCALE = 'pt-BR';

const monthYearFormat = new Intl.DateTimeFormat(LOCALE, { month: 'long', year: 'numeric' });
const dateMediumFormat = new Intl.DateTimeFormat(LOCALE, { dateStyle: 'medium' });
const dateLongFormat = new Intl.DateTimeFormat(LOCALE, { dateStyle: 'long' });
const dateTimeMediumFormat = new Intl.DateTimeFormat(LOCALE, {
  dateStyle: 'medium',
  timeStyle: 'short',
});

/** "novembro de 2026". */
export function formatMonthYear(date: Date): string {
  return monthYearFormat.format(date);
}

/** "23/11/2026" from an ISO string or a Date. */
export function formatDate(value: string | Date): string {
  return new Date(value).toLocaleDateString(LOCALE);
}

/** "23/11/2026, 13:20:00" from an ISO string or a Date. */
export function formatDateTime(value: string | Date): string {
  return new Date(value).toLocaleString(LOCALE);
}

/** "23 de nov. de 2026". */
export function formatDateMedium(date: Date): string {
  return dateMediumFormat.format(date);
}

/** "23 de novembro de 2026". */
export function formatDateLong(date: Date): string {
  return dateLongFormat.format(date);
}

/** "23 de nov. de 2026 13:20". */
export function formatDateTimeMedium(date: Date): string {
  return dateTimeMediumFormat.format(date);
}

/** Catalog release dates are plain `yyyy-MM-dd`; noon avoids a day shift across time zones. */
export function formatCatalogDate(date: string): string {
  return formatDateMedium(new Date(`${date}T12:00:00`));
}

export function formatNumber(value: number, options?: Intl.NumberFormatOptions): string {
  return value.toLocaleString(LOCALE, options);
}

/** Relative time for the social feed: "Agora", "Há 5 min", "Há 3 h", then a short date. */
export function formatActivityTime(value: string): string {
  const date = new Date(value);
  const minutes = Math.max(0, Math.floor((Date.now() - date.getTime()) / 60_000));
  if (minutes < 1) return 'Agora';
  if (minutes < 60) return `Há ${minutes} min`;
  if (minutes < 1440) return `Há ${Math.floor(minutes / 60)} h`;
  return date.toLocaleDateString(LOCALE, { day: 'numeric', month: 'short', year: 'numeric' });
}

/** Local calendar day as `yyyy-MM-dd` (not UTC). */
export function localDateKey(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

/** First letter of the name in upper case, or `fallback` when the name is empty. */
export function getInitials(name: string, fallback = '?'): string {
  return name.trim().slice(0, 1).toLocaleUpperCase(LOCALE) || fallback;
}

/** ISO timestamp as the local "DD/MM/AAAA HH:mm" text shown in date inputs. */
export function toLocalDateTimeInput(value: string): string {
  const date = new Date(value);
  const pad = (part: number) => String(part).padStart(2, '0');
  const year = String(date.getFullYear()).padStart(4, '0');
  return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${year} ${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

/** Parses "DD/MM/AAAA HH:mm" (local time) into an ISO timestamp, or null when invalid. */
export function parseLocalDateTimeInput(value: string): string | null {
  const match = /^(\d{2})\/(\d{2})\/(\d{4}) (\d{2}):(\d{2})$/.exec(value);
  if (!match) {
    return null;
  }

  const [day, month, year, hour, minute] = match.slice(1).map(Number);
  if (year < 1 || month < 1 || month > 12 || day < 1 || day > 31 || hour > 23 || minute > 59) {
    return null;
  }

  const date = new Date(0);
  date.setFullYear(year, month - 1, day);
  date.setHours(hour, minute, 0, 0);
  if (
    date.getFullYear() !== year ||
    date.getMonth() !== month - 1 ||
    date.getDate() !== day ||
    date.getHours() !== hour ||
    date.getMinutes() !== minute
  ) {
    return null;
  }
  return date.toISOString();
}
