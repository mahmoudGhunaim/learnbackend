/**
 * Inside Arabic (RTL) text, the Unicode bidi algorithm moves trailing neutral characters:
 * "C#" shows as "#C" and ".NET" as "NET.". An invisible LEFT-TO-RIGHT MARK (U+200E)
 * next to the neutral character pins it in place.
 */
export function fixBidi(text: string): string {
  return text.replace(/C#(?!‎)/g, 'C#‎').replace(/(?<!‎)\.NET/g, '‎.NET');
}
