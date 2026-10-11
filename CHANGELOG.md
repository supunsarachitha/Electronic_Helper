# Changelog

Notable changes to Electronic Helper. Versions follow [semantic versioning](https://semver.org/).

## 2.0.0 (unreleased)

A full rebuild. Version 1.9 could no longer be updated on Google Play, which now requires apps to target
Android 16 and ship 64-bit code.

### Fixed

- Ohm's law calculated current as voltage × resistance. It now divides: 12 V across 100 Ω is 120 mA, not 1200 A.
- The power calculator calculated voltage as power × current. It now divides.
- Calculators crashed when a field held zero or text that was not a number. They now explain what is wrong.
- The app waited three seconds on a splash screen before opening. It now opens as soon as it is ready.
- Pressing Back on the home screen asked for confirmation. It now simply goes back.

### Added

- 28 calculators, up from 2: voltage divider, series and parallel networks, LED resistor, RC and LC
  circuits, 555 timers, reactance, op-amp gain, decibels, dBm, battery life, LM317, regulator heat, heat sinks,
  wire voltage drop, PCB trace width, number bases, ADC resolution and component code decoders.
- Results update as you type. Every field has a unit picker, so you can enter 4.7 kΩ or 100 nF directly.
- An interactive resistor colour code reader for 4, 5 and 6 bands, which also finds the bands for a value.
- A logic gate simulator: flip the inputs and watch the symbol, output and truth table change.
- 18 reference pages, including transistors, IC pinouts, USB, coaxial connectors, wire gauges, batteries, a
  formula sheet, Arduino UNO pins, the Raspberry Pi GPIO header, and mains voltage for 129 countries.
- Search, category filters and favourites on the home screen.
- A dark theme that follows the system setting.
- Layouts for tablets and foldables.
- Tap any result to copy it, or share the whole calculation.

### Changed

- Rebuilt on .NET 10 for Android with Material 3. Xamarin.Android, which version 1.9 used, is no longer supported.
- Targets Android 16 (API 36). Requires Android 7.0 (API 24) or later; version 1.9 ran on Android 4.3.
- Ships 64-bit code as an app bundle. Version 1.9 was 32-bit only.
- Reference charts are now drawn from data instead of images, so they are searchable and readable in both themes.
- New launcher icon, with adaptive and themed variants.

### Removed

- Links to e-books. Several pointed at unauthorised copies of commercial textbooks. They are replaced by a
  reading list of free, legitimately published textbooks and courses.
- Chart images copied from third-party websites, including the Arduino board photographs.

## 1.9 (2018-11-22)

The last Xamarin.Android release. Its source is in the history of the `master` branch.
