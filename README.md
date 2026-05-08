# Unity Narrative Dialogue System Tech Demo

A writer-friendly dialogue framework for Unity focused on external `.txt` scripts, runtime parsing, branching conversations, validation, conditions, variables, save/load, and flexible UI presentation.

This project demonstrates how narrative content can live outside gameplay code while still driving characters, backgrounds, choices, variables, inventory, relationships, UI, and scene changes.

---

## Demo

GIF/video here.

Demo scenes:

- `Assets/_Project/_Scenes/Test_TextBox.unity`
- `Assets/_Project/_Scenes/Test_ScreenText.unity`

Sample scripts:

- `Assets/_Project/Resourses/DemoDialogue.txt`
- `Assets/_Project/Resourses/DemoDialogueKinnetic.txt`

---

## Features

- External `.txt` dialogue files
- Writer-friendly line-based scripting syntax
- Runtime dialogue parsing
- Dialogue graph, nodes, lines, hubs, and choices
- Branching choices with tab-based consequences
- Nested hubs and hub jumps
- `[once]` choices
- Generic variables and flags
- Inventory state and item conditions
- Relationship values and relationship conditions
- Character/background commands
- Scene switching command
- Screen-text page break command
- Character portraits for relationship UI
- Character emotion sprites: `Normal`, `Angry`, `Happy`
- Emotion tags inside dialogue lines
- Typewriter text effect
- Space/click to reveal or continue text
- Text box dialogue mode
- Kinetic novel / screen text mode
- TMP prefab-based dialogue UI
- Relationship panel UI
- Inventory panel UI
- Dialogue log panel
- Async JSON save/load
- Save migrations foundation
- Dialogue validation system with line-aware errors
- Modular runtime architecture

---

## Example Dialogue Script

```txt
showBackground: Room
showCharacter: Anna

Narrator: Round one begins. Two minutes with Anna, cover name Velvet, actual job unknown.
Anna: You're late. [Anna angry]
Player: The invitation said mysterious rooftop bar, not abandoned safehouse.
Anna: Good. You can read a room. [Anna happy] [set metAnna true]

Hub AnnaIntro
Player: Apologize smoothly. (Sorry. I got held up by surveillance.) [once]
	Anna: Surveillance? Cute answer. Maybe even true. [relationship trust Anna +1]
	Anna: Take this microphone. If you hear static, smile and leave. [get microphone]
	Hub AnnaFollowUp
	Player: Compliment her tradecraft. (You planned this well.)
		Anna: Planning is easy. Trusting someone at the table is harder. [relationship trust Anna +1]
	Player: Ask about the mission. (Who are we listening for?)
		Anna: The woman at the next table. May. She flirts like a weapon.
	Back to first questions. [back Hub AnnaIntro]
Player: Tease her. (I thought spies liked dramatic entrances.)
	Anna: We like punctual ones more.

Anna: Time is almost up.
hideCharacter: Anna
```

Tabs matter. A tabbed block belongs to the choice or hub above it. When the parser returns to a line with less indentation, that branch is finished.

---

## Script Syntax

### Dialogue Lines

```txt
Anna: You're late.
Player: I had no choice.
Narrator: Time is up.
```

`Player` appears in the player dialogue box. Other character names use the character dialogue box. `Narrator` is treated as narrator text in text-box mode.

### Commands

Commands are written on their own line.

```txt
showBackground: Room
hideBackground: Room

showCharacter: Anna
hideCharacter: Anna

startScene: MainMenu
startNextPage
```

Command notes:

- `showBackground` looks up a background in `BackgroundDatabase`.
- `showCharacter` looks up a character in `CharacterDatabase` and shows its `Normal` emotion sprite.
- `startScene` loads a Unity scene by name.
- `startNextPage` is used by Screen Text mode to clear the current page and continue on a fresh page.

`showScene` is also accepted as an alias for `showBackground`.

### Choices And Hubs

```txt
Hub AnnaIntro
Player: Apologize. (Sorry.)
	Anna: Fine.
Player: Leave. (*Leave.*)
	Anna: Typical.
```

Choice format:

```txt
Speaker: Button text. (Selected line text.)
```

The text before parentheses is shown on the choice button. The text inside parentheses becomes the first spoken line after choosing.

### Nested Hubs

```txt
Hub AnnaIntro
Player: Ask about the mission. (Who are we listening for?)
	Anna: The woman at the next table.
	Hub AnnaFollowUp
	Player: Ask about May. (Who is May?)
		Anna: A problem in lipstick.
```

Nested hubs let a selected branch contain another choice set.

### Hub Jumps

```txt
Back to first questions. [back Hub AnnaIntro]
Go to May. [go Hub MayDate]
```

`[back Hub X]` and `[go Hub X]` jump to another hub by name. The `Hub` prefix is optional inside the tag.

### Once Choices

```txt
Player: Apologize smoothly. (Sorry.) [once]
```

After this choice is selected, it will not appear again if the player returns to the same hub.

### Variables

```txt
Anna: Good. [set metAnna true]
Narrator: You chose the spy route. [set route spy]
Narrator: Score updated. [set score 3]
```

Variables are stored in `PlayerController`, saved to JSON, and restored on load.

Variable conditions:

```txt
Player: I know Anna. (I met Anna earlier.) [if metAnna true]
Player: Take spy option. (Use the spy route.) [if route == spy]
Player: Continue. (I am ready.) [if score >= 3]
```

Supported variable operators:

```txt
==  !=  >  <  >=  <=
```

Numeric comparisons work when both values can be parsed as numbers. Otherwise, equality checks compare text case-insensitively.

### Inventory

```txt
Anna: Take this microphone. [get microphone]
May: You lost it? [lose microphone]
```

Aliases:

- Add item: `[get itemName]`, `[take itemName]`, `[add itemName]`
- Remove item: `[lose itemName]`, `[lost itemName]`, `[remove itemName]`, `[drop itemName]`

Inventory conditions:

```txt
May: You have the microphone. [if got microphone]
May: You came empty-handed. [if no microphone]
```

Aliases:

- Has item: `[if got itemName]`, `[if has itemName]`, `[if have itemName]`
- Missing item: `[if no itemName]`, `[if missing itemName]`, `[if notgot itemName]`

### Relationships

```txt
Anna: Maybe I can trust you. [relationship trust Anna +1]
Anna: That was suspicious. [relationship suspect Anna +1]
```

Relationship format:

```txt
[relationship relationshipType characterName delta]
```

Relationship conditions:

```txt
Anna: You earned a little trust. [if trust Anna > 0]
Anna: I do not trust you yet. [if trust Anna == 0]
```

Supported relationship operators:

```txt
==  !=  >  <  >=  <=
```

Relationship types are ScriptableObjects, so designers can add more than `trust` and `suspect`.

### Emotions

Characters use a list of emotion sprites in their `CharacterData` ScriptableObject.

Current emotion enum:

```txt
Normal
Angry
Happy
```

Emotion tag format:

```txt
Anna: You're late. [Anna angry]
Player: No. [Anna angry]
May: Nice answer. [May happy]
```

The character must already be visible on stage. If the character is hidden or the emotion sprite is missing, the system logs a warning.

---

## UI Modes

The dialogue runner supports two display modes:

### Text Boxes

Current visual-novel style dialogue boxes:

- Character/Narrator text appears on the left
- Player text appears on the right
- Choices appear on the right
- Last dialogue line remains visible while choices are shown

### Screen Text

Kinetic novel style text over the screen:

- Multiple lines collect on the screen
- `startNextPage` forces the next line onto a new page
- Hubs and choices are skipped in this mode

Both modes use prefab-based TMP UI:

- `Assets/_Project/Prefabs/UI/DialogueBox.prefab`
- `Assets/_Project/Prefabs/UI/ScreenText.prefab`

---

## Validation System

`DialogueValidator` checks dialogue scripts and reports line-aware errors/warnings.

It currently checks:

- Parse errors
- Duplicate hub names
- Missing hub targets
- Choice links to duplicate hubs
- Unknown character speakers
- Unknown `showCharacter` / `hideCharacter` targets
- Unknown `showBackground` / `hideBackground` targets
- Unknown inventory item references
- Unknown relationship types
- Unknown relationship characters
- Emotion tags for unknown characters
- Missing emotion sprites

Run validation from the `DialogueRunner` component context menu:

```txt
Validate Dialogue
```

The result is printed to the Unity Console.

---

## Architecture Overview

```txt
Dialogue File (.txt)
        |
        v
DialogueParser
        |
        v
DialogueGraph / DialogueNode / DialogueLine / DialogueChoice
        |
        v
DialogueValidator
        |
        v
DialogueRunner
        |
        v
DialogueUI / Stage / PlayerController / SaveSystem
```

Core systems:

- `DialogueParser`
  Parses raw `.txt` files into runtime dialogue graphs.

- `DialogueGraph`
  Contains parsed dialogue nodes, hubs, lines, commands, and choice consequences.

- `DialogueValidator`
  Checks scripts for broken references, missing hubs, duplicate hubs, invalid database references, and missing emotion sprites.

- `DialogueRunner`
  Executes parsed dialogue, commands, choices, variables, conditions, UI flow, character/background display, and save/load state.

- `PlayerController`
  Stores relationships, inventory, and generic variables.

- `SaveSystem`
  Saves and loads JSON asynchronously using Unity persistent data path.

- `DialogueUI`
  Creates/uses TMP prefab UI for text boxes, screen text, and choices.

---

## ScriptableObject Data

The system uses ScriptableObject databases for designer-editable content:

- `CharacterData`
  Character name, portrait, and emotion sprite list.

- `CharacterDatabase`
  Lookup database for characters.

- `BackgroundData`
  Background name and sprite.

- `BackgroundDatabase`
  Lookup database for backgrounds.

- `ItemData`
  Inventory item name and image.

- `ItemDatabase`
  Lookup database for inventory items.

- `RelationshipTypeData`
  Relationship type name.

- `RelationshipTypeDatabase`
  Lookup database for scalable relationship types.

---

## Save / Load

The project includes async JSON save/load.

Saved state includes:

- Current dialogue node and element index
- Active hub
- Last retained dialogue line before choices
- Current background
- Visible characters
- Current character emotions
- Used `[once]` choices
- Dialogue log entries
- Relationships
- Inventory items
- Generic variables

---

## Controls

- `Space` - continue dialogue
- `Space` while typewriter is running - reveal the full current line
- Left mouse click on screen - same as Space
- Mouse click on choice button - select choice
- Number keys `1` to `9` - select visible choice by index
- UI buttons - Save, Load, Restore, Inventory, Relationships, Log

---

## How To Run

1. Clone the repository.
2. Open the project with Unity `6000.4.5f1` or newer.
3. Open one of the demo scenes:
   - `Assets/_Project/_Scenes/Test_TextBox.unity`
   - `Assets/_Project/_Scenes/Test_ScreenText.unity`
4. Press Play.
5. Use the `DialogueRunner` inspector to assign a `.txt` dialogue file and databases.
6. Use `Validate Dialogue` from the component context menu to check the script.

---

## Project Structure

```txt
Assets/
  _Project/
    Art/
      Backgrounds/
      Characters/
    Prefabs/
      UI/
    Resourses/
      DemoDialogue.txt
      DemoDialogueKinnetic.txt
    ScriptableObjects/
      Backgrounds/
      Characters/
      Database/
      Items/
      RelationshipTypes/
    Scripts/
      Dialogue/
      Runtime/
      Save/
      ScriptableObjects/
    _Scenes/
```

Note: the folder is currently named `Resourses` in the project.

---

## Design Goals

- Make dialogue files readable for writers and narrative designers
- Keep narrative content separate from gameplay code
- Support branching dialogue without requiring a graph editor
- Make state changes explicit in script tags
- Allow designers to validate files before playtesting
- Keep runtime systems modular and extensible
- Support visual novel and kinetic novel presentation styles

---

## Why External Dialogue Files?

External `.txt` dialogue files were chosen because they are:

- Easy for writers to edit
- Easy to diff in version control
- Fast to iterate on
- Easier to validate automatically
- Better suited to narrative-heavy projects than hardcoded dialogue

Tradeoff:

Custom parsing adds complexity, so validation and clear error messages are important.

---

## Known Limitations

- No graph editor
- No localization pipeline
- No voice-over support
- No timeline integration
- No generic custom event dispatcher yet
- No complex expression parser
- No automated unit test suite yet
- UI blur material exists but does not have a complete URP blur texture pipeline yet

---

## Future Improvements

- Generic custom events, for example `[event alarmStarted]`
- Editor validation window for validating all dialogue files at once
- Dialogue debugging window
- Graph visualization
- Localization table export/import
- More robust expression syntax
- More UI prefab customization
- Automated parser and validator tests
- Async dialogue asset loading

---

## Screenshots

Screenshots here.

---

## License

MIT. See [LICENSE](LICENSE).
