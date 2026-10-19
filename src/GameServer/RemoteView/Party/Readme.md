# Party vitals for the open-source client

`PartyVitalsViewExtendedPlugIn` replaces the health view for client definitions
with Season >= 106 and Episode >= 3 (the existing 2.04d open-source definition).
The original Season 6 health view and packet are unchanged. Update both the
custom client and server together, then restart the server and reconnect clients.
No database migration or character-data modification is required.

The extended C1/44 packet contains a count at offset 3 and 17 bytes per member:
party index (byte), current HP, maximum HP, current MP, maximum MP (four uint32
little-endian values). The receiving client distinguishes it from the original
one-byte-per-member health packet by exact payload length. It validates length
before applying updates. Mana is retained across list refreshes only when the
same name remains at the same index, and cleared when the member is removed.

Mana is read from the server attributes, including members outside the receiver's
viewport. Offline party snapshots have no mana attributes; their maximum is zero
and the client does not display an invented value. Remote aura icons still depend
on the existing observer effect packets and are only available in view.

Packet sources are in `Network/Packets/ServerToClient/PartyExtendedPackets.xml`.
The packet project's normal generation step regenerates their structures and
documentation. No original protocol definitions are overwritten.

Validation for this change: XML schema validation and source inspection only.
Compilation and in-game testing are intentionally left to the operator.
