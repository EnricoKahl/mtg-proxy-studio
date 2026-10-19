# MTG Proxy Studio

**Download Magic: The Gathering card images and create print-ready A4 proxy sheets on Windows.**

[![Build Windows EXE](https://github.com/EnricoKahl/mtg-proxy-studio/actions/workflows/build.yml/badge.svg)](https://github.com/EnricoKahl/mtg-proxy-studio/actions)
[![Latest release](https://img.shields.io/github/v/release/EnricoKahl/mtg-proxy-studio?include_prereleases)](https://github.com/EnricoKahl/mtg-proxy-studio/releases)

[**Download the latest release**](https://github.com/EnricoKahl/mtg-proxy-studio/releases) · [Report an issue](https://github.com/EnricoKahl/mtg-proxy-studio/issues)

> **Beta software.** Please check card images, token matches and printed dimensions before printing an entire deck.

## Features

### Card image downloader

- Import Moxfield-style deck exports with quantities, set codes and collector numbers.
- Download high-resolution card images from [Scryfall](https://scryfall.com/).
- Prefer a selected card language, or select alternate printings and artwork with previews.
- Use exact Moxfield printings where available; optionally filter special frames.
- Download both faces of double-faced cards as separate printable images.
- Save card download metadata to `manifest.csv` and `manifest.json`.

### Tokens and emblems

- Discover associated tokens and emblems through Scryfall card relationships.
- Prefer tokens in the selected language and fall back to English when needed.
- Choose token artwork manually when multiple matches are available.
- Download two copies of each token by default.
- Support double-faced tokens and save token metadata to `token_manifest.csv` and `token_manifest.json`.

> **Note:** Scryfall's token relationships and localized printings are not always complete. Review automatically selected tokens before printing.

### PDF print sheets

- Generate A4 PDFs with a **3 × 3** card grid.
- Print cards at **63 × 88 mm**.
- Center the grid and incomplete rows.
- Adjust spacing and safety margins.
- Add crop marks, card numbering and page numbers.
- Include downloaded cards and tokens from subfolders.
- Create PDFs using PDFsharp.

## Download and run

1. Open [Releases](https://github.com/EnricoKahl/mtg-proxy-studio/releases).
2. Download the Windows x64 ZIP from the latest release.
3. Extract the entire archive.
4. Launch `MTGProxyStudio.exe`.

**Platform:** Windows 10/11, 64-bit. An internet connection is required to download card images. The self-contained build does not require a separate .NET installation.

If no release has been published yet, open [Actions](https://github.com/EnricoKahl/mtg-proxy-studio/actions), select a successful **Build Windows EXE** run and download its artifact.

## Quick start

1. Export your decklist from Moxfield as text.
2. Import or paste the decklist into MTG Proxy Studio.
3. Select the preferred language and card-printing options.
4. Download card images, including tokens and both faces of double-faced cards where applicable.
5. Review the downloaded images and token selections.
6. Generate the A4 PDF sheets.
7. Open the PDF in Adobe Acrobat Reader and print at **Actual size / 100%**. Disable *Fit* and *Shrink oversized pages*.

Example decklist entries:

```text
1 Sol Ring (CMM) 396
1 Etali, Primal Conqueror (MOM) 137
```

## Build from source

The application is written in **C# / .NET 8** for Windows.

### Easy build (Windows)

1. Clone or download this repository.
2. Install the .NET 8 SDK if needed (`DOTNET-SDK-installieren.bat` is included).
3. Run `EXE-erstellen.bat`.
4. Find the generated application under `Fertige_EXE/`.

The first build downloads dependencies, including PDFsharp, through NuGet.

### Command-line build

```powershell
dotnet publish .\MTGProxyStudio\MTGProxyStudio.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

The repository also includes a GitHub Actions workflow to build the Windows application automatically.

## Project status and feedback

This is an early public version. Bug reports, reproducible examples and feature requests are welcome through [GitHub Issues](https://github.com/EnricoKahl/mtg-proxy-studio/issues).

When reporting a problem, please include the affected card name, set code, collector number, selected language and a description of the unexpected result. Avoid uploading copyrighted card-image collections.

## Third-party services and legal notice

MTG Proxy Studio is an **unofficial, fan-made project** and is not affiliated with, endorsed by or sponsored by **Wizards of the Coast** or **Scryfall**.

Magic: The Gathering, its names, symbols and artwork belong to their respective rights holders. Card information and images are retrieved from Scryfall at the user's request; they are not distributed with this repository. Users are responsible for complying with applicable copyright rules and third-party terms, including [Scryfall's API guidelines](https://scryfall.com/docs/api) and [Wizards of the Coast's Fan Content Policy](https://company.wizards.com/en/legal/fancontentpolicy).
