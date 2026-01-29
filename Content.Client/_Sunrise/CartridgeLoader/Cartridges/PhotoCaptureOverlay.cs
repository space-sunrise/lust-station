using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Enums;
using Content.Client.Viewport;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.CartridgeLoader.Cartridges;

public sealed class PhotoCaptureOverlay : Overlay
{
    private readonly IPlayerManager _playerManager;
    private readonly SharedTransformSystem _transformSystem;
    private readonly IStateManager _stateManager;
    private readonly IEyeManager _eyeManager;
    private readonly IEntityManager _entityManager;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public PhotoCaptureOverlay(IPlayerManager playerManager, SharedTransformSystem transformSystem, IStateManager stateManager, IEyeManager eyeManager, IEntityManager entityManager)
    {
        _playerManager = playerManager;
        _transformSystem = transformSystem;
        _stateManager = stateManager;
        _eyeManager = eyeManager;
        _entityManager = entityManager;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_stateManager.CurrentState is not IMainViewportState viewportState)
            return;

        if (args.Viewport.Eye != _eyeManager.CurrentEye)
            return;

        var screenHandle = args.ScreenHandle;
        var viewportControl = (Control)viewportState.Viewport;

        if (_playerManager.LocalPlayer?.ControlledEntity is not { } player)
            return;

        var playerPos = _transformSystem.GetWorldPosition(player);

        var system = _entityManager.System<PhotoCartridgeClientSystem>();
        var targetPos = system.GetCameraPosition(player, system.CaptureDistance);

        var targetScreen = _eyeManager.WorldToScreen(targetPos);
        var playerScreen = _eyeManager.WorldToScreen(playerPos);
        var offsetScreen = _eyeManager.WorldToScreen(playerPos + new Vector2(1, 0));
        var pixelsPerMeter = (offsetScreen - playerScreen).Length();

        if (pixelsPerMeter < 1) return;

        var halfSize = (3.0f * pixelsPerMeter) / 2.0f;
        var rect = new UIBox2(targetScreen.X - halfSize, targetScreen.Y - halfSize, targetScreen.X + halfSize, targetScreen.Y + halfSize);

        var vpRect = viewportControl.GlobalPixelRect;
        var color = Color.Black.WithAlpha(0.5f);

        screenHandle.DrawRect(new UIBox2(vpRect.Left, vpRect.Top, vpRect.Right, rect.Top), color);
        screenHandle.DrawRect(new UIBox2(vpRect.Left, rect.Bottom, vpRect.Right, vpRect.Bottom), color);
        screenHandle.DrawRect(new UIBox2(vpRect.Left, rect.Top, rect.Left, rect.Bottom), color);
        screenHandle.DrawRect(new UIBox2(rect.Right, rect.Top, vpRect.Right, rect.Bottom), color);

        screenHandle.DrawRect(rect, Color.Red.WithAlpha(0.3f), false);
        var borderThickness = 2f;
        screenHandle.DrawRect(new UIBox2(rect.Left, rect.Top, rect.Right, rect.Top + borderThickness), Color.Red);
        screenHandle.DrawRect(new UIBox2(rect.Left, rect.Bottom - borderThickness, rect.Right, rect.Bottom), Color.Red);
        screenHandle.DrawRect(new UIBox2(rect.Left, rect.Top, rect.Left + borderThickness, rect.Bottom), Color.Red);
        screenHandle.DrawRect(new UIBox2(rect.Right - borderThickness, rect.Top, rect.Right, rect.Bottom), Color.Red);
    }
}
