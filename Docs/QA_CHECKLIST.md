# QA Checklist (manual play-through)

Automated coverage: 19 Play Mode tests (`Assets/_Game/Tests/PlayMode/GameplayTests.cs`). Run them via **Window → General → Test Runner → PlayMode → Run All**. The items below are the things automation cannot judge: feel, readability, audio and visuals. Tick them while playing at 1920×1080 (Game view "Full HD"), then at one other resolution.

## Setup
- [ ] Open `Assets/_Game/Scenes/MainMenu.unity` and press Play (or play the build in `Builds/Windows/`).
- [ ] The Console shows no errors or warnings on entering Play Mode.

## Main menu
- [ ] Music starts (*Perilous Dungeon*). The title, subtitle, diorama, hint line and four buttons are visible.
- [ ] Arrow keys/WASD move between buttons, with a highlight and a select sound; Enter activates.
- [ ] The mouse hover highlight works, and clicking works.
- [ ] **Settings:** both sliders change volume immediately, and the values persist after restarting Play Mode.
- [ ] **Credits:** the text fits inside the panel and Back returns to the menu.
- [ ] **Quit** stops Play Mode (in a build, the app closes).
- [ ] **Play** fades to black, then into Level 1.

## Player feel (Level 1 meadow)
- [ ] Running starts and stops crisply, with no sliding or sticking to walls.
- [ ] A tap-jump is short and a held jump is high (about 3.5 tiles). You can still jump just after leaving a ledge (coyote time), and a jump pressed just before landing still fires (buffer).
- [ ] Landing makes dust, a squash and a thud sound. Jumping makes dust and a stretch.
- [ ] Facing always matches movement. Shots leave from the side you face, never from inside the hero.
- [ ] Holding Up + Shoot fires straight up.
- [ ] Shooting point-blank into a wall makes a spark, and no projectile passes through.
- [ ] The camera follows smoothly with look-ahead, never shows outside the level, and pixels stay crisp.

## Combat and hazards
- [ ] The slime takes 2 hits, flashes red on the first, and bursts with a sound on death.
- [ ] The skeleton patrols and turns at ledges and walls. It dies to 3 hits **from either side**.
- [ ] The ghost flashes violet before firing, and its bolt travels toward you.
- [ ] Touching an enemy or spikes costs **one** heart, knocks you back, and makes you blink. You can't be hit again while blinking.
- [ ] Falling into the chasm (Level 1, x≈63–66) costs a heart and fades you back to the last checkpoint.
- [ ] Losing all hearts plays the death fall, then **You Fell** with Retry and Main Menu. Retry restores 3 hearts at the checkpoint, and defeated enemies stay defeated.
- [ ] Projectiles never hit pickups, ladders, gates' trigger zones or the portal (shoot the portal: the level must not change).

## Level 1 progression
- [ ] Checkpoint pillars light a flame with a glow, play a sound, show "Checkpoint", and refill hearts.
- [ ] Ladder: Up grabs it and you climb to the top ledge. Down from the ledge climbs back down. Jump leaps off.
- [ ] Down + Jump drops through a wooden ledge.
- [ ] The Gold Key pickup shows a toast and a HUD key icon.
- [ ] In the cave: the spider bobs on its thread; the iron chest opens with an animation, and gems fly to you.
- [ ] The gate without the key shows a "Locked" toast and sound. With the key it rises and fades, and the HUD key icon disappears.
- [ ] The portal plays its opening animation as you approach. Entering it shrinks the hero, fades out, and loads the Castle.

## Level 2 — Castle
- [ ] The palette is darker; torches flicker and light the walls; the sky shows above the battlements.
- [ ] The spike-hall bridges and pillar jumps are all doable without taking damage.
- [ ] Bats fly predictably; the tower ladders reach the Gold Key.
- [ ] The Gold Gate opens; the spike gallery is crossable on the bridges.
- [ ] The Treasure Key ledge is reached by its ladder. The Treasure Door refuses the Gold Key and opens only with the Treasure Key.
- [ ] Music is *Spooky Dungeon*. The portal leads to the Treasure Chamber.

## Boss — Treasure Chamber
- [ ] Walking into the hall closes the portcullis behind you, starts the boss music, shows the "Guardian awakens" message, and brings up the health bar.
- [ ] Every attack is clearly telegraphed (flash and shake, red floor markers) before it can hurt you.
- [ ] Fire Volley: fireballs fan out toward you, and the dragon can be hit with straight shots from the side platforms.
- [ ] Dive Slam: the marker appears under you, the slam shakes the screen, and the dragon can be punished on the floor.
- [ ] Fire Rain: the markers are readable and one lane is always safe.
- [ ] Below half health: an enraged tint and message, faster pacing, and shockwaves after slams.
- [ ] Dying mid-fight then pressing Retry fully resets the boss (full bar), reopens the door and restores the pre-fight music.
- [ ] Defeat: explosions, the dragon falls and fades, music fades out, and the gold chest appears.
- [ ] Opening the chest showers gems and a message, then **Victory!** shows gems, time and deaths. Main Menu returns to the title.
- [ ] Difficulty feels fair for a short game. Note the number of attempts and the time taken.

## Pause and UI
- [ ] Esc pauses (gameplay freezes) and Esc again resumes. Pause doesn't open over the death or victory panels.
- [ ] Restart Level reloads the level and discards gems collected in it. Main Menu works from pause.
- [ ] The HUD stays readable at 1280×720 and at 2560×1440.

## Audio
- [ ] Every action has a sound: jump, land, shoot, gem, key, chest, gate, locked, hit, enemy death, hurt, death, checkpoint, portal, UI, boss warning, boss hit and boss death.
- [ ] No sound is too loud or clipping. Swap any you dislike in `Assets/_Game/Resources/SfxLibrary.asset`.
- [ ] Music cross-fades between scenes, and the Music slider affects music only.
