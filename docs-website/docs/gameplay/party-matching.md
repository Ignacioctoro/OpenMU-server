# Party matching for the NewUI client

This optional extended-client interface adds a recruitment directory within one GameServer.
It does not link subservers or teleport characters. Legacy Season 6 party packets and the
configured party size remain unchanged. The NewUI client opens it with P or the Party button.

## Use

- **My Party:** members, leader, health and map; leave or remove a member using existing party actions.
- **Search:** paged title/leader search, member count, map, required base level and classes;
  apply with a password when the advertisement requires one, or withdraw an application.
- **Recruit:** a solo character or party leader publishes a title, level range, allowed S6 class
  families, optional password and optional automatic acceptance. The advertised map follows
  the leader. Publishing again replaces the previous advertisement and its applications.
- **Requests:** leaders accept/reject applicants. The client polls every five seconds while open
  or while its character has an active advertisement/application, and shows a request-count badge.

Applications expire after two minutes. Listings are temporary and disappear when the leader
disconnects, changes character, loses leadership, disbands the advertised party or cancels.
Cleanup happens before the next directory operation; stale listings are never used for admission.
Party capacity and base-level maximum come from the GameConfiguration. Level restrictions use
normal character level, not resets or master level. Class filters include class advancements.

Joining rechecks connection, player state, leader identity, capacity, level/class requirements,
event party restrictions and the normal PartyResponseAction. Both players are reserved through
the existing party-request state while processing admission. A requester can have one pending
application. Only the leader of the stored advertisement can accept/reject it. No client sends
the effective leader identity, party capacity or destination player reference.

## Deployment

Update both client and server, then rebuild and restart them with your usual commands. No database
migration or package version change is required. Both new plugins appear as **Party matching for
extended clients**: a packet handler and a view. They target Season 106 Episode 3 extended clients.
If the client reports no response, verify both plugins are active for that client configuration.
The existing game connection carries matching; it does not use the ChatServer or a new port.

Limits: 500 advertisements per GameServer, 20 pending requests per advertisement, eight ads per
page, 300 ms minimum between a player's requests, 40-character titles/search and 16-character
passwords. These are feature/protocol bounds, not replacements for the configured party size.
Passwords use the encrypted C4 game transport and are stored only as SHA-256 hashes in memory.
Neither passwords nor their hashes are present in any response.

## Protocol and verification

The XML source is `src/Network/Packets/PartyMatchingExtendedPackets.xml`: opcode ED, version 1,
C4 request / C2 snapshot and bounded UTF-8 JSON using the DTOs in GameLogic/PartyMatching.
Status codes: 0 snapshot, 1 done, 2 unavailable, 3 not leader, 4 invalid input, 5 membership/full,
6 missing listing/application, 7 requirements, 8 password, 9 throttled/limit, 10 pending, 11 joined.
Client messages are rendered from these codes. The request operations are documented in the XML.
Regenerate the extension with `GeneratePartyMatchingPackets.ps1` in the packet directory.
Generated sources are committed separately so no existing package project/version is modified.

This change was syntax-checked and its XML/schema, DTO agreement and packet envelopes checked
without compiling, per the operator's request. Generated packet tests are included but were not run.
In-game verification remains required: two accounts on different maps; publish/search/apply/accept;
auto-accept; wrong password; wrong class/level; full party; withdrawal/rejection; disconnect;
leader change; two simultaneous applicants for the last slot; Android text entry; existing S6 party.
