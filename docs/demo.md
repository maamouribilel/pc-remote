# Try PC Remote in two minutes

This tour uses your paired browser and the computer screen in front of you. Complete [setup and certificate trust](setup.md#pair-a-browser) first. The animation is a preview of the actual **0.4.2** interface, which 0.4.3 leaves unchanged.

<img src="assets/v0.4.2/demo.gif" alt="Captioned PC Remote demo covering Touch, Air, keyboard, navigation, media and apps" width="900">

The recording uses a temporary recording backend and synthetic motion. Its test launcher and volume are fixture data; it does not move a real desktop pointer, type into another app, change physical audio or execute power actions. [Capture provenance](assets/README.md).

## 1. Move and click · 20 seconds

Open **Touchpad**, keep **Touch** selected, and watch your computer.

- Slide one finger to move; tap to click.
- Scroll with two fingers.
- Choose **Start drag**, move, then **End drag**.
- Tap **Release input** to clear any held controls.

## 2. Point with your wrist · 25 seconds

Select **Air**. Tap **Enable motion** if shown and allow the browser's motion request.

Hold your thumb on the pad and turn your wrist. **Pointing** appears while held. Lift to stop, re-grip, then hold again. A short tap clicks.

Open the settings button beside **Air** to adjust speed or invert either axis. If motion permission was denied, reload and try the permission tap again; if the device has no sensor, choose **Use Touch**. [Air instructions and current device limits](setup.md#air-pointer).

## 3. Send a line of text · 20 seconds

Locally open a disposable empty text document and focus it. Choose **Keyboard** on your phone, enter `Hello from PC Remote — café!`, then tap **Send text**. It sends once; composition alone sends nothing.

Try **Navigation → Left arrow** or **Home** in that document. These keys go to the app focused on the computer.

## 4. Choose a media profile · 20 seconds

Focus a player on the computer, then select its profile in **Media**:

| Profile | Use it with |
| :--- | :--- |
| General | System media controls; the OS chooses the media recipient (Linux requires an unambiguous controllable MPRIS player). |
| YouTube | The focused browser player and its default shortcuts. |
| VLC | The focused VLC window and its default shortcuts. |
| Stremio | The focused Stremio player; Next is next episode. |

Tap **Play / pause**. The separate system-volume slider sets the current output; it can unmute it. If you try it, note the old level and restore it afterward. Controls depend on platform capabilities.

## 5. Open an app · 20 seconds

In the companion's launcher settings, add a local app and choose its media profile. Back in **PC**, tap that configured app. It requests launch/activation and switches the phone to the configured profile in Media. If the computer denies focus, select the app locally.

The recording uses **Test player**, an inert fixture entry. On your machine, you choose the apps. [Launcher configuration](pc-features.md#app-launcher).

## 6. Finish · 15 seconds

Tap **Release input**. Use the companion's **Pause control** when you want to suspend the session.

The PC tab also offers supported power actions. Opening a power confirmation schedules nothing; **Keep PC running** dismisses it. Confirming starts a 30-second countdown, so leave power execution out of this introductory tour.

## What this demo establishes

The fresh capture exercises the real browser UI over loopback HTTPS/WSS: pairing, touch/drag/release, Air ready/hold/release, composed text, navigation, media profile and volume read-back, configured app selection and dismissal of a power confirmation. Commands are inspected on recording adapters.

It is browser UI evidence. It does not establish new physical iPhone, Android, macOS/Ubuntu desktop, native input/audio, certificate onboarding or power acceptance. The [compatibility table](../README.md#compatibility) and [release evidence](release-evidence.md) keep those results separate. There is no public hosted control demo; your companion serves the remote on your own network.
