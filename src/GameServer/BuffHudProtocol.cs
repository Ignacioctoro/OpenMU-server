// <copyright file="BuffHudProtocol.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer;

/// <summary>
/// Broyal client extension for buff duration and voluntary cancellation.
/// </summary>
internal static class BuffHudProtocol
{
    public const ushort EffectIdMarker = 0x8000;

    // Positive skill effects in the Season 6 configuration. Hostile effects,
    // consumables and event states must never be removed by this request.
    public static bool CanCancel(short effectId) => effectId is
        1 or 2 or 4 or 5 or 6 or 8
        or 71 or 81 or 82
        or 129 or 130 or 131 or 135 or 138 or 139 or 148 or 153 or 154 or 155;
}
