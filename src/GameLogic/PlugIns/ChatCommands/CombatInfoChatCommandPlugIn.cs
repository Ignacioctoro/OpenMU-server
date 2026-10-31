// <copyright file="CombatInfoChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands.Arguments;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Read-only diagnostics for live ammunition and Force configuration.
/// </summary>
[Guid("56B5E384-1F43-4D52-A548-ECB074DA9427")]
[PlugIn]
[Display(Name = "Combat information", Description = "Shows live ammunition and Force settings without changing combat state.")]
[ChatCommandHelp(Command, typeof(CharInfoChatCommandArgs), CharacterStatus.GameMaster)]
public class CombatInfoChatCommandPlugIn : ChatCommandPlugInBase<CharInfoChatCommandArgs>
{
    private const string Command = "/combatinfo";

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => CharacterStatus.GameMaster;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player gameMaster, CharInfoChatCommandArgs arguments)
    {
        Player? player = gameMaster;
        if (!string.IsNullOrWhiteSpace(arguments.CharacterName))
        {
            player = await this.GetPlayerByCharacterNameAsync(gameMaster, arguments.CharacterName).ConfigureAwait(false);
        }

        if (player?.Attributes is not { } attributes || player.SelectedCharacter is null)
        {
            return;
        }

        await ReportAsync($"COMBAT {player.Name} class={player.SelectedCharacter.CharacterClass?.Number}").ConfigureAwait(false);
        await ReportAsync(FormattableString.Invariant($"AMMO rate={attributes[Stats.AmmunitionConsumptionRate]:0.###} amount={attributes[Stats.AmmunitionAmount]:0.###}")).ConfigureAwait(false);
        var ammo = player.Inventory?.EquippedAmmunitionItem;
        await ReportAsync(ammo is null ? "AMMO inventory=none" : FormattableString.Invariant($"AMMO inventory={ammo.Durability:0.###} item={ammo.Definition?.Group}:{ammo.Definition?.Number}")).ConfigureAwait(false);
        await ReportAsync($"AMMO InfinityArrow={player.MagicEffectList.ActiveEffects.ContainsKey(6)}").ConfigureAwait(false);

        if (player.Inventory is { } inventory)
        {
            foreach (var item in inventory.EquippedItems.Where(candidate => candidate.ItemSlot is 0 or 1))
            {
                var definition = item.Definition;
                await ReportAsync($"HAND {item.ItemSlot} item={definition?.Group}:{definition?.Number} {definition?.Name.GetTranslation(player.Culture)}").ConfigureAwait(false);
                var rates = definition?.BasePowerUpAttributes.Where(powerUp => powerUp.TargetAttribute?.Id == Stats.AmmunitionConsumptionRate.Id).ToList();
                if (rates is not { Count: > 0 })
                {
                    await ReportAsync($"HAND {item.ItemSlot} no base ammo rate").ConfigureAwait(false);
                    continue;
                }

                foreach (var rate in rates)
                {
                    await ReportAsync(FormattableString.Invariant($"HAND {item.ItemSlot} ammoBase={rate.BaseValue:0.###} mode={rate.AggregateType}")).ConfigureAwait(false);
                }
            }
        }

        foreach (ushort id in new ushort[] { 60, 66, 509 })
        {
            var entry = player.SkillList?.GetSkill(id);
            var skill = entry?.Skill ?? player.GameContext.Configuration.Skills.FirstOrDefault(candidate => candidate.Number == id);
            if (skill is null)
            {
                await ReportAsync($"FORCE {id} missing from configuration").ConfigureAwait(false);
                continue;
            }

            await ReportAsync($"FORCE {id} available={entry != null} type={skill.SkillType}").ConfigureAwait(false);
            await ReportAsync($"FORCE {id} damage={skill.DamageType} base={skill.AttackDamage} hits={skill.NumberOfHitsPerAttack}").ConfigureAwait(false);
            await ReportAsync($"FORCE {id} target={skill.Target} restriction={skill.TargetRestriction}").ConfigureAwait(false);
        }

        await ReportAsync(FormattableString.Invariant($"MP={attributes[Stats.CurrentMana]:0.###} safezone={player.IsAtSafezone()}")).ConfigureAwait(false);

        async ValueTask ReportAsync(string line)
        {
            // Preserve full details in server logs; chat packets have limited space.
            gameMaster.Logger.LogInformation("COMBATINFO {Character}: {Details}", player?.Name, line);
            await gameMaster.ShowBlueMessageAsync(line.Length <= 72 ? line : line[..72]).ConfigureAwait(false);
        }
    }
}
