#!/usr/bin/env node
/**
 * audit-slices.mjs — auditoria mecânica da arquitetura em fatias verticais do backend .NET do Reelix.
 *
 * Uso (da raiz do repositório):
 *   node .claude/skills/refactor-slices/scripts/audit-slices.mjs
 *   node .claude/skills/refactor-slices/scripts/audit-slices.mjs --scope backend
 *   node .claude/skills/refactor-slices/scripts/audit-slices.mjs --module Lists
 *   node .claude/skills/refactor-slices/scripts/audit-slices.mjs --json
 *   node .claude/skills/refactor-slices/scripts/audit-slices.mjs --strict [--allow rule-a,rule-b]
 *
 * Opções:
 *   --scope <path>        diretório a varrer (padrão: backend). Aceita também uma pasta de projeto
 *                         (ex.: backend/Reelix.Application).
 *   --module <nome>       limita os achados a um módulo (Catalog, Lists, Tracking, ...).
 *   --json                saída JSON em vez de tabela markdown.
 *   --strict              exit 1 se sobrar qualquer achado (exceto regras de --allow).
 *   --allow <r1,r2>       regras toleradas no modo --strict (exceções documentadas no plano).
 *   --include-tests       inclui Reelix.UnitTests na varredura (padrão: ignora).
 *   --max-service-methods <n>  limite de métodos públicos por *Service.cs (padrão: 5).
 *   --max-endpoints-lines <n>  limite de linhas por *Endpoints.cs (padrão: 150).
 *   --max-file-lines <n>       limite de linhas por arquivo (padrão: 400).
 *
 * O script é um piso, não um teto: acha o que é verificável por regex/contagem.
 * O auditor (subagent) ainda precisa ler o código e julgar a semântica (regra de negócio em
 * handler, use case que só repassa chamada, port grande demais, etc.).
 *
 * Achados com confidence "suspeita" são heurísticos e exigem confirmação por leitura.
 *
 * Exit codes: 0 relatório gerado · 1 --strict com achados · 2 escopo inválido/falha.
 */

import { readdirSync, readFileSync, statSync } from "node:fs";
import { basename, join, relative, resolve, sep } from "node:path";

// ---------------------------------------------------------------------------
// CLI
// ---------------------------------------------------------------------------

const args = process.argv.slice(2);
const jsonOutput = args.includes("--json");
const strict = args.includes("--strict");
const includeTests = args.includes("--include-tests");

function readOption(name) {
  const equals = args.find((arg) => arg.startsWith(`${name}=`));
  if (equals) return equals.slice(name.length + 1);
  const index = args.indexOf(name);
  if (index !== -1 && index + 1 < args.length && !args[index + 1].startsWith("--")) {
    return args[index + 1];
  }
  return undefined;
}

function readNumber(name, fallback) {
  const raw = readOption(name);
  const value = raw === undefined ? NaN : Number(raw);
  return Number.isFinite(value) && value > 0 ? value : fallback;
}

const MAX_SERVICE_METHODS = readNumber("--max-service-methods", 5);
const MAX_ENDPOINTS_LINES = readNumber("--max-endpoints-lines", 150);
const MAX_FILE_LINES = readNumber("--max-file-lines", 400);
const moduleFilter = readOption("--module");
const allowedRules = new Set(
  (readOption("--allow") ?? "").split(",").map((rule) => rule.trim()).filter(Boolean),
);

const root = process.cwd();
const scope = resolve(root, readOption("--scope") ?? "backend");

// ---------------------------------------------------------------------------
// Utilidades
// ---------------------------------------------------------------------------

const SKIP_DIRS = new Set(["bin", "obj", "Migrations", "node_modules", ".git", ".vs", "wwwroot"]);
const SEVERITY_ORDER = { high: 0, medium: 1, low: 2 };

function isDirectory(path) {
  try {
    return statSync(path).isDirectory();
  } catch {
    return false;
  }
}

function walk(dir) {
  const files = [];
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (entry.name.startsWith(".") || SKIP_DIRS.has(entry.name)) continue;
    const full = join(dir, entry.name);
    if (entry.isDirectory()) files.push(...walk(full));
    else if (entry.isFile() && entry.name.endsWith(".cs")) files.push(full);
  }
  return files;
}

/** Troca comentários e literais de string por espaços, preservando quebras de linha. */
function stripNoise(source) {
  const blank = (match) => match.replace(/[^\n]/g, " ");
  return source.replace(
    /\/\/[^\n]*|\/\*[\s\S]*?\*\/|"""[\s\S]*?"""|@\$?"(?:[^"]|"")*"|\$@"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\\n])*"|'(?:\\.|[^'\\\n])'/g,
    blank,
  );
}

function lineAt(text, index) {
  let line = 1;
  for (let i = 0; i < index; i += 1) if (text.charCodeAt(i) === 10) line += 1;
  return line;
}

function countLines(text) {
  const parts = text.split(/\r?\n/);
  return parts[parts.length - 1] === "" ? parts.length - 1 : parts.length;
}

function matchesOf(text, pattern) {
  const flags = pattern.flags.includes("g") ? pattern.flags : `${pattern.flags}g`;
  return [...text.matchAll(new RegExp(pattern.source, flags))];
}

const toPosix = (path) => path.split(sep).join("/");

// ---------------------------------------------------------------------------
// Modelo de arquivos: projeto, módulo e pasta dentro do módulo
// ---------------------------------------------------------------------------

function classify(absPath) {
  const relFromRoot = toPosix(relative(root, absPath));
  const segments = relFromRoot.split("/");
  const projectIndex = segments.findIndex((segment) => /^Reelix\.[A-Za-z]+$/.test(segment));
  if (projectIndex === -1) return { project: null, module: "(fora de projeto)", sub: [] };

  const project = segments[projectIndex].slice("Reelix.".length);
  const inside = segments.slice(projectIndex + 1); // inclui o nome do arquivo
  let moduleName = "(raiz)";
  let sub = [];

  if (project === "Infrastructure") {
    if (inside[0] === "Persistence") {
      moduleName = inside.length > 2 ? inside[1] : "(compartilhado)";
      sub = inside.length > 2 ? inside.slice(2) : inside.slice(1);
    } else {
      moduleName = inside.length > 1 ? inside[0] : "(compartilhado)";
      sub = inside.length > 1 ? inside.slice(1) : inside;
    }
  } else if (inside.length > 1) {
    moduleName = inside[0];
    sub = inside.slice(1);
  } else {
    sub = inside;
  }

  return { project, module: moduleName, sub };
}

function loadFiles() {
  const files = [];
  for (const abs of walk(scope)) {
    const info = classify(abs);
    if (info.project === "UnitTests" && !includeTests) continue;
    const content = readFileSync(abs, "utf8");
    files.push({
      abs,
      rel: toPosix(relative(root, abs)),
      name: basename(abs),
      ...info,
      content,
      code: stripNoise(content),
      lines: countLines(content),
    });
  }
  return files;
}

// ---------------------------------------------------------------------------
// Achados
// ---------------------------------------------------------------------------

const findings = [];

function add(file, rule, severity, line, message, extra = {}) {
  findings.push({
    module: file.module,
    project: file.project,
    severity,
    rule,
    file: file.rel,
    line,
    message,
    confidence: extra.confidence ?? "certa",
    ...(extra.occurrences ? { occurrences: extra.occurrences } : {}),
  });
}

const USER_ID_HELPER = /\bstatic\s+bool\s+TryGet\w*Id\s*\(/g;

// Tipos injetáveis relevantes para a regra 1:1 do handler.
const DEP_TYPE = /\b(I?[A-Z]\w*(?:Service|UseCase|Repository|Processor))\b\s+[a-z_]\w*/g;
const NOT_A_DEP = /^(IServiceProvider|IServiceCollection|IServiceScope\w*|IServiceScopeFactory)$/;

function dependencyTypes(text) {
  const types = new Set();
  for (const match of matchesOf(text, DEP_TYPE)) {
    if (!NOT_A_DEP.test(match[1])) types.add(match[1]);
  }
  return types;
}

function auditFile(file) {
  const { project, code, name } = file;
  const inApplication = project === "Application";
  const inDomain = project === "Domain";
  const inInfrastructure = project === "Infrastructure";
  const inApi = project === "Api";

  // (i) arquivos longos
  const flaggedAsEndpoints = project === "Api" && /Endpoints\.cs$/.test(name) && file.lines > MAX_ENDPOINTS_LINES;
  if (file.lines > MAX_FILE_LINES && !flaggedAsEndpoints) {
    add(
      file,
      "file-too-long",
      file.lines > 600 ? "high" : "medium",
      1,
      `${file.lines} linhas (limite ${MAX_FILE_LINES}); dividir por fluxo/responsabilidade`,
    );
  }

  // (a) Service gordo
  if (inApplication && /Service\.cs$/.test(name)) {
    const methodPattern =
      /^[ \t]*public\s+(?:(?:static|async|virtual|override|sealed|abstract|new)\s+)*([\w<>\[\]?,.\s]+?)\s+(\w+)\s*(?:<[^>\n]*>)?\s*\(/gm;
    const methods = matchesOf(code, methodPattern).filter(
      (m) => !/\b(class|record|struct|interface|enum|delegate)\b/.test(m[1]),
    );
    if (methods.length > MAX_SERVICE_METHODS) {
      add(
        file,
        "service-too-many-methods",
        methods.length > 10 ? "high" : "medium",
        lineAt(code, methods[0].index),
        `${methods.length} métodos públicos (limite ${MAX_SERVICE_METHODS}): um service agregando fluxos; dividir em um use case por fluxo`,
        { occurrences: methods.length },
      );
    }
  }

  // (b) Contracts misturando DTOs, ports e exceções
  if (inApplication && /Contracts?\.cs$/.test(name)) {
    const ports = matchesOf(code, /\binterface\s+(I\w+)/g).map((m) => m[1]);
    const records = matchesOf(code, /\brecord\s+(?:struct\s+)?\w+/g);
    const exceptions = matchesOf(code, /\bclass\s+(\w+Exception)\b/g).map((m) => m[1]);
    if (ports.length > 0 && records.length > 0) {
      const parts = [`${records.length} record(s)`, `${ports.length} port(s) (${ports.join(", ")})`];
      if (exceptions.length > 0) parts.push(`${exceptions.length} exceção(ões) (${exceptions.join(", ")})`);
      add(
        file,
        "contracts-mixed",
        "high",
        lineAt(code, code.search(/\binterface\s+I\w+/)),
        `Contracts mistura DTOs e ports: ${parts.join(" + ")}; separar em Ports/, Errors/ e tipos <Fluxo>Command/Result ao lado de cada use case`,
      );
    }
  }

  // (c) e (d) Endpoints.cs
  if (inApi && /Endpoints\.cs$/.test(name)) {
    if (file.lines > MAX_ENDPOINTS_LINES) {
      add(
        file,
        "endpoints-too-long",
        file.lines > 300 ? "high" : "medium",
        1,
        `${file.lines} linhas (limite ~${MAX_ENDPOINTS_LINES}); Endpoints deve só mapear rotas, com um handler por endpoint em Handlers/`,
      );
    }

    const unauthorized = matchesOf(code, /\bResults\.Unauthorized\s*\(/g);
    const helperCalls = matchesOf(code, /\bTryGet\w*Id\s*\(/g);
    if (unauthorized.length >= 3 || helperCalls.length >= 3) {
      add(
        file,
        "endpoint-repeated-auth-check",
        "medium",
        lineAt(code, (unauthorized[0] ?? helperCalls[0]).index),
        `extração do usuário repetida (${helperCalls.length} chamada(s) a TryGet*Id, ${unauthorized.length} Results.Unauthorized); usar uma abstração única (ICurrentUser/extensão do ClaimsPrincipal)`,
        { occurrences: Math.max(unauthorized.length, helperCalls.length) },
      );
    }

    const catches = matchesOf(code, /\bcatch\s*\(\s*([\w.]+)?/g);
    if (catches.length > 0) {
      const types = [...new Set(catches.map((m) => m[1] ?? "?"))];
      const problems = matchesOf(code, /\bResults\.Problem\s*\(/g).length;
      add(
        file,
        "endpoint-inline-error-mapping",
        "medium",
        lineAt(code, catches[0].index),
        `${catches.length} catch inline (${types.join(", ")}) e ${problems} Results.Problem: exceção→ProblemDetails deve ter um mapeamento único e centralizado`,
        { occurrences: catches.length },
      );
    }

    // (d) endpoint injetando mais de um service/use case, por método ou lambda
    const signaturePattern = /\(((?:[^()]|\([^()]*\))*)\)\s*(?:=>|\{)/g;
    for (const match of matchesOf(code, signaturePattern)) {
      const types = dependencyTypes(match[1]);
      if (types.size > 1) {
        add(
          file,
          "endpoint-multiple-dependencies",
          "high",
          lineAt(code, match.index),
          `endpoint injeta ${types.size} serviços/use cases (${[...types].join(", ")}); o handler deve injetar exatamente um use case`,
        );
      }
    }
  }

  // Handlers/<Fluxo>Handler.cs: 1:1 com um use case
  if (inApi && file.sub[0] === "Handlers" && /Handler\.cs$/.test(name)) {
    const types = dependencyTypes(code);
    const useCases = [...types].filter((type) => type.endsWith("UseCase"));
    const others = [...types].filter((type) => !type.endsWith("UseCase"));
    if (useCases.length > 1) {
      add(
        file,
        "handler-multiple-usecases",
        "high",
        1,
        `handler injeta ${useCases.length} use cases (${useCases.join(", ")}); 1:1 exige exatamente um`,
      );
    }
    if (useCases.length === 0) {
      add(file, "handler-without-usecase", "medium", 1, "handler sem use case injetado; verificar se há regra no handler");
    }
    if (others.length > 0 || /\bReelixDbContext\b/.test(code)) {
      add(
        file,
        "handler-injects-non-usecase",
        "medium",
        1,
        `handler depende de ${[...others, ...(/\bReelixDbContext\b/.test(code) ? ["ReelixDbContext"] : [])].join(", ")}; só use case é permitido`,
      );
    }
  }

  // Use case: um fluxo, um ExecuteAsync
  if (inApplication && file.sub.includes("UseCases") && /UseCase\.cs$/.test(name)) {
    const executes = matchesOf(code, /\bTask(?:<[^>]*>)?\s+ExecuteAsync\s*\(/g);
    if (executes.length !== 1) {
      add(
        file,
        "usecase-execute-count",
        "high",
        executes.length > 0 ? lineAt(code, executes[0].index) : 1,
        `${executes.length} método(s) ExecuteAsync; um use case é um fluxo com exatamente um ExecuteAsync`,
      );
    }
  }

  // (e) relógio global
  if (inDomain || inApplication || inInfrastructure) {
    const clock = matchesOf(code, /\b(?:DateTimeOffset|DateTime)\.(?:UtcNow|Now)\b/g);
    if (clock.length > 0) {
      add(
        file,
        "direct-clock-access",
        "medium",
        lineAt(code, clock[0].index),
        `${clock.length} uso(s) de DateTime/DateTimeOffset.UtcNow|Now; injetar TimeProvider na Application e passar o instante ao Domain`,
        { occurrences: clock.length },
      );
    }
  }

  // (f) Api importando tipos de Domain
  if (inApi) {
    for (const match of matchesOf(code, /\bReelix\.Domain\b[\w.]*/g)) {
      add(
        file,
        "api-imports-domain",
        "medium",
        lineAt(code, match.index),
        `Api referencia \`${match[0]}\`; request/response DTOs ficam na Api, nunca tipos de Domain`,
      );
    }
  }

  // Direção de camadas por namespace
  const forbiddenNamespaces = {
    Domain: ["Application", "Infrastructure", "Api"],
    Application: ["Infrastructure", "Api"],
    Infrastructure: ["Api"],
    Api: ["Infrastructure"],
  }[project];
  if (forbiddenNamespaces) {
    const compositionRoot = inApi && (name === "Program.cs" || /(DependencyInjection|Module)\.cs$/.test(name));
    for (const match of matchesOf(code, /^[ \t]*(?:global\s+)?using\s+(?:static\s+)?Reelix\.(\w+)/gm)) {
      if (!forbiddenNamespaces.includes(match[1])) continue;
      if (inApi && compositionRoot) continue;
      add(
        file,
        "layer-direction",
        "high",
        lineAt(code, match.index),
        `${project} não pode depender de Reelix.${match[1]}${inApi ? " fora do composition root (Program.cs/DI)" : ""}`,
      );
    }
  }

  // (g) pureza de Application/Domain
  if (inDomain || inApplication) {
    for (const match of matchesOf(code, /^[ \t]*(?:global\s+)?using\s+(Microsoft\.EntityFrameworkCore|Microsoft\.AspNetCore)[\w.]*/gm)) {
      add(
        file,
        "layer-purity-framework",
        "high",
        lineAt(code, match.index),
        `${project} importa \`${match[1]}\`; persistência/HTTP pertencem à Infrastructure/Api`,
      );
    }
  }
  if (inApplication) {
    for (const match of matchesOf(code, /\b(HttpStatusCode|HttpRequestException)\b/g)) {
      add(
        file,
        "application-http-leak",
        "high",
        lineAt(code, match.index),
        `Application conhece \`${match[1]}\`; o adapter deve traduzir erro de HTTP para erro de aplicação`,
      );
    }
  }

  // (h) regra de negócio em repositório (heurística)
  if (inInfrastructure && /Repository\.cs$/.test(name)) {
    const hits = matchesOf(code, /\b(?:Earn|Revoke)\s*\(|\bswitch\b/g);
    if (hits.length > 0) {
      const words = [...new Set(hits.map((m) => m[0].replace(/\s*\($/, "(").trim()))];
      add(
        file,
        "repository-business-rule-suspect",
        "medium",
        lineAt(code, hits[0].index),
        `repositório com ${words.join(", ")} — ${hits.length} ocorrência(s); suspeita de regra de negócio/decisão por tipo na Infrastructure — confirmar por leitura`,
        { confidence: "suspeita", occurrences: hits.length },
      );
    }
  }
}

// ---------------------------------------------------------------------------
// Regras por módulo / entre arquivos
// ---------------------------------------------------------------------------

function auditModules(files) {
  const byModule = new Map();
  for (const file of files) {
    if (!file.project || file.project === "UnitTests") continue;
    if (/^\(/.test(file.module)) continue; // (raiz), (compartilhado)
    if (!byModule.has(file.module)) byModule.set(file.module, []);
    byModule.get(file.module).push(file);
  }

  const stats = new Map();

  for (const [moduleName, moduleFiles] of byModule) {
    const app = moduleFiles.filter((f) => f.project === "Application");
    const api = moduleFiles.filter((f) => f.project === "Api");
    const useCaseFiles = app.filter((f) => f.sub.includes("UseCases") && /UseCase\.cs$/.test(f.name));
    const handlerFiles = api.filter((f) => f.sub[0] === "Handlers" && /Handler\.cs$/.test(f.name));
    const endpointFiles = api.filter((f) => /Endpoints\.cs$/.test(f.name));
    const routes = endpointFiles.reduce(
      (total, f) => total + matchesOf(f.code, /\.Map(?:Get|Post|Put|Patch|Delete)\s*\(/g).length,
      0,
    );
    const services = app.filter((f) => /Service\.cs$/.test(f.name));

    stats.set(moduleName, {
      services: services.length,
      useCases: useCaseFiles.length,
      endpoints: routes,
      handlers: handlerFiles.length,
    });

    // (j) módulo da Application sem UseCases/
    if (app.length > 0 && !app.some((f) => f.sub.includes("UseCases"))) {
      const anchor = app.find((f) => /Service\.cs$/.test(f.name)) ?? app[0];
      add(
        anchor,
        "module-without-usecases-folder",
        "high",
        1,
        `módulo ${moduleName} sem pasta Application/${moduleName}/UseCases/ (${app.length} arquivo(s), ${services.length} service(s)); alvo: um use case por fluxo`,
      );
    }

    // Api sem Handlers/
    if (endpointFiles.length > 0 && routes > 0 && handlerFiles.length === 0) {
      add(
        endpointFiles[0],
        "module-without-handlers-folder",
        "medium",
        1,
        `módulo ${moduleName} mapeia ${routes} rota(s) sem pasta Api/${moduleName}/Handlers/; alvo: um handler por endpoint`,
      );
    }

    // Handlers ↔ rotas
    if (endpointFiles.length > 0 && handlerFiles.length > 0 && handlerFiles.length !== routes) {
      add(
        endpointFiles[0],
        "handler-route-count-mismatch",
        "medium",
        1,
        `${routes} rota(s) mapeada(s) e ${handlerFiles.length} handler(s); 1:1 exige um handler por rota`,
      );
    }

    // Use case ↔ handler 1:1
    for (const useCaseFile of useCaseFiles) {
      const className = (/\bclass\s+(\w+UseCase)\b/.exec(useCaseFile.code) ?? [])[1];
      if (!className) continue;
      const consumers = handlerFiles.filter((h) => new RegExp(`\\b${className}\\b`).test(h.code));
      if (consumers.length > 1) {
        add(
          useCaseFile,
          "usecase-shared-by-handlers",
          "high",
          1,
          `${className} é usado por ${consumers.length} handlers (${consumers.map((c) => c.name).join(", ")}); 1:1 exige um handler por use case`,
        );
      } else if (consumers.length === 0 && handlerFiles.length > 0) {
        add(useCaseFile, "usecase-without-handler", "low", 1, `${className} não é usado por nenhum handler de Api/${moduleName}/Handlers/ (órfão ou consumido por worker?)`);
      }
    }
  }

  // Helper de extração do usuário duplicado entre Endpoints
  const helperDefinitions = files
    .filter((f) => f.project === "Api")
    .map((f) => ({ file: f, match: matchesOf(f.code, USER_ID_HELPER)[0] }))
    .filter((entry) => entry.match);
  if (helperDefinitions.length > 1) {
    for (const { file, match } of helperDefinitions) {
      add(
        file,
        "duplicated-user-id-helper",
        "medium",
        lineAt(file.code, match.index),
        `TryGet*Id duplicado em ${helperDefinitions.length} arquivos da Api; unificar em ICurrentUser/extensão única`,
      );
    }
  }

  return stats;
}

// ---------------------------------------------------------------------------
// Execução e saída
// ---------------------------------------------------------------------------

const RULE_META = {
  "service-too-many-methods": "(a)",
  "contracts-mixed": "(b)",
  "endpoints-too-long": "(c)",
  "endpoint-repeated-auth-check": "(c)",
  "duplicated-user-id-helper": "(c)",
  "endpoint-inline-error-mapping": "(c)",
  "endpoint-multiple-dependencies": "(d)",
  "direct-clock-access": "(e)",
  "api-imports-domain": "(f)",
  "layer-purity-framework": "(g)",
  "application-http-leak": "(g)",
  "repository-business-rule-suspect": "(h)",
  "file-too-long": "(i)",
  "module-without-usecases-folder": "(j)",
  "layer-direction": "extra",
  "module-without-handlers-folder": "extra",
  "handler-route-count-mismatch": "extra",
  "handler-multiple-usecases": "extra",
  "handler-without-usecase": "extra",
  "handler-injects-non-usecase": "extra",
  "usecase-execute-count": "extra",
  "usecase-shared-by-handlers": "extra",
  "usecase-without-handler": "extra",
};

function main() {
  if (!isDirectory(scope)) {
    console.error(`Escopo não encontrado: ${scope}`);
    process.exitCode = 2;
    return;
  }

  const files = loadFiles();
  for (const file of files) auditFile(file);
  const stats = auditModules(files);

  const visible = findings
    .filter((finding) => !moduleFilter || finding.module === moduleFilter)
    .sort(
      (a, b) =>
        SEVERITY_ORDER[a.severity] - SEVERITY_ORDER[b.severity] ||
        a.module.localeCompare(b.module) ||
        a.file.localeCompare(b.file) ||
        a.line - b.line,
    );

  const byRule = {};
  const bySeverity = { high: 0, medium: 0, low: 0 };
  for (const finding of visible) {
    const entry = (byRule[finding.rule] ??= { id: RULE_META[finding.rule] ?? "extra", findings: 0, occurrences: 0 });
    entry.findings += 1;
    entry.occurrences += finding.occurrences ?? 1;
    bySeverity[finding.severity] += 1;
  }

  const moduleNames = new Set([...stats.keys(), ...visible.map((f) => f.module)]);
  const modules = [...moduleNames]
    .filter((name) => !moduleFilter || name === moduleFilter)
    .sort()
    .map((name) => {
      const own = visible.filter((f) => f.module === name);
      return {
        name,
        ...(stats.get(name) ?? { services: 0, useCases: 0, endpoints: 0, handlers: 0 }),
        findings: own.length,
        high: own.filter((f) => f.severity === "high").length,
        medium: own.filter((f) => f.severity === "medium").length,
        low: own.filter((f) => f.severity === "low").length,
        suspects: own.filter((f) => f.confidence === "suspeita").length,
      };
    });

  const result = {
    scope: toPosix(relative(root, scope)) || ".",
    generatedAt: new Date().toISOString(),
    limits: {
      maxServiceMethods: MAX_SERVICE_METHODS,
      maxEndpointsLines: MAX_ENDPOINTS_LINES,
      maxFileLines: MAX_FILE_LINES,
    },
    summary: {
      filesScanned: files.length,
      findings: visible.length,
      bySeverity,
      byRule,
    },
    modules,
    findings: visible,
  };

  if (jsonOutput) console.log(JSON.stringify(result, null, 2));
  else printMarkdown(result);

  if (strict) {
    const blocking = visible.filter((finding) => !allowedRules.has(finding.rule));
    if (blocking.length > 0) process.exitCode = 1;
  }
}

function escapeCell(text) {
  return String(text).replace(/\|/g, "\\|");
}

function printMarkdown(result) {
  const out = [];
  out.push("# Auditoria de slices (.NET)", "");
  out.push(`- Escopo: \`${result.scope}\``);
  out.push(`- Gerado em: ${result.generatedAt}`);
  out.push(
    `- Arquivos: ${result.summary.filesScanned} · achados: ${result.summary.findings} (high ${result.summary.bySeverity.high} · medium ${result.summary.bySeverity.medium} · low ${result.summary.bySeverity.low})`,
  );
  out.push(
    `- Limites: service > ${result.limits.maxServiceMethods} métodos · Endpoints > ${result.limits.maxEndpointsLines} linhas · arquivo > ${result.limits.maxFileLines} linhas`,
  );
  out.push("", "## Achados por regra", "");
  out.push("| Regra | Id | Achados | Ocorrências |", "| --- | --- | --- | --- |");
  for (const [rule, entry] of Object.entries(result.summary.byRule).sort((a, b) => b[1].findings - a[1].findings)) {
    out.push(`| \`${rule}\` | ${entry.id} | ${entry.findings} | ${entry.occurrences} |`);
  }

  out.push("", "## Por módulo", "");
  out.push(
    "| Módulo | Services | Use cases | Endpoints | Handlers | high | medium | low | Situação |",
    "| --- | --- | --- | --- | --- | --- | --- | --- | --- |",
  );
  for (const mod of result.modules) {
    const status =
      mod.findings === 0 ? "conforme" : mod.suspects === mod.findings ? "só suspeitas" : "não conforme";
    out.push(
      `| ${mod.name} | ${mod.services} | ${mod.useCases} | ${mod.endpoints} | ${mod.handlers} | ${mod.high} | ${mod.medium} | ${mod.low} | ${status} |`,
    );
  }

  out.push("", "## Achados", "");
  for (const mod of result.modules.filter((m) => m.findings > 0)) {
    out.push(`### ${mod.name}`, "");
    out.push("| Sev. | Regra | Local | Mensagem |", "| --- | --- | --- | --- |");
    for (const finding of result.findings.filter((f) => f.module === mod.name)) {
      const tag = finding.confidence === "suspeita" ? " _(suspeita)_" : "";
      out.push(
        `| ${finding.severity} | \`${finding.rule}\` | \`${finding.file}:${finding.line}\` | ${escapeCell(finding.message)}${tag} |`,
      );
    }
    out.push("");
  }

  out.push("> Relatório mecânico. Leia o código e confirme a semântica antes de planejar; suspeitas exigem leitura.");
  console.log(out.join("\n"));
}

main();
