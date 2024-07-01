using Robust.Shared.Random;
using System.Linq;
using System.Numerics;
using Content.Shared.GameTicking;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    // Sunrise-Start
    [ViewVariables]
    public string? LobbyParalax { get; private set; }

    [ViewVariables]
    private readonly List<string> _lobbyParalaxes =
    [
        "AspidParallax",
        "LighthouseStation",
        "AngleStation",
        "FastSpace",
        "Default",
        "BagelStation",
        "KettleStation",
        "AvriteStation",
        "DeltaStation",
        "TortugaStation",
        "ShipwreckedTurbulence1",
        "PebbleStation",
        "OutpostStation",
        "TrainStation",
        "CoreStation",
        // Яркие паралаксы, выглядят прикольно но кому-то мешают.
        //"Grass",
        //"SillyIsland",
        //"PilgrimAiur"
    ];

    [ViewVariables] private LobbyImage? LobbyImage { get; set; }

    [ViewVariables]
    private readonly List<LobbyImage> _lobbyImages = new ()
    {
        new LobbyImage(){Path = "_Lust/Parallax/silicons.rsi", State = "silicon_sit", Scale = new Vector2(10f, 10f)}
    };

    private void InitializeLobbyBackground()
    {
        RandomizeLobbyParalax();
        RandomizeLobbyImage();
    }

    private void RandomizeLobbyParalax() {
        LobbyParalax = _lobbyParalaxes.Any() ? _robustRandom.Pick(_lobbyParalaxes) : null;
    }

    private void RandomizeLobbyImage() {
        LobbyImage = _lobbyImages.Any() ? _robustRandom.Pick(_lobbyImages) : null;
    }
    // Sunrise-End
}
