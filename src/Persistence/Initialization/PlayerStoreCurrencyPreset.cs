// <copyright file="PlayerStoreCurrencyPreset.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization;

using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerShopCurrencies;

/// <summary>The operator's initial personal-store policy: jewels and WCoin C, with Zen compatibility.</summary>
internal static class PlayerStoreCurrencyPreset
{
    /// <summary>Creates editable configuration using the existing item definitions.</summary>
    internal static PlayerShopCurrenciesConfiguration Create(GameConfiguration gameConfiguration)
    {
        var configuration = new PlayerShopCurrenciesConfiguration
        {
            IsWCoinCAllowed = true,
            IsWCoinPAllowed = false,
            IsGoblinPointsAllowed = false,
        };
        ItemIdentifier[] jewels =
        [
            ItemConstants.JewelOfBless, ItemConstants.JewelOfSoul, ItemConstants.JewelOfChaos,
            ItemConstants.JewelOfCreation, ItemConstants.JewelOfLife,
        ];
        foreach (var jewel in jewels)
        {
            if (gameConfiguration.Items.FirstOrDefault(item => item.Group == jewel.Group && item.Number == jewel.Number) is { } item)
            {
                configuration.PriceItems.Add(item);
            }
        }

        return configuration;
    }
}
