/** The API sends UTC times; if the "Z" is missing the browser would read them as local time. */
export function toDate(value: string): Date {
  return new Date(/Z$|[+-]\d\d:\d\d$/.test(value) ? value : value + 'Z');
}