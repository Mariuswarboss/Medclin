const pptxgen = require("pptxgenjs");
const path = require("path");
const { execSync } = require("child_process");

const pptx = new pptxgen();
pptx.layout = "LAYOUT_WIDE";
pptx.author = "MediClin";
pptx.subject = "Prezentare proiect MediClin";
pptx.title = "MediClin - Sistem de management clinic";
pptx.company = "MediClin";
pptx.lang = "ro-RO";
pptx.theme = {
  headFontFace: "Aptos Display",
  bodyFontFace: "Aptos",
  lang: "ro-RO"
};
pptx.defineLayout({ name: "CUSTOM_WIDE", width: 13.333, height: 7.5 });
pptx.layout = "CUSTOM_WIDE";
pptx.defineSlideMaster({
  title: "MEDICLIN",
  background: { color: "F6FAF8" },
  objects: [
    { line: { x: 0.48, y: 7.08, w: 12.38, h: 0, line: { color: "D9E7E2", width: 1 } } },
    { text: { text: "MediClin · Care. Connect. Cure.", options: { x: 0.58, y: 7.14, w: 3.1, h: 0.18, fontFace: "Aptos", fontSize: 6.5, color: "6B7C86" } } },
    { text: { text: "Proiect aplicație desktop medicală", options: { x: 9.75, y: 7.14, w: 3.0, h: 0.18, align: "right", fontFace: "Aptos", fontSize: 6.5, color: "6B7C86" } } }
  ]
});

const OUT = path.join(__dirname, "output", "Prezentare_MediClin.pptx");
const LOGO = path.resolve(__dirname, "../../../..", "Mediclin.UI", "Assets", "logo-mediclin.png");
const repoDir = path.resolve(__dirname, "../../../..");
const C = {
  bg: "F6FAF8",
  ink: "071524",
  muted: "6B7C86",
  green: "0F7B62",
  green2: "1BB58A",
  mint: "DFF3EC",
  blue: "2F80ED",
  yellow: "F4B740",
  red: "D94E4E",
  card: "FFFFFF",
  line: "D9E7E2",
  navy: "08243A"
};

function git(cmd, fallback = "") {
  try {
    return execSync(cmd, { cwd: repoDir, encoding: "utf8" }).trim();
  } catch {
    return fallback;
  }
}

const commits = git("git log --oneline -5")
  .split(/\r?\n/)
  .filter(Boolean);
const remote = git("git remote get-url origin", "https://github.com/Mariuswarboss/Medclin");
const fileCount = git("git ls-files | find /c /v \"\"", "—");

function slide(title, kicker) {
  const s = pptx.addSlide("MEDICLIN");
  s.background = { color: C.bg };
  if (kicker) {
    s.addText(kicker.toUpperCase(), {
      x: 0.62, y: 0.34, w: 8.0, h: 0.22,
      fontFace: "Aptos", fontSize: 8, bold: true, color: C.green,
      charSpace: 1.2, margin: 0
    });
  }
  s.addText(title, {
    x: 0.58, y: 0.66, w: 8.6, h: 0.58,
    fontFace: "Aptos Display", fontSize: 25, bold: true, color: C.ink,
    margin: 0, breakLine: false, fit: "shrink"
  });
  s.addImage({ path: LOGO, x: 11.82, y: 0.32, w: 0.7, h: 0.7 });
  return s;
}

function card(s, x, y, w, h, opts = {}) {
  s.addShape(pptx.ShapeType.roundRect, {
    x, y, w, h, rectRadius: 0.08,
    fill: { color: opts.fill || C.card },
    line: { color: opts.line || C.line, width: opts.lineWidth || 1 }
  });
}

function pill(s, txt, x, y, w, color = C.mint, fg = C.green) {
  s.addShape(pptx.ShapeType.roundRect, {
    x, y, w, h: 0.34, rectRadius: 0.08,
    fill: { color }, line: { color, transparency: 100 }
  });
  s.addText(txt, { x: x + 0.12, y: y + 0.085, w: w - 0.24, h: 0.12, align: "center", fontSize: 8.5, bold: true, color: fg, margin: 0 });
}

function body(s, text, x, y, w, h, size = 13, color = C.ink, options = {}) {
  s.addText(text, {
    x, y, w, h, fontFace: "Aptos", fontSize: size, color,
    breakLine: false, fit: "shrink", valign: "mid",
    margin: 0.06, ...options
  });
}

function bulletList(s, items, x, y, w, h, size = 12.5) {
  s.addText(items.map(t => ({ text: t, options: { bullet: { type: "ul" } } })), {
    x, y, w, h, fontFace: "Aptos", fontSize: size, color: C.ink,
    breakLine: false, fit: "shrink", margin: 0.08,
    paraSpaceAfterPt: 8
  });
}

function sectionNumber(s, n) {
  s.addShape(pptx.ShapeType.ellipse, { x: 0.57, y: 6.55, w: 0.34, h: 0.34, fill: { color: C.green }, line: { color: C.green } });
  s.addText(String(n).padStart(2, "0"), { x: 0.57, y: 6.64, w: 0.34, h: 0.08, align: "center", fontSize: 6.5, bold: true, color: "FFFFFF", margin: 0 });
}

function addNotes(s, lines) {
  s.addNotes(lines.join("\n"));
}

// 1. Title
{
  const s = pptx.addSlide("MEDICLIN");
  s.background = { color: "F4FBF8" };
  s.addShape(pptx.ShapeType.rect, { x: 0, y: 0, w: 13.333, h: 7.5, fill: { color: "F4FBF8" }, line: { color: "F4FBF8" } });
  s.addShape(pptx.ShapeType.rect, { x: 0, y: 0, w: 5.1, h: 7.5, fill: { color: C.green }, line: { color: C.green } });
  s.addImage({ path: LOGO, x: 0.8, y: 0.62, w: 1.38, h: 1.38 });
  s.addText("MediClin", { x: 0.82, y: 2.42, w: 4.0, h: 0.46, fontSize: 29, bold: true, color: "FFFFFF", margin: 0 });
  s.addText("Sistem de management clinic desktop", { x: 0.84, y: 3.0, w: 3.9, h: 0.52, fontSize: 16, color: "DFF3EC", fit: "shrink", margin: 0 });
  ["Programări rapide", "Dosar medical digital", "Comunicare medic-pacient"].forEach((t, i) => {
    s.addShape(pptx.ShapeType.roundRect, { x: 0.82, y: 4.0 + i * 0.58, w: 3.65, h: 0.38, rectRadius: 0.08, fill: { color: "2F947B", transparency: 8 }, line: { color: "2F947B" } });
    s.addText(t, { x: 1.02, y: 4.105 + i * 0.58, w: 3.1, h: 0.12, fontSize: 10, bold: true, color: "FFFFFF", margin: 0 });
  });
  s.addText("TEMA:", { x: 6.0, y: 1.38, w: 1.2, h: 0.22, fontSize: 11, bold: true, color: C.green, margin: 0 });
  s.addText("Dezvoltarea unei aplicații pentru managementul unei clinici medicale", {
    x: 6.0, y: 1.72, w: 6.25, h: 1.25, fontSize: 28, bold: true, color: C.ink, fit: "shrink", margin: 0
  });
  s.addShape(pptx.ShapeType.line, { x: 6.02, y: 3.28, w: 5.75, h: 0, line: { color: C.line, width: 1.2 } });
  const info = [
    ["Elev", "Marius Chistrea"],
    ["Grupa", "__________"],
    ["Conducător", "Adina Pîntea"],
    ["Anul", "2026"]
  ];
  info.forEach((r, i) => {
    s.addText(r[0], { x: 6.02, y: 3.72 + i * 0.42, w: 1.25, h: 0.16, fontSize: 9.5, bold: true, color: C.muted, margin: 0 });
    s.addText(r[1], { x: 7.36, y: 3.72 + i * 0.42, w: 3.1, h: 0.16, fontSize: 10.5, color: C.ink, margin: 0 });
  });
  pill(s, "Doctor: Sandu Super", 6.02, 5.9, 1.9);
  pill(s, "Pacient: Marius Chistrea", 8.1, 5.9, 2.25, "EAF2FF", C.blue);
  addNotes(s, [
    "Prezint aplicația MediClin, un sistem desktop pentru managementul activității unei clinici medicale.",
    "Aplicația are două roluri principale, medic și pacient, iar scopul este să centralizeze programările, fișa medicală, rețetele, analizele și comunicarea."
  ]);
}

// 2. Intro
{
  const s = slide("Tema și motivația alegerii", "Introducere");
  sectionNumber(s, 2);
  body(s, "În multe cabinete mici, informațiile sunt împărțite între agende, fișiere, mesaje și documente separate. MediClin propune un flux unic pentru pacient și medic.", 0.65, 1.42, 5.75, 0.92, 16, C.ink, { bold: true });
  const problems = [
    ["Fragmentare", "Programări, rezultate, rețete și mesaje în locuri diferite."],
    ["Timp pierdut", "Medicul caută datele pacientului în loc să se concentreze pe consultație."],
    ["Trasabilitate", "Acțiunile importante trebuie urmărite prin notificări și jurnal."],
    ["Experiență", "Pacientul are nevoie de acces clar la programări, tratamente și analize."]
  ];
  problems.forEach((p, i) => {
    const x = 0.68 + (i % 2) * 6.1;
    const y = 2.72 + Math.floor(i / 2) * 1.45;
    card(s, x, y, 5.65, 1.05);
    s.addText(p[0], { x: x + 0.28, y: y + 0.18, w: 2.2, h: 0.18, fontSize: 13, bold: true, color: C.green, margin: 0 });
    body(s, p[1], x + 0.28, y + 0.48, 5.05, 0.34, 11.5, C.muted);
  });
  addNotes(s, [
    "Am ales tema deoarece o clinică are multe activități conectate: programare, consultație, diagnostic, rețetă, analize, mesaje.",
    "Problema reală este fragmentarea informației. MediClin încearcă să reducă acest haos printr-o platformă unitară."
  ]);
}

// 3. Similar systems
{
  const s = slide("Sisteme similare comparate", "Analiză comparativă");
  sectionNumber(s, 3);
  const headers = ["Sistem", "Punct forte", "Limitare observată", "Ce preia MediClin"];
  const rows = [
    ["DrChrono", "EMR + programări", "Complex pentru clinici mici", "Dosar medical + programări"],
    ["Cliniko", "Booking și pacienți", "Mai puțin orientat pe flux local desktop", "Flux simplu pacient-medic"],
    ["SimplePractice", "Cabinet + comunicare", "Accent pe piața SUA", "Mesaje și notificări"],
    ["MediClin", "Roluri doctor/pacient integrate", "Proiect educațional în dezvoltare", "Control local și adaptare"]
  ];
  const x = 0.62, y = 1.55, w = 12.05;
  card(s, x, y, w, 4.72);
  const col = [2.0, 2.85, 3.25, 3.55];
  let cx = x + 0.2;
  headers.forEach((h, i) => {
    s.addText(h, { x: cx, y: y + 0.28, w: col[i], h: 0.2, fontSize: 10.5, bold: true, color: C.green, margin: 0 });
    cx += col[i];
  });
  rows.forEach((r, ri) => {
    const yy = y + 0.82 + ri * 0.82;
    s.addShape(pptx.ShapeType.line, { x: x + 0.18, y: yy - 0.18, w: w - 0.36, h: 0, line: { color: C.line, width: 0.7 } });
    cx = x + 0.2;
    r.forEach((txt, ci) => {
      s.addText(txt, { x: cx, y: yy, w: col[ci] - 0.15, h: 0.28, fontSize: ci === 0 ? 11 : 9.8, bold: ci === 0, color: ci === 0 && txt === "MediClin" ? C.green : C.ink, fit: "shrink", margin: 0 });
      cx += col[ci];
    });
  });
  body(s, "Concluzie: soluțiile existente confirmă nevoia sistemului, iar MediClin se diferențiază printr-un flux educațional complet, local, ușor de demonstrat și adaptat.", 0.82, 6.42, 11.5, 0.28, 12, C.muted);
  addNotes(s, [
    "Am analizat soluții existente pentru cabinete și clinici.",
    "MediClin nu încearcă să copieze un sistem comercial mare, ci să preia ideile esențiale: programări, dosar medical, comunicare și raportare, într-o aplicație clară pentru două roluri."
  ]);
}

// 4. Goals
{
  const s = slide("Scopul, obiectivele și cerințele sistemului", "Specificație");
  sectionNumber(s, 4);
  card(s, 0.65, 1.36, 3.65, 4.85, { fill: C.green, line: C.green });
  s.addText("Scop", { x: 0.98, y: 1.76, w: 2.1, h: 0.25, fontSize: 19, bold: true, color: "FFFFFF", margin: 0 });
  body(s, "Realizarea unei platforme desktop care ajută medicul și pacientul să gestioneze consultațiile, documentele medicale și comunicarea într-un singur loc.", 0.98, 2.25, 2.8, 1.55, 15, "FFFFFF", { fit: "shrink" });
  pill(s, "clean · modern · secure", 0.98, 5.35, 2.5, "DFF3EC", C.green);
  card(s, 4.65, 1.36, 3.7, 4.85);
  s.addText("Obiective", { x: 4.98, y: 1.74, w: 2.5, h: 0.24, fontSize: 18, bold: true, color: C.ink, margin: 0 });
  bulletList(s, [
    "autentificare pe roluri",
    "dashboard medic și pacient",
    "programări cu status",
    "fișă medicală cronologică",
    "rețete, analize, mesaje",
    "rapoarte financiare și export"
  ], 4.98, 2.22, 2.8, 2.85, 11.2);
  card(s, 8.72, 1.36, 3.96, 4.85);
  s.addText("Cerințe", { x: 9.05, y: 1.74, w: 2.5, h: 0.24, fontSize: 18, bold: true, color: C.ink, margin: 0 });
  bulletList(s, [
    "interfață responsivă WPF",
    "validare date medicale",
    "notificări și jurnal audit",
    "dark mode salvat local",
    "export PDF/CSV",
    "separare UI / business / data"
  ], 9.05, 2.22, 3.05, 2.85, 11.2);
  addNotes(s, [
    "Scopul proiectului este o aplicație practică, nu doar o interfață.",
    "Obiectivele urmăresc cele mai importante procese din clinică: programarea, consultația, documentarea și comunicarea.",
    "Cerințele includ stabilitate, validare, roluri și trasabilitate."
  ]);
}

// 5. Use case
{
  const s = slide("Diagrama Use-Case", "Comportamentul aplicației");
  sectionNumber(s, 5);
  s.addText("Pacient", { x: 0.72, y: 1.65, w: 1.0, h: 0.2, fontSize: 13, bold: true, color: C.green, margin: 0 });
  s.addShape(pptx.ShapeType.ellipse, { x: 0.92, y: 2.02, w: 0.38, h: 0.38, fill: { color: C.green }, line: { color: C.green } });
  s.addShape(pptx.ShapeType.line, { x: 1.11, y: 2.4, w: 0, h: 0.85, line: { color: C.green, width: 2 } });
  s.addShape(pptx.ShapeType.line, { x: 0.72, y: 2.68, w: 0.78, h: 0, line: { color: C.green, width: 2 } });
  s.addShape(pptx.ShapeType.line, { x: 1.11, y: 3.25, w: -0.42, h: 0.7, line: { color: C.green, width: 2 } });
  s.addShape(pptx.ShapeType.line, { x: 1.11, y: 3.25, w: 0.42, h: 0.7, line: { color: C.green, width: 2 } });
  s.addText("Medic", { x: 11.62, y: 1.65, w: 1.0, h: 0.2, fontSize: 13, bold: true, color: C.blue, margin: 0 });
  s.addShape(pptx.ShapeType.ellipse, { x: 11.78, y: 2.02, w: 0.38, h: 0.38, fill: { color: C.blue }, line: { color: C.blue } });
  s.addShape(pptx.ShapeType.line, { x: 11.97, y: 2.4, w: 0, h: 0.85, line: { color: C.blue, width: 2 } });
  s.addShape(pptx.ShapeType.line, { x: 11.58, y: 2.68, w: 0.78, h: 0, line: { color: C.blue, width: 2 } });
  s.addShape(pptx.ShapeType.line, { x: 11.97, y: 3.25, w: -0.42, h: 0.7, line: { color: C.blue, width: 2 } });
  s.addShape(pptx.ShapeType.line, { x: 11.97, y: 3.25, w: 0.42, h: 0.7, line: { color: C.blue, width: 2 } });
  const cases = [
    ["Autentificare", 4.9, 1.48],
    ["Vizualizare dashboard", 4.65, 2.15],
    ["Gestionare programări", 4.55, 2.82],
    ["Fișă medicală / consultații", 4.25, 3.49],
    ["Rețete și analize", 4.72, 4.16],
    ["Mesagerie și notificări", 4.45, 4.83],
    ["Export rapoarte PDF/CSV", 4.45, 5.5]
  ];
  cases.forEach(([txt, x, y], i) => {
    s.addShape(pptx.ShapeType.ellipse, { x, y, w: 3.1, h: 0.43, fill: { color: i % 2 ? "EAF2FF" : C.mint }, line: { color: i % 2 ? "BFD8FF" : "BFE5D8" } });
    s.addText(txt, { x: x + 0.15, y: y + 0.135, w: 2.8, h: 0.12, align: "center", fontSize: 9.8, bold: true, color: C.ink, margin: 0 });
    s.addShape(pptx.ShapeType.line, { x: 1.52, y: 3.08, w: x - 1.52, h: y - 3.08 + 0.21, line: { color: "B5C9C2", width: 0.7, transparency: 25 } });
    s.addShape(pptx.ShapeType.line, { x: x + 3.1, y: y + 0.21, w: 11.55 - (x + 3.1), h: 3.08 - (y + 0.21), line: { color: "B5C9C2", width: 0.7, transparency: 25 } });
  });
  addNotes(s, [
    "Diagrama arată că pacientul și medicul folosesc unele funcții comune, cum ar fi autentificarea, programările, mesajele și notificările.",
    "Medicul are în plus zona de consultații, fișe și rapoarte, iar pacientul are accent pe vizualizare, programare și acces la documente."
  ]);
}

// 6. Technologies
{
  const s = slide("Tehnologiile utilizate", "Stack tehnic");
  sectionNumber(s, 6);
  const tech = [
    ["C# / .NET 8", "limbaj și runtime principal"],
    ["WPF", "interfață desktop pentru Windows"],
    ["MVVM", "separarea logicii UI de date"],
    ["MySQL", "persistență pentru utilizatori, programări, fișe"],
    ["Visual Studio / CLI", "dezvoltare, build și rulare"],
    ["Git + GitHub", "versionare și istoric de lucru"]
  ];
  tech.forEach((t, i) => {
    const x = 0.72 + (i % 3) * 4.15;
    const y = 1.55 + Math.floor(i / 3) * 1.45;
    card(s, x, y, 3.65, 1.08);
    s.addText(t[0], { x: x + 0.25, y: y + 0.22, w: 2.8, h: 0.18, fontSize: 14, bold: true, color: C.green, margin: 0 });
    body(s, t[1], x + 0.25, y + 0.55, 3.1, 0.25, 10.8, C.muted);
  });
  s.addShape(pptx.ShapeType.roundRect, { x: 1.22, y: 5.1, w: 10.9, h: 0.72, rectRadius: 0.08, fill: { color: C.navy }, line: { color: C.navy } });
  s.addText("Arhitectură pe straturi: UI (WPF)  →  Business Services  →  Repositories  →  MySQL", { x: 1.55, y: 5.34, w: 10.1, h: 0.18, fontSize: 15, bold: true, color: "FFFFFF", margin: 0 });
  body(s, "Această separare face codul mai ușor de extins: de exemplu, exportul PDF și notificările au fost adăugate fără rescrierea fluxurilor principale.", 1.25, 6.12, 10.8, 0.36, 12, C.muted);
  addNotes(s, [
    "Tehnologiile sunt alese pentru o aplicație desktop stabilă pe Windows.",
    ".NET 8 și WPF oferă baza aplicației, iar MySQL păstrează datele medicale și administrative.",
    "Modelul pe straturi ajută la mentenanță și la extindere."
  ]);
}

// 7. GitHub
{
  const s = slide("Activitatea de pe GitHub", "Versionare");
  sectionNumber(s, 7);
  card(s, 0.72, 1.46, 7.2, 4.95, { fill: "0D1117", line: "30363D" });
  s.addText("github.com/Mariuswarboss/Medclin", { x: 1.05, y: 1.82, w: 5.8, h: 0.18, fontSize: 13, bold: true, color: "FFFFFF", margin: 0 });
  s.addText(remote, { x: 1.05, y: 2.14, w: 6.2, h: 0.18, fontSize: 8.7, color: "8B949E", margin: 0 });
  commits.forEach((c, i) => {
    const y = 2.7 + i * 0.52;
    s.addShape(pptx.ShapeType.ellipse, { x: 1.06, y, w: 0.12, h: 0.12, fill: { color: C.green2 }, line: { color: C.green2 } });
    s.addText(c, { x: 1.34, y: y - 0.03, w: 5.9, h: 0.16, fontSize: 9.2, color: "E6EDF3", fit: "shrink", margin: 0 });
  });
  card(s, 8.35, 1.46, 3.95, 1.15);
  body(s, "Captura reală", 8.68, 1.74, 2.1, 0.18, 14, C.ink, { bold: true });
  body(s, "Înlocuiește acest panou cu screenshot-ul din GitHub dacă vrei dovada vizuală exactă.", 8.68, 2.03, 3.1, 0.28, 10.5, C.muted);
  card(s, 8.35, 2.9, 3.95, 1.15);
  body(s, "Repository", 8.68, 3.18, 1.6, 0.18, 14, C.ink, { bold: true });
  body(s, `${fileCount} fișiere urmărite în Git`, 8.68, 3.48, 2.9, 0.18, 11, C.muted);
  card(s, 8.35, 4.35, 3.95, 1.15);
  body(s, "Flux de lucru", 8.68, 4.63, 1.8, 0.18, 14, C.ink, { bold: true });
  body(s, "commit-uri incrementale, testare build, rulare locală", 8.68, 4.92, 2.9, 0.22, 11, C.muted);
  addNotes(s, [
    "Slide-ul arată activitatea de versionare: repository-ul și ultimele commit-uri.",
    "Pentru prezentarea finală, pot deschide GitHub și înlocui zona din stânga cu o captură reală a paginii repository-ului."
  ]);
}

// 8. Demo
{
  const s = slide("Demo aplicație", "Flux prezentat în 90 secunde");
  sectionNumber(s, 8);
  card(s, 0.78, 1.45, 7.35, 4.85, { fill: "FDFEFE" });
  s.addShape(pptx.ShapeType.rect, { x: 1.18, y: 1.92, w: 6.55, h: 3.65, fill: { color: "EBF3F0" }, line: { color: C.line } });
  s.addShape(pptx.ShapeType.ellipse, { x: 3.72, y: 3.0, w: 1.0, h: 1.0, fill: { color: C.green }, line: { color: C.green } });
  s.addShape(pptx.ShapeType.triangle, { x: 4.08, y: 3.28, w: 0.34, h: 0.42, rotate: 90, fill: { color: "FFFFFF" }, line: { color: "FFFFFF" } });
  s.addText("Inserează aici video screen/demo live", { x: 1.55, y: 5.78, w: 5.8, h: 0.18, fontSize: 11.5, bold: true, align: "center", color: C.muted, margin: 0 });
  const steps = [
    ["1", "Login", "doctor/pacient"],
    ["2", "Dashboard", "indicatori și navigare"],
    ["3", "Programare", "filtru, status, anulare"],
    ["4", "Fișă/analize", "timeline + export PDF"],
    ["5", "Notificări", "badge, citire, refresh"]
  ];
  steps.forEach((st, i) => {
    const y = 1.55 + i * 0.9;
    s.addShape(pptx.ShapeType.ellipse, { x: 8.82, y: y + 0.02, w: 0.36, h: 0.36, fill: { color: C.green }, line: { color: C.green } });
    s.addText(st[0], { x: 8.82, y: y + 0.12, w: 0.36, h: 0.08, align: "center", fontSize: 7.5, bold: true, color: "FFFFFF", margin: 0 });
    body(s, st[1], 9.35, y, 2.2, 0.17, 13, C.ink, { bold: true });
    body(s, st[2], 9.35, y + 0.27, 2.7, 0.18, 10.5, C.muted);
  });
  body(s, "Pentru 7 minute total: păstrează demo-ul la 1 minut și jumătate, fără detalii de cod.", 8.82, 6.18, 3.5, 0.3, 11, C.red, { bold: true });
  addNotes(s, [
    "În demo arăt rapid autentificarea, trecerea între roluri, pagina de programări, exportul de PDF și notificările.",
    "Important este să demonstrez fluxul real, nu fiecare detaliu tehnic."
  ]);
}

// 9. Implemented results
{
  const s = slide("Funcționalități implementate", "Rezultatul proiectului");
  sectionNumber(s, 9);
  const groups = [
    ["Doctor", ["dashboard cu indicatori", "pacienți și programări", "fișe medicale", "rapoarte PDF/CSV"]],
    ["Pacient", ["programări și filtre", "rețete active", "analize + PDF", "setări și dark mode"]],
    ["Platformă", ["mesagerie", "notificări", "audit logs", "logout după inactivitate"]]
  ];
  groups.forEach((g, i) => {
    const x = 0.75 + i * 4.15;
    card(s, x, 1.52, 3.65, 4.5);
    s.addText(g[0], { x: x + 0.3, y: 1.86, w: 2.4, h: 0.24, fontSize: 20, bold: true, color: i === 0 ? C.blue : i === 1 ? C.green : C.ink, margin: 0 });
    bulletList(s, g[1], x + 0.34, 2.42, 2.75, 2.35, 12);
  });
  s.addText("Stabilitate: build verificat fără erori după integrarea ultimelor funcții.", { x: 1.0, y: 6.34, w: 10.9, h: 0.18, align: "center", fontSize: 13, bold: true, color: C.green, margin: 0 });
  addNotes(s, [
    "Rezultatul este o aplicație funcțională cu fluxuri pentru ambele roluri.",
    "Am lucrat și la partea de realism: notificări, audit, exporturi, dark mode, validări și separarea logicii."
  ]);
}

// 10. Conclusions
{
  const s = slide("Concluzii", "Închidere");
  sectionNumber(s, 10);
  body(s, "MediClin demonstrează cum un sistem desktop poate organiza activitatea unei clinici într-un mod clar, modern și extensibil.", 0.88, 1.45, 11.3, 0.62, 21, C.ink, { bold: true, align: "center" });
  const points = [
    ["Ce am obținut", "flux complet medic-pacient, date centralizate, exporturi și notificări"],
    ["Ce am învățat", "importanța arhitecturii pe straturi și a experienței utilizatorului"],
    ["Ce poate urma", "calendar avansat, semnătură digitală, rapoarte medicale mai detaliate, backup cloud"]
  ];
  points.forEach((p, i) => {
    const y = 2.65 + i * 1.0;
    card(s, 1.05, y, 11.2, 0.72);
    s.addText(p[0], { x: 1.38, y: y + 0.18, w: 2.2, h: 0.18, fontSize: 13, bold: true, color: C.green, margin: 0 });
    body(s, p[1], 3.78, y + 0.17, 7.8, 0.22, 12.5, C.ink);
  });
  s.addText("Mulțumesc!", { x: 4.85, y: 6.05, w: 3.5, h: 0.3, align: "center", fontSize: 24, bold: true, color: C.green, margin: 0 });
  addNotes(s, [
    "În concluzie, MediClin acoperă un scenariu realist de clinică și poate fi extins în continuare.",
    "Cele mai importante realizări sunt centralizarea datelor, separarea pe roluri și funcțiile practice de export, notificări și raportare."
  ]);
}

pptx.writeFile({ fileName: OUT });
