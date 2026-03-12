# Namoz Vaqtlari CLI 🌙

A fast and lightweight Command Line Interface (CLI) tool to get prayer times in Uzbekistan, powered by Islom.uz data. Built with .NET 8 and Puppeteer.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![AUR version](https://img.shields.io/aur/version/namoz-vaqtlari.svg)](https://aur.archlinux.org/packages/namoz-vaqtlari)

## Features

- **Accurate Data:** Fetches real-time prayer times directly from Islom.uz.
- **Region Support:** Supports all major regions and cities across Uzbekistan.
- **Smart UI:** Beautiful terminal tables and countdowns using [Spectre.Console](https://spectreconsole.net/).
- **Lightweight:** Efficiently uses the system's Chromium browser.
- **Persistence:** Automatically saves your selected region for future use.

## Installation

### Arch Linux (AUR)
The easiest way to install on Arch Linux is via an AUR helper like `yay` or `paru`:

```bash
yay -S namoz-vaqtlari
```

## Manual Installation (from Source)

Ensure you have the .NET 8 SDK installed:

1. Clone the repository:
```bash
git clone [https://github.com/OneWay2Go/NamozVaqtlariCLI.git](https://github.com/OneWay2Go/NamozVaqtlariCLI.git)
cd NamozVaqtlariCLI
```

2. Build and Publish:
```bash
dotnet publish -c Release -r linux-x64 --self-contained false
```

## Usage

Simply run the command in your terminal:
```bash
namoz
```

## Commands & Options

1. Upon first launch, the tool will prompt you to select your Region.

2. To change your region later, select the "Change Region" option from the interactive menu.

3. The configuration is stored at ~/.config/namoz-vaqtlari/namoz-config.json.

## Requirements

1. Runtime: .NET Runtime 8.0

2. Browser: Chromium (required for web scraping)

## Screenshots

<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/5fbb153c-8c70-499a-b4a8-477e84667934" />

## Contributing

Contributions are welcome! Feel free to open issues or submit pull requests to improve the tool.

## License

This project is licensed under the MIT License. See the LICENSE file for details.

---

***Data provided by Islom.uz***
