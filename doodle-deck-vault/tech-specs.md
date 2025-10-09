Each Card Has
```cs
struct Unit
{
	string name;
	int energyCost;
	List<CardTraits> traits;
	List<StatusEffects> effects;
	int attackDamage;
	int currentHealth;
	int maxHealth;
	int overhealth;
	int inspiration;
}

struct CardTraits
{
	TraitsEnum trait;
	int val;
}

enum TraitsEnum
{
	LightFooted,
	HealingTrail,
	DriveBy,
	Opportunistic,
	Immobile,
	ProtectiveAura,
	InspiringAura,
	Shielding,
	Cleave,
	Braced,
	GrandEntrance,
	Overtime,
	LeftLauncher,
	RightLauncher
}

enum StatusEffects
{
	Fatigued,
	Intimidated,
	Warded
}
```

```cs
struct Spell
{
	bool isSwift;
	string name;
	int energyCost;
}
```
Kinds of messages the Client needs to send to the Server:
	Place Unit
	Use Spell
	End Turn
	Move Unit




Server is in charge of the game state. The Client must ask the Server to change the game state.