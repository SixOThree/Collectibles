import fs from 'fs';
import path from 'path';

export type SeedManifest = {
  users: {
    admin: { email: string; password: string; displayName: string };
    regular: { email: string; password: string; displayName: string };
    otherOwner: { email: string; password: string; displayName: string };
  };
  showcases: {
    regularPrivate: { name: string; hash: string };
    regularPublic: { name: string; hash: string };
    otherPrivate: { name: string; hash: string };
  };
  items: {
    regularRoot: { name: string; hash: string };
    regularChild: { name: string; hash: string };
    otherPrivate: { name: string; hash: string };
  };
};

export function readSeedManifest(): SeedManifest {
  const manifestPath = path.resolve(
    __dirname,
    '../../../../Source/Collectibles.Web/App_Data/playwright/seed-manifest.json'
  );

  return JSON.parse(fs.readFileSync(manifestPath, 'utf8')) as SeedManifest;
}
