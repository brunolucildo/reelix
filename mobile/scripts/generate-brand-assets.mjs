import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const mobileDirectory = path.resolve(scriptDirectory, '..');
const brandDirectory = path.join(mobileDirectory, 'assets', 'brand');
const colors = {
  red: '#FF2D55',
  pink: '#FF6688',
  black: '#0F0F10',
  surface: '#1A1A1E',
  raised: '#2A2A2E',
  white: '#FFFFFF',
  muted: '#E5E7EB',
};

const markPath = [
  'M13 21C13 16 17 12 22 12H31C37 12 41 13 46 16L82 35',
  'C89 39 89 47 82 51L64 61L83 74C90 79 88 86 83 90',
  'C79 93 75 91 70 88L34 64C30 61 28 58 28 53V78',
  'C28 84 24 88 19 88C16 88 13 85 13 81Z',
  'M17 27H23V34H17Z',
  'M17 42H23V49H17Z',
  'M17 57H23V64H17Z',
  'M47 35L69 46L47 57Z',
].join('');

function svg(width, height, viewBox, body) {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="${viewBox}">${body}</svg>\n`;
}

function symbol(color) {
  return `<path d="${markPath}" fill="${color}" fill-rule="evenodd" clip-rule="evenodd"/>`;
}

function writeSvg(relativePath, contents) {
  const filePath = path.join(brandDirectory, relativePath);
  mkdirSync(path.dirname(filePath), { recursive: true });
  writeFileSync(filePath, contents, 'utf8');
}

function logoHorizontal(color = colors.white, monochrome = false) {
  const markColor = monochrome ? color : color === colors.white ? colors.red : color;
  const taglineColor = monochrome ? color : color === colors.white ? colors.red : color;
  return svg(
    400,
    120,
    '0 0 400 120',
    `<g transform="translate(8 10)">${symbol(markColor)}</g>` +
      `<text x="128" y="72" fill="${color}" font-family="Sora_700Bold" font-size="58" font-weight="700" letter-spacing="-2.5">reelix</text>` +
      `<text x="132" y="103" fill="${taglineColor}" font-family="Sora_600SemiBold" font-size="10.5" font-weight="600" letter-spacing="3.15">YOUR STORIES. YOUR SCREEN.</text>`,
  );
}

function logoVertical(color = colors.white) {
  const markColor = color === colors.white ? colors.red : color;
  const taglineColor = color === colors.white ? colors.red : color;
  return svg(
    320,
    210,
    '0 0 320 210',
    `<g transform="translate(110 8)">${symbol(markColor)}</g>` +
      `<text x="160" y="143" fill="${color}" text-anchor="middle" font-family="Sora_700Bold" font-size="47" font-weight="700" letter-spacing="-1.8">reelix</text>` +
      `<text x="160" y="174" fill="${taglineColor}" text-anchor="middle" font-family="Sora_600SemiBold" font-size="8.5" font-weight="600" letter-spacing="2">YOUR STORIES. YOUR SCREEN.</text>`,
  );
}

const navigationIcons = {
  achievements: ['#FF6688', '<path d="M8 4h8v4a4 4 0 0 1-8 0V4Z"/><path d="M8 6H5v2a4 4 0 0 0 4 4M16 6h3v2a4 4 0 0 1-4 4M12 12v4m-4 4h8m-7-4h6v4H9z"/>'],
  calendar: ['#E5E7EB', '<rect x="3.5" y="5" width="17" height="16" rx="2"/><path d="M7.5 3v4m9-4v4M3.5 9.5h17M8 13h2m4 0h2m-8 4h2"/>'],
  check: ['#FF2D55', '<path d="m5 12.5 4.2 4.2L19 7"/>'],
  clock: ['#E5E7EB', '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3.5 2"/>'],
  close: ['#E5E7EB', '<path d="m6 6 12 12M18 6 6 18"/>'],
  eye: ['#E5E7EB', '<path d="M2.5 12s3.2-6 9.5-6 9.5 6 9.5 6-3.2 6-9.5 6-9.5-6-9.5-6Z"/><circle cx="12" cy="12" r="2.5"/>'],
  favorites: ['#FF6688', '<path d="M20.2 8.8c0 4.2-8.2 9.3-8.2 9.3S3.8 13 3.8 8.8A4.3 4.3 0 0 1 12 6.6a4.3 4.3 0 0 1 8.2 2.2Z"/>'],
  films: ['#E5E7EB', '<rect x="4" y="3" width="16" height="18" rx="2"/><path d="M4 8h16M4 16h16M8 3v5m8-5v5M8 16v5m8-5v5M10 10.5l4 2-4 2v-4Z" fill="#E5E7EB" stroke="none"/>'],
  filter: ['#E5E7EB', '<path d="M3.5 5h17l-6.5 7.3v5l-4 2v-7L3.5 5Z"/>'],
  fire: ['#FF2D55', '<path d="M12 21a7 7 0 0 0 7-7c0-3.4-2.2-5.8-4.1-8.5-.1 2.2-1.1 3.4-2.1 4.2C12.2 6 10.8 3.8 9 2c.4 4.2-4 7.2-4 12a7 7 0 0 0 7 7Z"/><path d="M12 21a3.5 3.5 0 0 0 3.5-3.5c0-1.6-1-2.8-2.2-4.2-.2 1.1-.8 1.8-1.5 2.2-.6-1.4-1.3-2.3-2.2-3.1.1 2.1-1.1 3.2-1.1 5.1A3.5 3.5 0 0 0 12 21Z"/>'],
  friends: ['#E5E7EB', '<circle cx="9" cy="8" r="3.2"/><path d="M3.5 20v-1.3A5.5 5.5 0 0 1 9 13.2a5.5 5.5 0 0 1 5.5 5.5V20H3.5Z"/><path d="M15.5 5.2a3.2 3.2 0 0 1 0 6.2m2 2.2a5.5 5.5 0 0 1 3 5.1V20h-3.2"/>'],
  home: ['#FF2D55', '<path d="m3 10.8 9-7.3 9 7.3V20a1 1 0 0 1-1 1h-5.5v-6h-5v6H4a1 1 0 0 1-1-1v-9.2Z"/>'],
  'list-add': ['#E5E7EB', '<path d="M4 6h11M4 11h11M4 16h6"/><path d="M17 15v7m-3.5-3.5h7"/>'],
  more: ['#E5E7EB', '<circle cx="5" cy="12" r="1"/><circle cx="12" cy="12" r="1"/><circle cx="19" cy="12" r="1"/>'],
  notifications: ['#E5E7EB', '<path d="M18 9a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9Zm-8 12h4"/>'],
  pause: ['#E5E7EB', '<path d="M8 5h3v14H8zM15 5h3v14h-3z"/>'],
  play: ['#FF2D55', '<path d="M7 4.8a1.5 1.5 0 0 1 2.2-1.3l11 6.9a1.8 1.8 0 0 1 0 3.1l-11 6.9A1.5 1.5 0 0 1 7 19.1V4.8Z"/>'],
  profile: ['#E5E7EB', '<circle cx="12" cy="8" r="4"/><path d="M4 21a8 8 0 0 1 16 0H4Z"/>'],
  rating: ['#FF6688', '<path d="m12 3 2.8 5.8 6.4.9-4.6 4.5 1.1 6.4-5.7-3-5.7 3 1.1-6.4-4.6-4.5 6.4-.9L12 3Z"/>'],
  search: ['#E5E7EB', '<circle cx="10.8" cy="10.8" r="6.8"/><path d="m16 16 4.5 4.5"/>'],
  series: ['#E5E7EB', '<rect x="3" y="5" width="18" height="15" rx="2"/><path d="m10 9 5 3.5-5 3.5V9ZM7 2l2 3m6-3 2 3"/>'],
  settings: ['#E5E7EB', '<path d="M12 8.5a3.5 3.5 0 1 0 0 7 3.5 3.5 0 0 0 0-7Z"/><path d="m19.4 13.5 1.1.8-1.6 2.8-1.3-.5a7.8 7.8 0 0 1-1.5.9l-.2 1.4h-3.2l-.2-1.4a7.8 7.8 0 0 1-1.5-.9l-1.3.5-1.6-2.8 1.1-.8a7 7 0 0 1 0-1.8l-1.1-.8 1.6-2.8 1.3.5a7.8 7.8 0 0 1 1.5-.9l.2-1.4h3.2l.2 1.4a7.8 7.8 0 0 1 1.5.9l1.3-.5 1.6 2.8-1.1.8a7 7 0 0 1 0 1.8Z"/>'],
  share: ['#E5E7EB', '<circle cx="18" cy="5" r="3"/><circle cx="6" cy="12" r="3"/><circle cx="18" cy="19" r="3"/><path d="m8.7 10.6 6.6-4.2m-6.6 7 6.6 4.1"/>'],
  trash: ['#E5E7EB', '<path d="M4 7h16M10 11v6m4-6v6M6 7l1 14h10l1-14M9 7V4h6v3"/>'],
};

const genres = {
  action: '<path d="m26 5 4 11 11-7-6 12 11 3-11 4 6 11-11-6-4 11-4-11-11 6 6-11-11-4 11-3-6-12 11 7z"/>',
  adventure: '<path d="m24 7 15 34L9 28 24 7Z"/><circle cx="24" cy="25" r="3"/><path d="M12 37 8 42l8-2m20-22 4-5-8 2"/>',
  animation: '<path d="M24 6 28 16 39 9 33 20 45 24 33 28 39 39 28 32 24 44 20 32 9 39 15 28 3 24 15 20 9 9 20 16 24 6Z"/><circle cx="24" cy="24" r="4"/>',
  comedy: '<path d="M7 10h34v18a17 17 0 0 1-34 0V10Z"/><path d="M16 20h.1M32 20h.1M16 32c4 4 12 4 16 0"/>',
  documentary: '<rect x="8" y="9" width="32" height="30" rx="3"/><path d="M13 15h12v17H13zM29 17h6m-6 5h6m-6 5h6M4 15v18m40-18v18"/>',
  drama: '<path d="M24 8c-5-6-15-2-15 6 0 10 15 22 15 22S39 24 39 14c0-8-10-12-15-6Z"/><path d="M14 17h6l3-5 4 12 3-7h4"/>',
  horror: '<path d="M24 5 6 39h36L24 5Z"/><path d="M24 17v10m0 6h.1"/>',
  romance: '<path d="M24 39S7 28 7 17a9 9 0 0 1 17-4 9 9 0 0 1 17 4c0 11-17 22-17 22Z"/><path d="M16 24h5l3-5 4 10 3-5h3"/>',
  scifi: '<circle cx="24" cy="24" r="4"/><ellipse cx="24" cy="24" rx="19" ry="8" transform="rotate(-35 24 24)"/><ellipse cx="24" cy="24" rx="19" ry="8" transform="rotate(35 24 24)"/><circle cx="37" cy="13" r="1.5" fill="#FF2D55"/>',
};

const badgeMarks = {
  collector: '<path d="M35 32h26l-3 6H38zM37 41h22l-3 6H40zM40 50h16"/>',
  critic: '<path d="m48 28 3.5 8 8.5 1.2-6.2 6 1.5 8.5-7.3-4-7.3 4 1.5-8.5-6.2-6 8.5-1.2L48 28Z"/>',
  explorer: '<path d="m48 27 16 21-16 17-16-17 16-21Z"/><circle cx="48" cy="48" r="4"/><path d="m51 45 7-7"/>',
  'first-film': '<rect x="34" y="34" width="28" height="27" rx="3"/><path d="M34 42h28m-21-8v8m14-8v8m-14 8v11m14-11v11m-5-10 6 3.5-6 3.5V49Z"/>',
  friend: '<circle cx="43" cy="42" r="7"/><path d="M31 62v-2a12 12 0 0 1 24 0v2H31Zm23-27a7 7 0 1 1 0 14m3 3a11 11 0 0 1 8 10"/>',
  legend: '<path d="m48 28 4.5 9 10 1.5-7.2 7 1.7 10L48 51l-9 4.5 1.7-10-7.2-7 10-1.5 4.5-9Z"/><path d="m39 60-2 8 11-5 11 5-2-8"/>',
  marathon: '<path d="M37 34h22v20H37zM33 59h30m-21-25v-6m10 6v-6"/><path d="m45 38-4 7h6l-2 6 7-9h-6l2-4"/>',
  'series-fan': '<path d="M48 31c-5-5-15-1-15 6 0 9 15 18 15 18s15-9 15-18c0-7-10-11-15-6Z"/><path d="M35 58h26m-21 5h16"/>',
};

for (const [name, [color, paths]] of Object.entries(navigationIcons)) {
  writeSvg(
    `navigation-icons/${name}.svg`,
    svg(
      24,
      24,
      '0 0 24 24',
      `<g fill="none" stroke="${color}" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">${paths}</g>`,
    ),
  );
}

for (const [name, paths] of Object.entries(genres)) {
  writeSvg(
    `genre-icons/${name}.svg`,
    svg(
      48,
      48,
      '0 0 48 48',
      `<g fill="none" stroke="${name === 'action' || name === 'scifi' ? colors.red : colors.pink}" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">${paths}</g>`,
    ),
  );
}

for (const [name, mark] of Object.entries(badgeMarks)) {
  writeSvg(
    `badges/${name}.svg`,
    svg(
      96,
      108,
      '0 0 96 108',
      `<path d="m48 5 30 16v34L48 72 18 55V21L48 5Z" fill="${colors.surface}" stroke="${colors.red}" stroke-width="2.5" stroke-linejoin="round"/>` +
        `<path d="m31 66-2 34 19-9 19 9-2-34" fill="${colors.raised}" stroke="${colors.red}" stroke-width="2.5" stroke-linejoin="round"/>` +
        `<circle cx="48" cy="40" r="19" fill="${colors.black}" stroke="${colors.pink}" stroke-width="2"/>` +
        `<g fill="none" stroke="${colors.pink}" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">${mark}</g>`,
    ),
  );
}

const logos = [
  ['logos/logo-horizontal.svg', logoHorizontal()],
  ['logos/logo-vertical.svg', logoVertical()],
  ['logos/logo-white.svg', logoHorizontal(colors.white, true)],
  ['logos/logo-black.svg', logoHorizontal(colors.black, true)],
  ['logos/symbol.svg', svg(100, 100, '0 0 100 100', symbol(colors.red))],
];

for (const [file, contents] of logos) writeSvg(file, contents);

writeSvg(
  'app-icons/app-icon.svg',
  svg(
    1024,
    1024,
    '0 0 1024 1024',
    `<rect width="1024" height="1024" rx="230" fill="${colors.black}"/>` +
      `<g transform="translate(102 102) scale(8.2)">${symbol(colors.red)}</g>`,
  ),
);
writeSvg(
  'app-icons/app-icon-light.svg',
  svg(
    1024,
    1024,
    '0 0 1024 1024',
    `<rect width="1024" height="1024" rx="230" fill="${colors.red}"/>` +
      `<g transform="translate(102 102) scale(8.2)">${symbol(colors.white)}</g>`,
  ),
);
writeSvg(
  'app-icons/adaptive-icon-foreground.svg',
  svg(
    432,
    432,
    '0 0 432 432',
    `<g transform="translate(56 56) scale(3.2)">${symbol(colors.red)}</g>`,
  ),
);
writeSvg(
  'app-icons/adaptive-icon-monochrome.svg',
  svg(
    432,
    432,
    '0 0 432 432',
    `<g transform="translate(56 56) scale(3.2)">${symbol(colors.white)}</g>`,
  ),
);
writeSvg(
  'app-icons/favicon.svg',
  svg(
    48,
    48,
    '0 0 48 48',
    `<rect width="48" height="48" rx="11" fill="${colors.black}"/>` +
      `<g transform="translate(4 4) scale(.4)">${symbol(colors.red)}</g>`,
  ),
);

writeSvg(
  'ui/empty-state.svg',
  svg(
    400,
    300,
    '0 0 400 300',
    `<rect x="1" y="1" width="398" height="298" rx="24" fill="${colors.black}" stroke="${colors.raised}"/>` +
      `<g transform="translate(152 36) scale(.96)">${symbol(colors.red)}</g>` +
      `<path d="M150 154h100m-82 14h64" stroke="${colors.raised}" stroke-width="4" stroke-linecap="round"/>` +
      `<circle cx="200" cy="124" r="66" fill="none" stroke="${colors.raised}" stroke-width="1"/>`,
  ),
);
writeSvg(
  'ui/splash-screen.svg',
  svg(
    400,
    600,
    '0 0 400 600',
    `<rect width="400" height="600" fill="${colors.black}"/>` +
      `<circle cx="200" cy="240" r="164" fill="${colors.surface}"/>` +
      `<g transform="translate(150 190) scale(1)">${symbol(colors.red)}</g>` +
      `<text x="200" y="370" fill="${colors.white}" text-anchor="middle" font-family="Sora, Arial, sans-serif" font-size="46" font-weight="700" letter-spacing="-1.5">reelix</text>` +
      `<text x="200" y="401" fill="${colors.red}" text-anchor="middle" font-family="Sora, Arial, sans-serif" font-size="9" font-weight="600" letter-spacing="2.1">YOUR STORIES. YOUR SCREEN.</text>`,
  ),
);
writeSvg(
  'docs/palette.svg',
  svg(
    640,
    164,
    '0 0 640 164',
    `<rect width="640" height="164" rx="18" fill="${colors.black}"/>` +
      [
        ['BRAND RED', colors.red],
        ['PINK', colors.pink],
        ['BLACK', colors.black],
        ['SURFACE', colors.surface],
        ['RAISED', colors.raised],
        ['WHITE', colors.white],
        ['MUTED', colors.muted],
      ]
        .map(
          ([name, color], index) =>
            `<rect x="${20 + index * 86}" y="22" width="70" height="70" rx="14" fill="${color}" stroke="${color === colors.black || color === colors.white ? colors.raised : 'none'}"/>` +
            `<text x="${55 + index * 86}" y="118" fill="${colors.muted}" text-anchor="middle" font-family="Arial, sans-serif" font-size="9" font-weight="600" letter-spacing=".35">${name}</text>` +
            `<text x="${55 + index * 86}" y="136" fill="${colors.white}" text-anchor="middle" font-family="Arial, sans-serif" font-size="9">${color}</text>`,
        )
        .join(''),
  ),
);

function findChrome() {
  const candidates = [
    process.env.CHROME_PATH,
    'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
    'C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe',
  ].filter(Boolean);
  const browser = candidates.find((candidate) => existsSync(candidate));
  if (!browser) {
    throw new Error('Chrome or Edge is required to export the transparent PNG assets. Set CHROME_PATH to its executable.');
  }
  return browser;
}

const pngExports = [
  ['app-icons/app-icon.svg', 'app-icons/app-icon.png', 1024, 1024],
  ['app-icons/app-icon-light.svg', 'app-icons/app-icon-light.png', 1024, 1024],
  ['app-icons/adaptive-icon-foreground.svg', 'app-icons/adaptive-icon-foreground.png', 432, 432],
  ['app-icons/adaptive-icon-monochrome.svg', 'app-icons/adaptive-icon-monochrome.png', 432, 432],
  ['app-icons/favicon.svg', 'app-icons/favicon.png', 48, 48],
  ['logos/logo-horizontal.svg', 'logos/logo-horizontal.png', 800, 240],
  ['logos/logo-vertical.svg', 'logos/logo-vertical.png', 520, 342],
  ['logos/logo-white.svg', 'logos/logo-white.png', 800, 240],
  ['logos/logo-black.svg', 'logos/logo-black.png', 800, 240],
  ['logos/symbol.svg', 'logos/symbol.png', 512, 512],
];

const chrome = findChrome();
const temporaryDirectory = path.join(tmpdir(), `reelix-brand-render-${process.pid}`);
const boldFont = path.join(
  mobileDirectory,
  'node_modules',
  '@expo-google-fonts',
  'sora',
  '700Bold',
  'Sora_700Bold.ttf',
);
const semiBoldFont = path.join(
  mobileDirectory,
  'node_modules',
  '@expo-google-fonts',
  'sora',
  '600SemiBold',
  'Sora_600SemiBold.ttf',
);
if (!existsSync(boldFont) || !existsSync(semiBoldFont)) {
  throw new Error('The configured Sora bold and semibold font files are required to render the brand wordmarks.');
}
mkdirSync(temporaryDirectory, { recursive: true });

try {
  for (const [source, target, width, height] of pngExports) {
    const sourcePath = path.join(brandDirectory, source);
    const targetPath = path.join(brandDirectory, target);
    const htmlPath = path.join(temporaryDirectory, `${path.basename(target, '.png')}.html`);
    const boldFontUrl = pathToFileURL(boldFont).href;
    const semiBoldFontUrl = pathToFileURL(semiBoldFont).href;
    const sourceSvg = readFileSync(sourcePath, 'utf8');
    writeFileSync(
      htmlPath,
      `<!doctype html><html><head><meta charset="utf-8"><style>` +
        `@font-face{font-family:'Sora_700Bold';src:url('${boldFontUrl}') format('truetype');font-weight:700}` +
        `@font-face{font-family:'Sora_600SemiBold';src:url('${semiBoldFontUrl}') format('truetype');font-weight:600}` +
        `html,body{width:100%;height:100%;margin:0;background:transparent;overflow:hidden}` +
        `svg{display:block;width:100%;height:100%}` +
        `</style></head><body>${sourceSvg}</body></html>`,
      'utf8',
    );
    execFileSync(
      chrome,
      [
        '--headless=new',
        '--disable-gpu',
        '--no-first-run',
        '--no-default-browser-check',
        '--allow-file-access-from-files',
        '--hide-scrollbars',
        '--force-device-scale-factor=1',
        '--default-background-color=00000000',
        `--user-data-dir=${path.join(temporaryDirectory, 'chrome-profile')}`,
        `--window-size=${width},${height}`,
        `--screenshot=${targetPath}`,
        pathToFileURL(htmlPath).href,
      ],
      { stdio: 'inherit' },
    );
    process.stdout.write(`${target} (${width}x${height})\n`);
  }
} finally {
  rmSync(temporaryDirectory, { recursive: true, force: true });
}
