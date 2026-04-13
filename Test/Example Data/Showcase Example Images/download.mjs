#!/usr/bin/env node
/**
 * Downloads showcase images from Wikimedia Commons.
 * Skips files already downloaded. Retries failures automatically.
 *
 * Usage:
 *   node download.mjs            # download all missing images
 *   node download.mjs --delay 8  # custom delay between downloads (seconds, default 5)
 *   node download.mjs --retry 5  # max retries per image (default 3)
 */
import https from "https";
import fs from "fs";
import path from "path";

// ── Config ──────────────────────────────────────────────────────────────────
const args = process.argv.slice(2);
const flag = (name, fallback) => {
  const i = args.indexOf(`--${name}`);
  return i >= 0 && args[i + 1] ? Number(args[i + 1]) : fallback;
};
const DELAY_SEC = flag("delay", 5);
const MAX_RETRIES = flag("retry", 3);
const BASE_DIR = path.dirname(new URL(import.meta.url).pathname).replace(/^\/([A-Z]:)/, "$1");
const UA = "CollectiblesShowcase/1.0 (educational project; https://github.com)";

// ── Image manifest ──────────────────────────────────────────────────────────
// Each entry: [folder, local_filename, Wikimedia_Commons_filename]
const IMAGES = [
  // ── Apple / Mac ───────────────────────────────────────────────────────────
  // ["vintage_computers/apple_mac", "macintosh_512k_desktop.jpg", "Apple_Macintosh_512K_Desktop.jpg"],
  // ["vintage_computers/apple_mac", "macintosh_512k_enhanced.jpg", "Macintosh_512k_enhanced.jpg"],
  // ["vintage_computers/apple_mac", "quadra_650.png",             "Quadra_650.png"],

  // // ── TRS-80 ────────────────────────────────────────────────────────────────
  // ["vintage_computers/trs80", "trs80_model_1.jpg",   "TRS-80_Model_I_-_Rechnermuseum_Cropped.jpg"],
  // ["vintage_computers/trs80", "trs80_model_4.jpg",   "RadioShack_TRS-80_mod4-IMG_5778.jpg"],
  // ["vintage_computers/trs80", "trs80.jpg",           "Trs-80.jpg"],
  // ["vintage_computers/trs80", "trs80_coco1.jpg",     "TRS-80_Color_Computer_1_front_right.jpg"],
  // ["vintage_computers/trs80", "trs80_model_100.jpg", "TRS-80_Model_100.jpg"],
  // ["vintage_computers/trs80", "trs80_coco3.jpg",     "TRS-80_Color_Computer_3.jpg"],
  // ["vintage_computers/trs80", "trs80_videotex.jpg",  "TRS-80_Videotex_terminal_retouched.jpg"],
  // ["vintage_computers/trs80", "trs80_mc10.jpg",      "TRS-80_Model_MC-10.jpg"],

  // // ── Commodore ─────────────────────────────────────────────────────────────
  // ["vintage_computers/commodore", "amiga_500_plus.jpg", "Commodore_Amiga_500%2B.jpg"],
  // ["vintage_computers/commodore", "amiga_1200.jpg",     "Commodore_Amiga_A1200.jpg"],
  // ["vintage_computers/commodore", "commodore_64.png",   "Commodore-64-Computer-FL.png"],

  // // ── MSX ───────────────────────────────────────────────────────────────────
  // ["vintage_computers/msx", "msx_turbo_r.jpg",            "MSX_Turbo_R_(1990).jpg"],
  // ["vintage_computers/msx", "msx_kuvt2.jpg",              "%D0%9A%D0%A3%D0%92%D0%A22,_%D0%BE%D0%B1%D1%89%D0%B8%D0%B9_%D0%B2%D0%B8%D0%B4.jpg"],
  // ["vintage_computers/msx", "msx2_kuvt2_disassembled.jpg", "MSX2_%D0%9A%D0%A3%D0%92%D0%A22_%D0%B2_%D1%80%D0%B0%D0%B7%D0%B1%D0%BE%D1%80%D0%B5,_%D0%BA%D0%BB%D0%B0%D0%B2%D0%B8%D0%B0%D1%82%D1%83%D1%80%D0%B0_%D0%B8_%D1%80%D0%B0%D0%B7%D1%8A%D1%91%D0%BC%D1%8B_%D0%BA%D0%B0%D1%80%D1%82%D1%80%D0%B8%D0%B4%D0%B6%D0%B5%D0%B9.jpg"],

  // // ── IBM ───────────────────────────────────────────────────────────────────
  // ["vintage_computers/ibm", "ibm_ps2_model_50.jpg",    "IBM_PS2_model_50.jpg"],
  // ["vintage_computers/ibm", "ibm_ps1.jpg",             "IBM_PS_1_Front_(2954638472)_(cropped).jpg"],
  // ["vintage_computers/ibm", "ibm_ps2_diefenbunker.jpg", "IBM_PS-2_in_Diefenbunker.jpg"],
  // ["vintage_computers/ibm", "ibm_pc_xt_5160.jpg",      "IBM_PC_XT_5160.jpg"],
  // ["vintage_computers/ibm", "ibm_pc_at.jpg",           "IBM_PC_AT.jpg"],

  // // ── Workstations (Sun / NeXT) ─────────────────────────────────────────────
  // ["vintage_computers/workstations", "sparcstation_1.jpg",   "SPARCstation_1.jpg"],
  // ["vintage_computers/workstations", "sparcstation_ipx.jpg", "Sun_SPARCstation_IPX.jpg"],
  // ["vintage_computers/workstations", "next_cube.jpg",        "NEXT_Cube-IMG_7154.jpg"],

  // // ── Compaq ────────────────────────────────────────────────────────────────
  // ["vintage_computers/compaq", "compaq_portable.jpg",   "Compaq_portable.jpg"],
  // ["vintage_computers/compaq", "compaq_deskpro_ep.jpg", "Compaq_deskpro_EP_-_1999_desktop.jpg"],

  // ── Atari 2600 Cartridges ───────────────────────────────────────────────
  ["video_games/atari_2600_cartridges", "pitfall.jpg",              "Cartucho_de_Atari_2600_del_juego_Pitfall.jpg"],
  ["video_games/atari_2600_cartridges", "enduro.jpg",               "Cartucho_de_Atari_2600_del_juego_Enduro_con_etiqueta.jpg"],
  ["video_games/atari_2600_cartridges", "championship_soccer.jpg",  "Cartucho_de_Atari_2600_del_juego_Championship_Soccer_con_etiqueta.jpg"],
  ["video_games/atari_2600_cartridges", "chopper_command.jpg",      "Cartucho_de_Atari_2600_del_juego_Chopper_Command.jpg"],
  ["video_games/atari_2600_cartridges", "keystone_kapers.jpg",      "Cartucho_de_Atari_2600_del_juego_Keystone_Kapers.jpg"],
  ["video_games/atari_2600_cartridges", "chopper_command_label.jpg", "Cartucho_de_Atari_2600_del_juego_Chopper_Command_con_etiqueta.jpg"],

  // // ── Vintage Software ──────────────────────────────────────────────────────
  // ["vintage_software", "ibm_dos_330_box.jpg",      "IBM_DOS_3.30_Retail_Box.jpg"],
  // ["vintage_software", "norton_utilities_60.png",   "Norton-utilities-6.0.png"],
  // ["vintage_software", "wordperfect_51_dos.png",    "Wordperfect-5.1-dos.png"],
  // ["vintage_software", "dosbox_screenshot.png",     "DOSBox_v0.74-3_screenshot.png"],
  // ["vintage_software", "dre.png",                   "Dre.png"],
];

// ── Helpers ─────────────────────────────────────────────────────────────────
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

function fetchJson(url) {
  return new Promise((resolve, reject) => {
    https.get(url, { headers: { "User-Agent": UA } }, (res) => {
      let data = "";
      res.on("data", (c) => (data += c));
      res.on("end", () => {
        try { resolve(JSON.parse(data)); }
        catch (e) { reject(e); }
      });
    }).on("error", reject);
  });
}

function downloadFile(url, dest) {
  return new Promise((resolve, reject) => {
    const follow = (u, hops = 0) => {
      if (hops > 5) { reject(new Error("Too many redirects")); return; }
      https.get(u, { headers: { "User-Agent": UA } }, (res) => {
        if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
          follow(res.headers.location, hops + 1);
          return;
        }
        if (res.statusCode === 429) {
          res.resume();
          reject(new Error("RATE_LIMITED"));
          return;
        }
        if (res.statusCode !== 200) {
          res.resume();
          reject(new Error(`HTTP ${res.statusCode}`));
          return;
        }
        const file = fs.createWriteStream(dest);
        res.pipe(file);
        file.on("finish", () => { file.close(); resolve(); });
        file.on("error", (e) => { fs.unlink(dest, () => {}); reject(e); });
      }).on("error", (e) => { reject(e); });
    };
    follow(url);
  });
}

async function getDirectUrl(wikiFilename) {
  const apiUrl =
    `https://commons.wikimedia.org/w/api.php?action=query` +
    `&titles=File:${wikiFilename}&prop=imageinfo&iiprop=url&format=json`;
  const json = await fetchJson(apiUrl);
  const pages = json.query.pages;
  for (const pid of Object.keys(pages)) {
    if (pages[pid].imageinfo) return pages[pid].imageinfo[0].url;
  }
  throw new Error("File not found on Wikimedia Commons");
}

// ── Main ────────────────────────────────────────────────────────────────────
console.log(`\nShowcase Image Downloader`);
console.log(`  Delay: ${DELAY_SEC}s between downloads | Max retries: ${MAX_RETRIES}\n`);

// Ensure all target directories exist
for (const folder of new Set(IMAGES.map(([f]) => f))) {
  fs.mkdirSync(path.join(BASE_DIR, folder), { recursive: true });
}

let downloaded = 0, skipped = 0, failedItems = [];

for (let i = 0; i < IMAGES.length; i++) {
  const [folder, name, wikiFile] = IMAGES[i];
  const dest = path.join(BASE_DIR, folder, name);
  const label = `${folder}/${name}`;

  // Skip already-downloaded files
  if (fs.existsSync(dest) && fs.statSync(dest).size > 1000) {
    console.log(`  [${i + 1}/${IMAGES.length}] SKIP  ${label} (already exists)`);
    skipped++;
    continue;
  }

  let ok = false;
  for (let attempt = 1; attempt <= MAX_RETRIES; attempt++) {
    try {
      if (i > 0 || attempt > 1) {
        const wait = attempt > 1 ? DELAY_SEC * 2 * attempt : DELAY_SEC;
        process.stdout.write(`  [${i + 1}/${IMAGES.length}] ` + (attempt > 1 ? `RETRY ${attempt}/${MAX_RETRIES} ` : "") + `${label} ...`);
        await sleep(wait * 1000);
      } else {
        process.stdout.write(`  [${i + 1}/${IMAGES.length}] ${label} ...`);
      }

      const directUrl = await getDirectUrl(wikiFile);
      await sleep(500);
      await downloadFile(directUrl, dest);

      const size = fs.statSync(dest).size;
      console.log(` OK (${(size / 1024).toFixed(0)} KB)`);
      downloaded++;
      ok = true;
      break;
    } catch (e) {
      if (fs.existsSync(dest)) fs.unlinkSync(dest);
      if (e.message === "RATE_LIMITED" && attempt < MAX_RETRIES) {
        const backoff = DELAY_SEC * 2 * (attempt + 1);
        console.log(` 429 rate limited, waiting ${backoff}s...`);
        await sleep(backoff * 1000);
      } else {
        console.log(` FAIL (${e.message})`);
        if (attempt >= MAX_RETRIES) failedItems.push(label);
      }
    }
  }
}

// ── Summary ─────────────────────────────────────────────────────────────────
console.log(`\n── Summary ──`);
console.log(`  Downloaded: ${downloaded}`);
console.log(`  Skipped:    ${skipped} (already existed)`);
console.log(`  Failed:     ${failedItems.length}`);
if (failedItems.length) {
  console.log(`\n  Failed files (re-run this script to retry):`);
  failedItems.forEach((f) => console.log(`    - ${f}`));
}
console.log();
