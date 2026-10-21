# Jewel and WCoin C personal stores

This deployment adds a Season 6 preset for Bless, Soul, Chaos, Creation, Life and WCoin C prices.
WCoin P/Goblin Point prices and automatic play-time Goblin Point awards are disabled by this preset.
Existing balances are not deleted or converted. Zen remains available. All three account balances
remain supported by the protocol; changing the allowed currencies later only requires configuration.
This does not configure a real-money payment provider or change the separate XShop product catalog.

## Existing database

After updating and rebuilding the server and client, apply the optional configuration update named
**Enable jewel and WCoin C personal stores** in the OpenMU administration panel's configuration updates.
It enables PlayerShopCurrenciesFeaturePlugIn, replaces its currency configuration with this preset,
and disables GoblinPointsForPlayTimePlugIn. This is a saved configuration change, not a database reset.
Restart the GameServer and reconnect clients so they receive the enabled currency list.

The preset uses the existing default limits: 1,000 jewel units and 1,000,000 account coins per item.
These remain editable through the plugin configuration. Applying the update explicitly again resets
those settings to the preset; normal startup does not continually overwrite later operator edits.
Fresh Season 6 databases receive the same personal-store preset during initialization.

Existing schema migrations AddCashShop and AddItemStorePriceCurrency must already be applied using
the server's normal migration procedure. This change adds no new entity fields or schema migration.
No running database was edited by the code publication.

## Client and balance behavior

The shared Windows/Android client implements the existing extended store-price and currency packets.
The inventory displays WCoin C; all three account balances are decoded separately. Read-only balance
requests can now be answered while playing, without opening XShop. Buying/consuming cash-shop products
retains its original open-window, feature, life, and safe-zone checks. Coin payments notify both accounts
of their new balances through the existing cash-shop point-info view.

The existing server payment implementation is reused: single jewels and supported 10/20/30 bundles
must pay exactly, bundles are not split, and the seller must have inventory space. Account-coin payments
use the existing persistence flow (buyer saved immediately, seller through normal progress saves);
this patch does not replace that flow with an atomic cross-account ledger.

No build or runtime tests were executed at the operator's request. Configuration tests are supplied
for fresh databases, missing configuration and idempotence, but require execution locally. Verify
both balances and inventory after purchase and reconnection, including insufficient funds, exact
jewel payment, full seller inventory, disabled currencies and Android interaction.
