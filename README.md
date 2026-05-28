# Doodle Deck

Doodle Deck is a solo-developed multiplayer tactical card battler built in Unity. Players draw from custom decks, spend energy, place units onto a lane-based board, and use movement, attacks, traits, and status effects to outplay an opponent.

This project is an in-progress prototype. The current version was shown at a live in-person expo/demo and received strong positive feedback from players.

## Demo

Gameplay video & gifs coming soon.

## Technical Focus

Doodle Deck emphasizes gameplay programming, multiplayer networking, and system design. The project is built around server-authoritative game logic, data-driven card definitions, and modular gameplay systems that can support new cards, traits, and mechanics as the design expands.

## Highlights

- Solo-developed Unity prototype
- Server-authoritative multiplayer using Unity Netcode for GameObjects
- Two-player turn system with spectator support
- ScriptableObject-driven card and deck data
- Lane-based unit placement, movement, combat, and death resolution
- Network-synced energy economy and visual indicators
- Trait and status-effect systems including Cleave, Vampiric, Shielding, Braced, Sneak Attack, Inspired, Intimidated, and Warded
- Client hand UI with card selection, hover animation, and board interaction
- Debug UI for testing server/client startup, turns, card draw, and energy behavior

## Gameplay Overview

Each player starts with health, a shuffled deck, and an opening hand. On their turn, a player spends energy to play unit cards, move units between board slots, and attack opposing units in the same lane. If no enemy unit blocks a lane, attacks damage the opposing player directly.

Unit traits create tactical decisions around positioning and timing. For example, Shielding can redirect incoming damage, Cleave can pressure adjacent lanes, Vampiric converts damage into bonus durability, and aura-style traits reward careful board placement.

## Technical Overview

The core architecture is server-authoritative. The server validates player actions, owns game state, resolves combat, updates turn flow, and synchronizes results to clients through RPCs.

Core systems include:

- `GameManager`: turn flow, deck shuffling, card play validation, movement, combat, health, and win/loss handling
- `HandManager`: client hand state, card selection, board click handling, and move/attack requests
- `EnergyManager`: max/current energy, spending validation, and networked energy indicators
- `UnitCard`: unit health, attack, action state, status effects, and synced card UI
- `CardBaseSO`, `UnitCardSO`, `SpellCardSO`, `DeckSO`: data-driven card and deck definitions
- `ClientUIManager`: player/spectator UI, camera setup, turn button, and round display

## Tech Stack

- Unity `6000.2.4f1`
- C#
- Unity Netcode for GameObjects
- Unity Multiplayer packages
- Universal Render Pipeline
- Unity UI / TextMesh Pro
- ScriptableObjects

## Project Structure

```text
Assets/
  Scripts/                  Core gameplay, networking, UI, and card systems
  Resources/ScriptableObjects/
    Decks/                  Deck definitions
    *.asset                 Unit card data
  Prefabs/                  Card, UI card, and energy indicator prefabs
  Scenes/SampleScene.unity  Main playable scene
  Art/                      Card and status-effect artwork
Packages/
  manifest.json             Unity package dependencies
ProjectSettings/
  ProjectVersion.txt        Unity editor version
```

## Current Scope

Implemented:

- Networked turn flow
- Deck loading and shuffling
- Card draw and hand UI
- Unit placement
- Unit movement
- Unit attacks
- Player health and win/loss state
- Energy growth and spending
- Multiple unit traits and temporary status effects
- Spectator client handling

In progress / future work:

- Full spell-card behavior
- Expanded card set and balance pass
- Matchmaking/lobby flow
- Dedicated gameplay tests
- Public demo video and build packaging
