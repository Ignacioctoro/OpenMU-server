// <copyright file="PartyMatchingDirectory.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PartyMatching;

using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using MUnique.OpenMU.GameLogic.PlayerActions.Party;
using Nito.AsyncEx;

/// <summary>A temporary recruitment directory scoped to one game context, never shared across servers.</summary>
public sealed class PartyMatchingDirectory
{
    private const int MaximumAdvertisements = 500;
    private const int MaximumApplicationsPerAdvertisement = 20;
    private const int AdvertisementsPerPage = 8;
    private static readonly TimeSpan RequestInterval = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan ApplicationLifetime = TimeSpan.FromMinutes(2);
    private static readonly ConditionalWeakTable<IGameContext, PartyMatchingDirectory> Directories = new();
    private readonly AsyncLock _gate = new();
    private readonly Dictionary<string, Advertisement> _ads = new();
    private readonly Dictionary<string, Application> _applications = new();
    private readonly ConditionalWeakTable<Player, RequestClock> _clocks = new();

    /// <summary>Returns the directory for the player's GameServer.</summary>
    public static PartyMatchingDirectory For(Player player) => Directories.GetValue(player.GameContext, _ => new PartyMatchingDirectory());

    /// <summary>Executes a request and returns a bounded, password-free snapshot for the requester.</summary>
    public async ValueTask<PartyMatchingSnapshot> ExecuteAsync(Player player, PartyMatchingRequest request)
    {
        using (await this._gate.LockAsync().ConfigureAwait(false))
        {
            var clock = this._clocks.GetValue(player, _ => new RequestClock());
            if (DateTime.UtcNow - clock.LastRequest < RequestInterval)
            {
                return new PartyMatchingSnapshot { Status = 9 };
            }

            clock.LastRequest = DateTime.UtcNow;
            this.Prune();
            int status = 0;
            if (!Eligible(player))
            {
                status = 2;
            }
            else
            {
                status = await this.ApplyAsync(player, request).ConfigureAwait(false);
            }

            return this.Snapshot(player, request, status);
        }
    }

    private static bool Eligible(Player player) => player.IsConnected && player.SelectedCharacter != null &&
        player.PlayerState.CurrentState == PlayerState.EnteredWorld && player.CurrentMiniGame?.Definition.AllowParty != false;

    private static bool Alive(Player player, string name) => player.IsConnected && player.SelectedCharacter != null && player.Name == name;

    private static int ClassOf(Player player) => (player.SelectedCharacter?.CharacterClass?.Number ?? 255) >> 4;

    private static bool Meets(Advertisement ad, Player applicant) => applicant.Level >= ad.Min && applicant.Level <= ad.Max &&
        ClassOf(applicant) < 7 && (ad.Classes & (1 << ClassOf(applicant))) != 0;

    private static byte[] Hash(string password) => SHA256.HashData(Encoding.UTF8.GetBytes(password));

    private async ValueTask<int> ApplyAsync(Player player, PartyMatchingRequest request)
    {
        switch (request.Operation)
        {
            case 0: return 0;
            case 1:
                if (player.Party != null && player.Party.PartyMaster != player) return 3;
                if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 40 || request.Title.Any(char.IsControl) ||
                    request.Password.Length > 16 || request.MinLevel < 1 || request.MaxLevel < request.MinLevel ||
                    request.MaxLevel > player.GameContext.Configuration.MaximumLevel || request.ClassMask is < 1 or > 127) return 4;
                if (this._ads.Count >= MaximumAdvertisements && !this._ads.Values.Any(a => a.Leader == player)) return 9;
                this.RemoveOwned(player);
                var ad = new Advertisement
                {
                    Leader = player, LeaderName = player.Name, Party = player.Party,
                    Title = request.Title.Trim(), Min = request.MinLevel, Max = request.MaxLevel,
                    Classes = request.ClassMask, Auto = request.AutoAccept,
                    Password = request.Password.Length == 0 ? null : Hash(request.Password),
                };
                this._ads.Add(ad.Id, ad);
                return 1;
            case 2:
                this.RemoveOwned(player);
                return 1;
            case 3:
                if (player.Party != null || this._applications.Values.Any(a => a.Player == player)) return 5;
                if (!this._ads.TryGetValue(request.Id, out var target) || target.Leader == player) return 6;
                if (!Meets(target, player)) return 7;
                if (target.Password != null && !CryptographicOperations.FixedTimeEquals(target.Password, Hash(request.Password))) return 8;
                if (target.Leader.Party?.PartyList.Count >= player.GameContext.Configuration.MaximumPartySize) return 5;
                if (target.Auto) return await this.JoinAsync(target, player).ConfigureAwait(false);
                if (this._applications.Values.Count(a => a.Ad == target) >= MaximumApplicationsPerAdvertisement) return 9;
                var application = new Application { Ad = target, Player = player, Name = player.Name };
                this._applications.Add(application.Id, application);
                return 10;
            case 4:
                foreach (var key in this._applications.Where(a => a.Value.Player == player).Select(a => a.Key).ToArray()) this._applications.Remove(key);
                return 1;
            case 5:
            case 6:
                if (!this._applications.TryGetValue(request.Id, out var pending) || pending.Ad.Leader != player) return 6;
                this._applications.Remove(request.Id);
                return request.Operation == 6 ? 1 : await this.JoinAsync(pending.Ad, pending.Player).ConfigureAwait(false);
            default: return 4;
        }
    }

    private async ValueTask<int> JoinAsync(Advertisement ad, Player applicant)
    {
        var leader = ad.Leader;
        if (!Eligible(leader) || !Eligible(applicant) || applicant.Party != null || applicant.LastPartyRequester != null ||
            !Alive(leader, ad.LeaderName) || !Meets(ad, applicant) || leader.GameContext != applicant.GameContext ||
            (leader.Party != null && leader.Party.PartyMaster != leader)) return 2;
        if (leader.Party?.PartyList.Count >= leader.GameContext.Configuration.MaximumPartySize) return 5;
        // PartyResponseAction is the stable admission entry point in both the
        // published branch and local builds with additional party-rule plugins.
        // Reserve both players against incoming trade/party requests while admitting.
        // The directory gate serializes matching joins; normal party rules are also
        // checked again by PartyResponseAction, and Party.AddAsync enforces capacity.
        if (!Eligible(leader) || !Eligible(applicant) || leader.LastPartyRequester != null || applicant.LastPartyRequester != null) return 2;
        if (!await leader.PlayerState.TryAdvanceToAsync(PlayerState.PartyRequest).ConfigureAwait(false)) return 2;
        bool reservedApplicant = false;
        try
        {
            if (applicant.Party != null || (leader.Party != null && leader.Party.PartyMaster != leader)) return 2;
            reservedApplicant = await applicant.PlayerState.TryAdvanceToAsync(PlayerState.PartyRequest).ConfigureAwait(false);
            if (!reservedApplicant) return 2;
            applicant.LastPartyRequester = leader;
            await new PartyResponseAction().HandleResponseAsync(applicant, true).ConfigureAwait(false);
        }
        finally
        {
            if (reservedApplicant) applicant.LastPartyRequester = null;
            if (reservedApplicant && applicant.PlayerState.CurrentState == PlayerState.PartyRequest)
                await applicant.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
            if (leader.PlayerState.CurrentState == PlayerState.PartyRequest)
                await leader.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        }

        if (applicant.Party == null || applicant.Party != leader.Party) return 5;
        ad.Party = leader.Party;
        this.RemoveOwned(applicant);
        foreach (var key in this._applications.Where(a => a.Value.Player == applicant).Select(a => a.Key).ToArray()) this._applications.Remove(key);
        return 11;
    }

    private void RemoveOwned(Player player)
    {
        foreach (var key in this._ads.Where(a => a.Value.Leader == player).Select(a => a.Key).ToArray()) this._ads.Remove(key);
        foreach (var key in this._applications.Where(a => !this._ads.ContainsKey(a.Value.Ad.Id)).Select(a => a.Key).ToArray()) this._applications.Remove(key);
    }

    private void Prune()
    {
        foreach (var ad in this._ads.Values.ToArray())
        {
            if (!Alive(ad.Leader, ad.LeaderName) || (ad.Party != null && ad.Leader.Party != ad.Party) ||
                (ad.Leader.Party != null && ad.Leader.Party.PartyMaster != ad.Leader)) this.RemoveOwned(ad.Leader);
            else ad.Party = ad.Leader.Party;
        }

        foreach (var app in this._applications.Values.ToArray())
            if (!this._ads.ContainsKey(app.Ad.Id) || !Alive(app.Player, app.Name) || app.Player.Party != null ||
                DateTime.UtcNow - app.Created > ApplicationLifetime) this._applications.Remove(app.Id);
    }

    private PartyMatchingSnapshot Snapshot(Player player, PartyMatchingRequest request, int status)
    {
        string search = request.Search ?? string.Empty;
        var filtered = this._ads.Values.Where(a => a.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            a.LeaderName.Contains(search, StringComparison.OrdinalIgnoreCase)).OrderBy(a => a.LeaderName).ToArray();
        int pages = Math.Max(1, (filtered.Length + AdvertisementsPerPage - 1) / AdvertisementsPerPage), page = Math.Clamp(request.Page, 0, pages - 1);
        return new PartyMatchingSnapshot
        {
            Status = status, Page = page, Pages = pages, MaximumLevel = player.GameContext.Configuration.MaximumLevel,
            OwnAd = this._ads.Values.FirstOrDefault(a => a.Leader == player)?.Id ?? string.Empty,
            PendingAd = this._applications.Values.FirstOrDefault(a => a.Player == player)?.Ad.Id ?? string.Empty,
            Ads = filtered.Skip(page * AdvertisementsPerPage).Take(AdvertisementsPerPage).Select(a => new PartyMatchingAd
            {
                Id = a.Id, Leader = a.LeaderName, Title = a.Title, Map = a.Leader.CurrentMap?.Definition.Number ?? 0,
                Count = a.Leader.Party?.PartyList.Count ?? 1, Capacity = player.GameContext.Configuration.MaximumPartySize,
                MinLevel = a.Min, MaxLevel = a.Max, ClassMask = a.Classes, Locked = a.Password != null, AutoAccept = a.Auto,
            }).ToArray(),
            Requests = this._applications.Values.Where(a => a.Ad.Leader == player).Select(a => new PartyMatchingApplicant
            { Id = a.Id, Name = a.Name, Level = a.Player.Level, Class = ClassOf(a.Player) }).ToArray(),
        };
    }

    private sealed class RequestClock
    {
        public DateTime LastRequest { get; set; }
    }

    private sealed class Advertisement
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public Player Leader { get; set; } = null!;
        public string LeaderName { get; set; } = string.Empty;
        public Party? Party { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Min { get; set; }
        public int Max { get; set; }
        public int Classes { get; set; }
        public bool Auto { get; set; }
        public byte[]? Password { get; set; }
    }

    private sealed class Application
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public Advertisement Ad { get; set; } = null!;
        public Player Player { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public DateTime Created { get; } = DateTime.UtcNow;
    }
}
