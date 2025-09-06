# Automatic Hydroponics Linkables

Adds new powered, linkable facilities for the “Automatic Hydroponics” mod. Facilities connect to hydroponic bays and modify processing speed and/or yield. Effects are data‑driven and are shown directly in the processor UI and menus.

## 🌟 Features

### Linkable Facilities
- **Fertilizer Injector**
  - Modest speed boost, low power, adjacent placement
  - Up to 2 linked injectors per hydroponic bay
- **Hydroponic AI Optimizer**
  - Stronger speed boost, high power, 2x2 footprint, adjacent placement
  - Max 1 per hydroponic bay
- **Hydroponic Yield Overclocker**
  - Doubles yield while making processes ~1.5× slower
  - Moderate power, adjacent placement, max 1 per bay

### UI/UX
- Process row shows effective duration (accounting for all linkables)
- Process options menu shows effective duration and, when applicable, “×N yield”
- Effects are computed per‑building and update live with links

### Data‑Driven Config
Each facility can define in XML (via DefModExtension):
- `tickFactorPerLink`: additive speed factor; contributes to `(1 + sum)`
- `speedMultPerLink`: multiplicative speed factor (`>1` slower, `<1` faster)
- `yieldMultPerLink`: multiplicative yield factor
- `maxLinks`: per‑facility cap per bay

## 🔬 Research

- **Automatic Hydroponics Enhancements** (after Fabrication + Hydroponics)
  - Unlocks all new facilities

## 📦 Installation

### Steam Workshop (Recommended)
1. Subscribe to this mod and required dependencies
2. Enable them in the Mods menu
3. Start a new game or load an existing save

### Manual Installation
1. Copy this folder into RimWorld `Mods/`
2. Ensure dependencies are installed
3. Enable in Mods menu

## 📋 Requirements

- RimWorld 1.6 (tested)
- Dependencies:
  - Vanilla Expanded Framework
  - Automatic Hydroponics (parent mod)

## 🚀 Compatibility

- Designed to link with AutoHydroponic, SmallAutoHydroponic, TinyAutoHydroponic
- Harmony patches for speed/yield and UI annotations
- Safe to add/remove mid‑playthrough (effects disappear with facilities)

## 🛠️ Technical Details

- .NET Framework 4.8
- Harmony patches:
  - `PipeSystem.Process.Tick` (applies cumulative speed factor)
  - `PipeSystem.Process.DoInterface` (shows effective duration)
  - `CompAdvancedResourceProcessor.ProcessesOptions` (annotates menu with duration/yield)
  - Yield scaling on result spawn/net insertion
- Facility effects read from `FacilitySpeedBoostExtension`

## 📝 Changelog

### v1.0.0
- Initial release with 3 linkables, UI integration, and research gate

## 🤝 Contributing

- Bug reports and suggestions via Workshop comments or GitHub issues
- PRs welcome

## 📄 License

- MIT (unless stated otherwise)

## 🙏 Credits

- Ludeon Studios – RimWorld
- Harmony team
- Vanilla Expanded Framework
- Automatic Hydroponics (parent mod)

## 🔗 Links

- Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=3562533352
- GitHub: https://github.com/averrin/AutomaticHydroponicsLinkables

---

Enjoy growing smarter hydroponics with modular, linkable upgrades! 🌱
