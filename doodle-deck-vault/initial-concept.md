## Overview
### Major Constraints
Lackluster art skills
2.5 weeks to make the game
Unfamiliar with networks / programming multiplayer experiences

### Project Purpose
Doodle Deck is being created for a distributed systems project for my university degree. I try to relate every project to my future work and I believe that a networked game has a lot to teach me. I also really wanted an excuse to make a card game (I've never designed one before and it seemed like a lot of fun). (we'll see how much I come regret this given the short timeline)

### Basic Concept
Doodle Deck is an online 1v1 card game that is designed around the positions of cards on the play field. 

## Major Gameplay Elements
### The Board
![[board-layout.png|left]]

#### Slots (Positions)
Each side has 5 Slots (POS\[0-4])

Slots can be either occupied or empty

Each slot can be occupied by 1 Unit

### Your Resources
#### Energy
Energy is used to play Cards or activate specified Card Game Rules

At the start of the game, the player who goes first, has 1 max Energy, and the other player has 2 max Energy

Every round after the first, each player gets 1 more max Energy (given to them on their turn), up to a max of 10 max Energy

The player's Energy is refilled at the start of their turn

#### Player Health
Each player starts with 20 Health

When a player's Health >= 0, they lose

#### Your Hand
You can have up to 5 Cards in your Hand

If you have more than 5 Cards in your Hand at the end of your Turn you must discard Cards until you have 5

Cards from your Hand can be played according to their Card Type
##### Card Types
**Unit**
Unit Cards can be placed on the Board

When a Unit's Health is reduced to 0, it is sent to it's Graveyard

**Spell**
Spell Cards can only be played on your Turn

Spell Cards' Game Rules are activated immediately upon playing, and then the Card is sent to your Graveyard

**Swift Spell**
Swift Spell Cards can be played on either player's Turn

Swift Spell Cards' Game Rules are activated immediately upon playing, and then the Card is sent to your Graveyard

#### Your Deck
You can have 30-50 Cards in your Deck

Duplicates are allowed in your deck

If you run out of Cards, you lose

#### Your Graveyard
Your Graveyard is where Cards go when they die, are used up, or discarded

### The Cards
#### Anatomy of a Unit Card 
![[card-layout.png|left|400]]
Every Unit has:
- Card Name
- Energy Cost (The circles right of the Card Name)
- Card Doodle (Card Art)
- Card Type
- Game Rules (These can affect & change most every basic rule)
- Damage / Health Values (The two numbers in the bottom right)

Every turn, each Unit gets 1 Action which can be used to:
	Attack the card directly across from them (unless otherwise specified in the Card's Game Rules)
	Move 1 position over (unless otherwise specified in the Card's Game Rules)
	Invoke Game Rules that require an action

When the Unit's Action is used up, it cannot take another action until the player's (who owns the card) next turn (unless specified by the Game Rules)

Unit Health and Actions are reset at the beginning the player's turn

#### Attacking & Defending
When a player attacks with a Unit, if there is a Unit directly across from the attacking Unit, that Unit is attacked, otherwise the enemy player is attacked

If a Unit attacks another card, they both do damage to each other's Health equal to their Damage
Ex.
	(Damage / Health)
	A(1 / 5) attacks B(2/ 3)
	A takes 2 damage and now has 3 Health => A(1/3 {MAX 5})
	B takes 1 damage and now has 2 Health => B(2/2 {MAX 3})

When a Unit's Health reaches 0, they are sent to their Graveyard

#### Moving
When a player moves a Unit to an adjacent slot
	If there is no Card in that slot, the Card is moved to that slot
	If there is a Card in that slot, the Cards swap slots

### Your Turn
#### Turn Start
Draw 1 Card
Refill Your Cards' Health to max
Refill Your Energy to max

#### Mid Turn
Play Cards
Use Card Actions
	Attack
	Move
	Activate Action-Required Game Rules

#### Turn End
You can end your Turn at any time
