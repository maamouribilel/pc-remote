# PC Remote logo

The shared logo represents a phone controlling a desktop PC. The mint mark, graphite background, and white pointer match the application's interface. The master is `assets/branding/pc-remote-logo-v2.png`; exported web/Windows assets are in `src/remote/public/pc-remote-v2-*`. Existing v1 assets are retained.

Generated with the built-in image generation tool. PNG size conversion and multi-frame ICO packaging use Windows WPF in `scripts/generate-icons.ps1`. Run that script to regenerate the exports. The Windows executable, window icon, dashboard header, notification tray, installer, browser favicon, remote header, and web manifest use this identity. The 180px opaque Apple touch icon is explicitly linked with its size; versioned filenames avoid reuse of the previous icon URL. A separate padded 512px maskable icon keeps the symbol inside the safe circle.

On an iPhone or iPad, open the companion's address in Safari, then Share → Add to Home Screen → keep Open as Web App enabled → Add; other browsers offer their own Add to Home Screen or Install option, which the project has not verified. If an existing Home Screen shortcut retains the old image, remove that shortcut and add it again. The Home Screen app may need PC approval separately from Safari. Actual iPhone icon display needs confirmation on the device.

Apple references: https://developer.apple.com/library/archive/documentation/AppleApplications/Reference/SafariWebContent/ConfiguringWebApplications/ConfiguringWebApplications.html and https://support.apple.com/guide/iphone/iphea86e5236/ios

## Generation prompt

Use case: logo-brand
Asset type: final square application icon for PC Remote, shared by the Windows, macOS and Ubuntu companions and the browser remote, including its Home Screen web app.
Primary request: create a modern, immediately understandable logo showing a phone controlling a desktop computer. One single polished icon, not a presentation sheet.
Style: minimal flat vector-like geometry, exceptionally clean thick strokes, rounded joins, strong readable silhouettes at 32 pixels. A large mint desktop monitor outline with a short centered stand; a smaller solid mint smartphone overlaps the lower right of the monitor. The phone has a dark inset screen and a simple white cursor arrow centered in its screen, signifying remote control. Keep the composition balanced and uncomplicated.
Color palette: full-bleed uniform dark graphite #0b0f14 background, mint #5ee8be monitor and phone, white #f4f6f8 cursor.
Composition: square 1024x1024 canvas, straight-on front view, centered mark, all foreground shapes inside the central 76 percent of canvas. Generous clear padding. The dark background extends exactly to all four edges, with square corners; the operating system will apply its own corner mask.
Constraints: no words, letters, gradients, texture, shadows, glows, 3D, tiny details, Wi-Fi arcs, decorative dots, borders, watermarks, device mockups or external logos.
