// <copyright file="EnableJewelAndWCoinCStoresUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.PlayerShopCurrencies;
using MUnique.OpenMU.PlugIns;

/// <summary>Applies the operator's jewel/WCoin C store policy to an existing Season 6 configuration.</summary>
[PlugIn]
[Display(Name = nameof(StoreCurrencyUpdateResources.Name), Description = nameof(StoreCurrencyUpdateResources.Description), ResourceType = typeof(StoreCurrencyUpdateResources))]
[Guid("99C181B2-C26A-4D57-9947-F8B5F5B06B17")]
public sealed class EnableJewelAndWCoinCStoresUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string Name => "Enable jewel and WCoin C personal stores";

    /// <inheritdoc />
    public override string Description => "Enables Bless, Soul, Chaos, Creation, Life and WCoin C prices (Zen remains available). Replaces personal-store currency settings with default limits, disables WCoin P/Goblin Point prices and play-time Goblin Point awards. Existing balances are preserved. Requires the updated client.";

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var typeId = typeof(PlayerShopCurrenciesFeaturePlugIn).GUID;
        var plugin = gameConfiguration.PlugInConfigurations.FirstOrDefault(p => p.TypeId == typeId);
        if (plugin is null)
        {
            plugin = context.CreateNew<PlugInConfiguration>();
            plugin.SetGuid(typeId);
            plugin.TypeId = typeId;
            gameConfiguration.PlugInConfigurations.Add(plugin);
        }

        plugin.SetConfiguration(PlayerStoreCurrencyPreset.Create(gameConfiguration), new IdReferenceHandler());
        plugin.IsActive = true;
        foreach (var award in gameConfiguration.PlugInConfigurations.Where(p => p.TypeId == typeof(GoblinPointsForPlayTimePlugIn).GUID))
        {
            award.IsActive = false;
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>Serializes configured jewel references by their persisted identifiers.</summary>
    private sealed class IdReferenceHandler : ReferenceHandler
    {
        /// <inheritdoc />
        public override ReferenceResolver CreateResolver() => new IdReferenceResolver();
    }

    /// <summary>
    /// A reference resolver which serializes objects of the configuration by their id.
    /// </summary>
    private sealed class IdReferenceResolver : ReferenceResolver
    {
        /// <inheritdoc />
        public override void AddReference(string referenceId, object value)
        {
            // Only used for serialization.
        }

        /// <inheritdoc />
        public override string GetReference(object value, out bool alreadyExists)
        {
            if (value is IIdentifiable identifiable)
            {
                alreadyExists = true;
                return identifiable.Id.ToString();
            }

            alreadyExists = false;
            return string.Empty;
        }

        /// <inheritdoc />
        public override object ResolveReference(string referenceId) => throw new NotSupportedException($"This resolver only serializes references, so it can't resolve the reference '{referenceId}'.");
    }
}
