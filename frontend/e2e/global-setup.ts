import { execSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '..', '..');
const frontendDir = path.join(repoRoot, 'frontend');
const wwwroot = path.join(repoRoot, 'src', 'Greenhouse.Api', 'wwwroot');
const dist = path.join(frontendDir, 'dist');

export default async function globalSetup(): Promise<void> {
  execSync('npm run build', { cwd: frontendDir, stdio: 'inherit', shell: true });
  fs.mkdirSync(wwwroot, { recursive: true });
  fs.cpSync(dist, wwwroot, { recursive: true });
  execSync('dotnet build Greenhouse.sln -c Release --verbosity minimal', {
    cwd: repoRoot,
    stdio: 'inherit',
    shell: true,
  });
}
