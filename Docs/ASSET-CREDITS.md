# Cat's Mission — asset sources

Only selected files are imported into `Assets/CatsMission/ThirdParty`.
Downloaded bundles and unused files are kept out of the Unity asset database.

| Source | Imported files | License |
| --- | --- | --- |
| [Kenney Nature Kit 2.1](https://kenney.nl/assets/nature-kit) | Three tree models and three flower models (FBX) | CC0; original License.txt included |
| [Kenney UI Pack](https://kenney.nl/assets/ui-pack) | Green button rectangle | CC0; original License.txt included |
| [Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds) | confirmation_001 → collect, open_001 → jump, error_003 → life, tick_001 → warning, click_001 → click | CC0; original license included |
| [Kenney Music Jingles](https://kenney.nl/assets/music-jingles) | jingles_PIZZI07 → victory; jingles_PIZZI04 → game-over | CC0; original license included |
| [Happy — Pro Sensory](https://opengameart.org/content/happy) | happy_song_1.mp3 → garden-music.mp3 | CC0, as stated on the source page |
| [Birds and Wind — Spring Spring](https://opengameart.org/content/birds-and-wind-ambient-birds-wind-and-synth) | Birds and Wind - Ambient_0.ogg → garden-ambience.ogg | CC0; original bird recordings credited on source to isaiah658, syncopika and pauliuw |

The existing Miso cat, house, fish, terrain and obstacles remain project-created.
The heart silhouette is generated locally by AssetPolish; no third-party license is needed for it.
The Quaternius Ultimate Animated Animals pack was inspected but not imported:
its FBX folder did not contain a cat. No third-party gameplay scripts or packages were added.

## Integration

`GameAudio` uses quiet looping music and ambience sources and one effects source.
Audio is paused with gameplay; success and failure stop the music and play a jingle.
Imported trees and flowers replace decorative renderers only; original colliders remain.
`AssetPolish.Apply` updates the four build scenes, with backups before their first edit.
It can be reapplied without duplicating trees, hearts or audio components.

## Controls and updated navigation

- WASD / arrows: move; Space: jump; Escape: pause or resume.
- Game Over waits for Retry or Main Menu; there is no automatic retry.
- The final victory button starts a new campaign and resets its score.

## Validation records

See `Docs/Testing/assets-edit-01.xml` and `assets-play-01.xml` for the current run.
These tests do not replace human listening and a full player walkthrough.
