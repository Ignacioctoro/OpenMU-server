# Force and ammunition diagnostics (broyal-server)

This is diagnostic support, not a confirmed damage/consumption fix. No damage,
requirements, configuration or ammunition amounts are changed by this patch.
No build or in-game execution was performed.

As a GM, run /combatinfo on yourself, or /combatinfo char=OnlineCharacterName.
The command reports the live effective ammo rate, attribute count, equipped
ammo durability, Infinity Arrow state, both hand items and their configured
base ammo power-ups. It also reports configured and available Force skills
60, 66 and 509, their skill/damage types, base damage, hits and restrictions.
Full lines are logged with COMBATINFO; chat lines are limited to 72 characters.

For the affected Elf, run it before and after shooting without Infinity Arrow:

- rate=1 and amount/inventory both decrease: investigate client 0x2A delivery.
- rate=0, InfinityArrow=False: investigate live bow/global/buff modifiers.
- amount decreases but inventory does not: investigate IsAmmunition and the
  durability synchronization callback.

The S6 Stinger Bow initializer (4:23) adds ammo rate 1. Excellent options alone
do not make it ammunition-free. The actual runtime output is authoritative.

For DL, compare Force 60 and effective Force Wave 66. The server may substitute
66 when the weapon grants it. Force Wave 66 is intentionally hidden from the
client palette. FORCE warnings identify ammunition blocking (which would be
invalid for a non-bow weapon) or targets rejecting the hit. Debug FORCE hit
entries contain the resulting health damage. Existing validation warnings
still report unsupported skill types/configuration.

Send the COMBATINFO output for both characters and any FORCE warnings to finish
the diagnosis. Do not change stats or decrement arrows client-side to mask a
server-side issue.
