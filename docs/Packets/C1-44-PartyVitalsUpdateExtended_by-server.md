# C1 44 - PartyVitalsUpdateExtended (by server)

## Is sent when

Periodically when party membership, health or mana changes. Used by clients of Season 106 Episode 3 or newer.

## Causes the following actions on the client side

The client updates party health and mana bars, including members outside the local viewport. Original Season 6 clients continue receiving PartyHealthUpdate.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC1  | [Packet type](PacketTypes.md) |
| 1 | 1 |    Byte   |      | Packet header - length of the packet |
| 2 | 1 |    Byte   | 0x44  | Packet header - packet type identifier |
| 3 | 1 | Byte |  | Count |
| 4 | PartyMemberVitalsExtended.Length * Count | Array of PartyMemberVitalsExtended |  | Members |

### PartyMemberVitalsExtended Structure

Party list index and current and maximum health and mana.

Length: 17 Bytes

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 | Byte |  | Index |
| 1 | 4 | IntegerLittleEndian |  | CurrentHealth |
| 5 | 4 | IntegerLittleEndian |  | MaximumHealth |
| 9 | 4 | IntegerLittleEndian |  | CurrentMana |
| 13 | 4 | IntegerLittleEndian |  | MaximumMana |