import { createServer } from "node:http";
import { createHash } from "node:crypto";
import { readFile, readdir, stat } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(scriptDirectory, "../..");
const webglRoot = path.join(repositoryRoot, "Builds", "WebGL");
const buildRoot = path.join(webglRoot, "Build");
const pagePath = path.join(scriptDirectory, "index.html");
const portArgument = process.argv.indexOf("--port");
const port = portArgument >= 0 ? Number(process.argv[portArgument + 1]) : 4173;

function findNamedFile(names, includes, label) {
  const found = names.find((name) => includes.every((part) => name.includes(part)));
  if (!found) {
    throw new Error("The WebGL Build directory is missing its " + label + " file. Found: " + names.join(", "));
  }
  return found;
}

let buildFiles;
try {
  const names = await readdir(buildRoot);
  buildFiles = {
    loaderFile: findNamedFile(names, [".loader.js"], "loader"),
    dataFile: findNamedFile(names, [".data"], "data"),
    frameworkFile: findNamedFile(names, [".framework.js"], "framework"),
    wasmFile: findNamedFile(names, [".wasm"], "WebAssembly")
  };
  const artifactSha256 = {};
  for (const filename of Object.values(buildFiles)) {
    const file = await stat(path.join(buildRoot, filename));
    if (!file.isFile()) throw new Error("Build artifact is not a file: " + filename);
    const bytes = await readFile(path.join(buildRoot, filename));
    artifactSha256[filename] = createHash("sha256").update(bytes).digest("hex");
  }
  buildFiles.artifactSha256 = artifactSha256;
  buildFiles.cacheKey = createHash("sha256")
    .update(JSON.stringify(artifactSha256))
    .digest("hex")
    .slice(0, 16);
} catch (error) {
  console.error("No usable local WebGL build was found under " + buildRoot);
  console.error(error.message);
  process.exit(1);
}

const htmlTemplate = await readFile(pagePath, "utf8");
const html = htmlTemplate.replace("__BUILD_MANIFEST__", JSON.stringify(buildFiles));

function contentType(filename) {
  const uncompressedName = filename.toLowerCase().replace(/\.br$/, "").replace(/\.gz$/, "");
  if (uncompressedName.endsWith(".wasm")) return "application/wasm";
  if (uncompressedName.endsWith(".js")) return "text/javascript; charset=utf-8";
  if (uncompressedName.endsWith(".data")) return "application/octet-stream";
  if (uncompressedName.endsWith(".json")) return "application/json; charset=utf-8";
  if (uncompressedName.endsWith(".png")) return "image/png";
  return "application/octet-stream";
}

function compressionEncoding(filename) {
  if (filename.endsWith(".br")) return "br";
  if (filename.endsWith(".gz")) return "gzip";
  return null;
}

const server = createServer(async (request, response) => {
  const url = new URL(request.url || "/", "http://" + (request.headers.host || "localhost"));
  if (url.pathname === "/healthz") {
    response.writeHead(200, { "Content-Type": "application/json; charset=utf-8" });
    response.end(JSON.stringify({ ok: true, buildFiles }));
    return;
  }

  if (url.pathname === "/" || url.pathname === "/signal-garden/play" || url.pathname === "/signal-garden/play/") {
    response.writeHead(200, { "Content-Type": "text/html; charset=utf-8", "Cache-Control": "no-store" });
    response.end(html);
    return;
  }

  const buildPrefix = "/game-assets/signal-garden/local/Build/";
  if (url.pathname.startsWith(buildPrefix)) {
    const filename = decodeURIComponent(url.pathname.slice(buildPrefix.length));
    if (filename !== path.basename(filename) || filename.includes("..")) {
      response.writeHead(400);
      response.end("Invalid build path");
      return;
    }

    try {
      const filePath = path.join(buildRoot, filename);
      const bytes = await readFile(filePath);
      const headers = {
        "Content-Type": contentType(filename),
        "Content-Length": bytes.length,
        "Cache-Control": "public, max-age=0, must-revalidate",
        "X-Content-Type-Options": "nosniff"
      };
      const encoding = compressionEncoding(filename);
      if (encoding) headers["Content-Encoding"] = encoding;
      response.writeHead(200, headers);
      response.end(bytes);
    } catch {
      response.writeHead(404, { "Content-Type": "text/plain; charset=utf-8" });
      response.end("Build file not found");
    }
    return;
  }

  if (url.pathname.startsWith("/game-assets/signal-garden/local/StreamingAssets/")) {
    const relativePath = decodeURIComponent(url.pathname.slice("/game-assets/signal-garden/local/StreamingAssets/".length));
    const filePath = path.resolve(webglRoot, "StreamingAssets", relativePath);
    const streamingRoot = path.resolve(webglRoot, "StreamingAssets");
    if (!filePath.startsWith(streamingRoot + path.sep)) {
      response.writeHead(400);
      response.end("Invalid streaming asset path");
      return;
    }

    try {
      const bytes = await readFile(filePath);
      response.writeHead(200, {
        "Content-Type": contentType(filePath),
        "Content-Length": bytes.length,
        "Cache-Control": "public, max-age=0, must-revalidate"
      });
      response.end(bytes);
    } catch {
      response.writeHead(404);
      response.end("Streaming asset not found");
    }
    return;
  }

  response.writeHead(404, { "Content-Type": "text/plain; charset=utf-8" });
  response.end("Not found");
});

server.listen(port, "127.0.0.1", () => {
  console.log("Signal Garden local host preview: http://127.0.0.1:" + port + "/signal-garden/play/");
  console.log("Asset base: http://127.0.0.1:" + port + "/game-assets/signal-garden/local");
  console.log("Exact WebGL files: " + JSON.stringify(buildFiles));
});
