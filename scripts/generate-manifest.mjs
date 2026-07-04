import { createHash } from "node:crypto";
import { readFile, writeFile } from "node:fs/promises";

const plugin = {
  guid: "00dff61d-951a-46f9-a40b-9f1ba1b78a9e",
  name: "Inscura",
  description: "Import movie metadata, people and images from the Inscura local API.",
  overview: "Inscura movie metadata provider",
  owner: "Inscura",
  category: "Metadata"
};

const defaults = {
  repository: "InscuraApp/inscura-jellyfin-plugin",
  releaseBranch: "release",
  targetAbi: "10.11.11.0",
  changelog: "Upgrade to Jellyfin 10.11.11 / .NET 9. Add person metadata and image providers.",
  csproj: "Inscura.Jellyfin.Plugin.csproj"
};

async function main() {
  const args = parseArgs(process.argv.slice(2));
  const csproj = args.csproj ?? defaults.csproj;
  const repository = args.repository ?? defaults.repository;
  const releaseBranch = args.releaseBranch ?? defaults.releaseBranch;
  const targetAbi = args.targetAbi ?? defaults.targetAbi;
  const changelog = args.changelog ?? process.env.CHANGELOG ?? defaults.changelog;
  const output = requireArg(args.output, "output");
  const zip = requireArg(args.zip, "zip");
  const version = await readVersion(csproj);
  const checksum = await md5(zip);

  const versionEntry = {
    version,
    changelog,
    targetAbi,
    sourceUrl: `https://cdn.jsdelivr.net/gh/${repository}@${releaseBranch}/releases/Inscura_${version}.zip`,
    checksum,
    timestamp: new Date().toISOString().replace(/\.\d{3}Z$/, "Z")
  };

  const versions = [versionEntry];
  const manifest = [
    {
      ...plugin,
      versions
    }
  ];

  await writeFile(output, JSON.stringify(manifest, null, 2) + "\n", "utf8");
}

function parseArgs(values) {
  const args = {};
  for (let index = 0; index < values.length; index += 1) {
    const key = values[index];
    if (!key.startsWith("--")) {
      throw new Error(`Unexpected argument: ${key}`);
    }

    const name = key.slice(2);
    const value = values[index + 1];
    if (!value || value.startsWith("--")) {
      throw new Error(`Missing value for --${name}`);
    }

    args[name] = value;
    index += 1;
  }

  return args;
}

function requireArg(value, name) {
  if (!value) {
    throw new Error(`Missing required argument --${name}`);
  }

  return value;
}

async function readVersion(csproj) {
  const content = await readFile(csproj, "utf8");
  const match = content.match(/<Version>([^<]+)<\/Version>/);
  if (!match) {
    throw new Error(`Unable to read <Version> from ${csproj}`);
  }

  return match[1].trim();
}

async function md5(filePath) {
  const file = await readFile(filePath);
  return createHash("md5").update(file).digest("hex");
}

main().catch(error => {
  console.error(error.message);
  process.exitCode = 1;
});
