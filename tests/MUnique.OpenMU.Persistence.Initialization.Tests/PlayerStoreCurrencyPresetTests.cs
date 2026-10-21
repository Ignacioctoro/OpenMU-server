// <copyright file="PlayerStoreCurrencyPresetTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.PlayerShopCurrencies;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>Checks fresh, missing and repeated application of the deployment's currency policy.</summary>
[TestFixture]
internal class PlayerStoreCurrencyPresetTests
{
    /// <summary>New Season 6 configurations enable only WCoin C and the five jewel types.</summary>
    [Test]
    public async Task FreshConfigurationUsesPresetAsync()
    {
        var (context, configuration) = await CreateAsync().ConfigureAwait(false);
        using var ownedContext = context;
        AssertPolicy(configuration);
    }

    /// <summary>The update repairs missing configuration, disables point awards, and is idempotent.</summary>
    [Test]
    public async Task UpdateRepairsMissingConfigurationAndCanBeRepeatedAsync()
    {
        var (context, configuration) = await CreateAsync().ConfigureAwait(false);
        using var ownedContext = context;
        var previous = configuration.PlugInConfigurations.Single(p => p.TypeId == typeof(PlayerShopCurrenciesFeaturePlugIn).GUID);
        configuration.PlugInConfigurations.Remove(previous);
        var award = configuration.PlugInConfigurations.Single(p => p.TypeId == typeof(GoblinPointsForPlayTimePlugIn).GUID);
        award.IsActive = true;
        var update = new EnableJewelAndWCoinCStoresUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        AssertPolicy(configuration);
        Assert.That(award.IsActive, Is.False);
        var saved = configuration.PlugInConfigurations.Single(p => p.TypeId == typeof(PlayerShopCurrenciesFeaturePlugIn).GUID).CustomConfiguration;
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        AssertPolicy(configuration);
        Assert.That(configuration.PlugInConfigurations.Single(p => p.TypeId == typeof(PlayerShopCurrenciesFeaturePlugIn).GUID).CustomConfiguration, Is.EqualTo(saved));
    }

    private static void AssertPolicy(GameConfiguration configuration)
    {
        var plugin = configuration.PlugInConfigurations.Single(p => p.TypeId == typeof(PlayerShopCurrenciesFeaturePlugIn).GUID);
        Assert.That(plugin.IsActive, Is.True);
        using var json = JsonDocument.Parse(plugin.CustomConfiguration!);
        Assert.That(json.RootElement.GetProperty("IsWCoinCAllowed").GetBoolean(), Is.True);
        Assert.That(json.RootElement.GetProperty("IsWCoinPAllowed").GetBoolean(), Is.False);
        Assert.That(json.RootElement.GetProperty("IsGoblinPointsAllowed").GetBoolean(), Is.False);
        ItemIdentifier[] jewels = [ItemConstants.JewelOfBless, ItemConstants.JewelOfSoul, ItemConstants.JewelOfChaos, ItemConstants.JewelOfCreation, ItemConstants.JewelOfLife];
        foreach (var jewel in jewels)
        {
            var definition = configuration.Items.Single(i => i.Group == jewel.Group && i.Number == jewel.Number);
            Assert.That(plugin.CustomConfiguration, Does.Contain(definition.GetId().ToString()));
        }
    }

    private static async Task<(IContext Context, GameConfiguration Configuration)> CreateAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        await new VersionSeasonSix.DataInitialization(provider, new NullLoggerFactory()).CreateInitialDataAsync(1, true).ConfigureAwait(false);
        var context = provider.CreateNewContext();
        return (context, (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First());
    }
}
