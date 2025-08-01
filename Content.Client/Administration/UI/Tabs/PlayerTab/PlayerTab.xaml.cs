using System.Linq;
using Content.Client._Sunrise.AntagObjectives;
using Content.Client.Administration.Systems;
using Content.Shared.Administration; // Sunrise-Edit
using Content.Shared.Administration.Managers; // Sunrise-Edit
using Content.Client.Administration.UI.AntagObjectives;
using Content.Client.UserInterface.Controls;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Configuration;
using static Content.Client.Administration.UI.Tabs.PlayerTab.PlayerTabHeader;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.Administration.UI.Tabs.PlayerTab;

[GenerateTypedNameReferences]
public sealed partial class PlayerTab : Control
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly ISharedAdminManager _adminManager = default!; // Sunrise-Edit

    private const string ArrowUp = "↑";
    private const string ArrowDown = "↓";
    private readonly Color _altColor = Color.FromHex("#292B38");
    private readonly Color _defaultColor = Color.FromHex("#2F2F3B");
    private readonly AdminSystem _adminSystem;
    private IReadOnlyList<PlayerInfo> _players = new List<PlayerInfo>();

    private Header _headerClicked = Header.Username;
    private bool _ascending = true;
    private bool _showDisconnected;

    private AdminPlayerTabColorOption _playerTabColorSetting;
    private AdminPlayerTabRoleTypeOption _playerTabRoleSetting;
    private AdminPlayerTabSymbolOption _playerTabSymbolSetting;

    private readonly AntagObjectivesUIController _antagObjectivesUIController; // Sunrise-Edit

    public event Action<GUIBoundKeyEventArgs, ListData>? OnEntryKeyBindDown;

    public PlayerTab()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);

        _adminSystem = _entManager.System<AdminSystem>();
        _adminSystem.PlayerListChanged += RefreshPlayerList;
        _adminSystem.OverlayEnabled += OverlayEnabled;
        _adminSystem.OverlayDisabled += OverlayDisabled;

        _config.OnValueChanged(CCVars.AdminPlayerTabRoleSetting, RoleSettingChanged, true);
        _config.OnValueChanged(CCVars.AdminPlayerTabColorSetting, ColorSettingChanged, true);
        _config.OnValueChanged(CCVars.AdminPlayerTabSymbolSetting, SymbolSettingChanged, true);


        OverlayButton.OnPressed += OverlayButtonPressed;
        ShowDisconnectedButton.OnPressed += ShowDisconnectedPressed;

        ListHeader.BackgroundColorPanel.PanelOverride = new StyleBoxFlat(_altColor);
        ListHeader.OnHeaderClicked += HeaderClicked;

        SearchList.SearchBar = SearchLineEdit;
        SearchList.GenerateItem += GenerateButton;
        SearchList.DataFilterCondition += DataFilterCondition;
        SearchList.ItemKeyBindDown += (args, data) => OnEntryKeyBindDown?.Invoke(args, data);

        RefreshPlayerList(_adminSystem.PlayerList);

        _antagObjectivesUIController = UserInterfaceManager.GetUIController<AntagObjectivesUIController>(); // Sunrise-Edit
    }

    #region Antag Overlay

    private void OverlayEnabled()
    {
        OverlayButton.Pressed = true;
    }

    private void OverlayDisabled()
    {
        OverlayButton.Pressed = false;
    }

    private void OverlayButtonPressed(ButtonEventArgs args)
    {
        // Sunrise-Start
        if (_playerMan.LocalEntity is not { } playerUid
        || !_adminManager.HasAdminFlag(playerUid, AdminFlags.Moderator))
            return;
        // Sunrise-End
        if (args.Button.Pressed)
        {
            _adminSystem.AdminOverlayOn();
        }
        else
        {
            _adminSystem.AdminOverlayOff();
        }
    }

    #endregion

    private void ShowDisconnectedPressed(ButtonEventArgs args)
    {
        _showDisconnected = args.Button.Pressed;
        RefreshPlayerList(_players);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _adminSystem.PlayerListChanged -= RefreshPlayerList;
            _adminSystem.OverlayEnabled -= OverlayEnabled;
            _adminSystem.OverlayDisabled -= OverlayDisabled;

            OverlayButton.OnPressed -= OverlayButtonPressed;

            ListHeader.OnHeaderClicked -= HeaderClicked;
        }
    }

    #region ListContainer

    private void RoleSettingChanged(string s)
    {
        if (!Enum.TryParse<AdminPlayerTabRoleTypeOption>(s, out var format))
            format = AdminPlayerTabRoleTypeOption.Subtype;
        _playerTabRoleSetting = format;
        RefreshPlayerList(_adminSystem.PlayerList);
    }

    private void ColorSettingChanged(string s)
    {
        if (!Enum.TryParse<AdminPlayerTabColorOption>(s, out var format))
            format = AdminPlayerTabColorOption.Both;
        _playerTabColorSetting = format;
        RefreshPlayerList(_adminSystem.PlayerList);
    }

    private void SymbolSettingChanged(string s)
    {
        if (!Enum.TryParse<AdminPlayerTabSymbolOption>(s, out var format))
            format = AdminPlayerTabSymbolOption.Specific;
        _playerTabSymbolSetting = format;
        RefreshPlayerList(_adminSystem.PlayerList);
    }

    private void RefreshPlayerList(IReadOnlyList<PlayerInfo> players)
    {
        // Sunrise-Start
        if (_playerMan.LocalEntity is not { } playerUid
        || !_adminManager.HasAdminFlag(playerUid, AdminFlags.Moderator))
            return;
        // Sunrise-End
        _players = players;
        PlayerCount.Text = Loc.GetString("player-tab-player-count", ("count", _playerMan.PlayerCount));

        var filteredPlayers = players.Where(info => _showDisconnected || info.Connected).ToList();

        var sortedPlayers = new List<PlayerInfo>(filteredPlayers);
        sortedPlayers.Sort(Compare);

        UpdateHeaderSymbols();

        // Sunrise-Sponsors-Start
        var antagCount = 0;
        var sponsorCount = 0;
        foreach (var player in sortedPlayers)
        {
            if (!_showDisconnected && !player.Connected)
                continue;

            if (player.Antag)
                antagCount += 1;

            if (player.IsSponsor)
                sponsorCount += 1;
        }

        SponsorCount.Text = Loc.GetString("player-tab-sponsor-count", ("count", sponsorCount));
        AntagCount.Text = Loc.GetString("player-tab-antag-count", ("count", antagCount));
        // Sunrise-Sponsors-End

        SearchList.PopulateList(sortedPlayers.Select(info => new PlayerListData(info,
                $"{info.Username} {info.CharacterName} {info.IdentityName} {info.StartingJob}"))
            .ToList());
    }

    private void GenerateButton(ListData data, ListContainerButton button)
    {
        if (data is not PlayerListData { Info: var player})
            return;

        var entry = new PlayerTabEntry(
            player,
            new StyleBoxFlat(button.Index % 2 == 0 ? _altColor : _defaultColor),
            _playerTabColorSetting,
            _playerTabRoleSetting,
            _playerTabSymbolSetting);
        entry.OnObjectives += GetObjectives; // Sunrise-Edit
        button.AddChild(entry);
        button.ToolTip = $"{player.Username}, {player.CharacterName}, {player.IdentityName}, {player.StartingJob}";
    }

    // Sunrise-Start
    private void GetObjectives(NetEntity? nent)
    {
        if (nent == null)
            return;

        _entManager.System<AntagObjectivesSystem>().RequestAntagObjectives(nent.Value);
        _antagObjectivesUIController.OpenWindow();
    }
    // Sunrise-End

    /// <summary>
    /// Determines whether <paramref name="filter"/> is contained in <paramref name="listData"/>.FilteringString.
    /// If all characters are lowercase, the comparison ignores case.
    /// If there is an uppercase character, the comparison is case sensitive.
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="listData"></param>
    /// <returns>Whether <paramref name="filter"/> is contained in <paramref name="listData"/>.FilteringString.</returns>
    private bool DataFilterCondition(string filter, ListData listData)
    {
        if (listData is not PlayerListData {Info: var info, FilteringString: var playerString})
            return false;

        if (!_showDisconnected && !info.Connected)
            return false;

        if (IsAllLower(filter))
        {
            if (!playerString.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
                return false;
        }
        else
        {
            if (!playerString.Contains(filter))
                return false;
        }

        return true;
    }

    private bool IsAllLower(string input)
    {
        foreach (var c in input)
        {
            if (char.IsLetter(c) && !char.IsLower(c))
                return false;
        }

        return true;
    }

    #endregion

    #region Header

    private void UpdateHeaderSymbols()
    {
        ListHeader.ResetHeaderText();
        ListHeader.GetHeader(_headerClicked).Text += $" {(_ascending ? ArrowUp : ArrowDown)}";
    }

    private int Compare(PlayerInfo x, PlayerInfo y)
    {
        if (!_ascending)
        {
            (x, y) = (y, x);
        }

        return _headerClicked switch
        {
            Header.Username => Compare(x.Username, y.Username),
            Header.Character => Compare(x.CharacterName, y.CharacterName),
            Header.Job => Compare(x.StartingJob, y.StartingJob),
            Header.Sponsor => string.Compare(x.SponsorTitle!, y.SponsorTitle, StringComparison.Ordinal), // Sunrise-Sponsors
            Header.RoleType => y.SortWeight - x.SortWeight,
            Header.Playtime => TimeSpan.Compare(x.OverallPlaytime ?? default, y.OverallPlaytime ?? default),
            _ => 1
        };
    }

    private int Compare(string x, string y)
    {
        return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
    }

    private void HeaderClicked(Header header)
    {
        if (_headerClicked == header)
        {
            _ascending = !_ascending;
        }
        else
        {
            _headerClicked = header;
            _ascending = true;
        }

        RefreshPlayerList(_adminSystem.PlayerList);
    }

    #endregion
}

public record PlayerListData(PlayerInfo Info, string FilteringString) : ListData;
