using System.Numerics;
using Content.Client.Movement.Components;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Camera;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Map;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Client.Movement.Systems;

public sealed partial class EyeCursorOffsetSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;

    // Sunrise-Start
    private bool _holdLookUp;
    private bool _toggled;
    // Sunrise-End

    // This value is here to make sure the user doesn't have to move their mouse
    // all the way out to the edge of the screen to get the full offset.
    private readonly float _edgeOffset = 0.9f;

    public override void Initialize()
    {
        base.Initialize();

        // Sunrise-Start
        SubscribeLocalEvent<EyeCursorOffsetComponent, GetEyeOffsetEvent>(OnGetEyeOffsetEvent);
        _cfg.OnValueChanged(SunriseCCVars.HoldLookUp, OnHoldLookUpChanged, true);
        // Sunrise-End
    }

    // Sunrise-Start
    private void OnHoldLookUpChanged(bool val)
    {
        _holdLookUp = val;
        var input = val ? null : InputCmdHandler.FromDelegate(_ => _toggled = !_toggled);
        _inputManager.SetInputCommand(ContentKeyFunctions.LookUp, input);
    }
    // Sunrise-End

    private void OnGetEyeOffsetEvent(EntityUid uid, EyeCursorOffsetComponent component, ref GetEyeOffsetEvent args)
    {
        var offset = OffsetAfterMouse(uid, component);
        if (offset == null)
            return;

        args.Offset += offset.Value;
    }

    public Vector2? OffsetAfterMouse(EntityUid uid, EyeCursorOffsetComponent? component)
    {
        // Sunrise-Start
        if (_holdLookUp)
        {
            if (_inputSystem.CmdStates.GetState(ContentKeyFunctions.LookUp) != BoundKeyState.Down)
            {
                return Vector2.Zero;
            }
        }
        else if (!_toggled)
        {
            return Vector2.Zero;
        }
        // Sunrise-End

        var localPlayer = _player.LocalEntity;
        var mousePos = _inputManager.MouseScreenPosition;
        var screenControl = _eyeManager.MainViewport as Control;
        var screenSize = screenControl?.PixelSize ?? _clyde.MainWindow.Size;
        var minValue = MathF.Min(screenSize.X / 2, screenSize.Y / 2) * _edgeOffset;

        var mouseNormalizedPos = new Vector2(-(mousePos.X - screenSize.X / 2) / minValue, (mousePos.Y - screenSize.Y / 2) / minValue); // X needs to be inverted here for some reason, otherwise it ends up flipped.

        if (localPlayer == null)
            return null;

        if (component == null)
        {
            component = EnsureComp<EyeCursorOffsetComponent>(uid);
        }

        // Doesn't move the offset if the mouse has left the game window!
        if (mousePos.Window != WindowId.Invalid)
        {
            // The offset must account for the in-world rotation.
            var eyeRotation = _eyeManager.CurrentEye.Rotation;
            var mouseActualRelativePos = Vector2.Transform(mouseNormalizedPos, System.Numerics.Quaternion.CreateFromAxisAngle(-System.Numerics.Vector3.UnitZ, (float)(eyeRotation.Opposite().Theta))); // I don't know, it just works.

            // Caps the offset into a circle around the player.
            mouseActualRelativePos *= component.MaxOffset;
            if (mouseActualRelativePos.Length() > component.MaxOffset)
            {
                mouseActualRelativePos = mouseActualRelativePos.Normalized() * component.MaxOffset;
            }

            component.TargetPosition = mouseActualRelativePos;

            //Makes the view not jump immediately when moving the cursor fast.
            if (component.CurrentPosition != component.TargetPosition)
            {
                var vectorOffset = component.TargetPosition - component.CurrentPosition;
                if (vectorOffset.Length() > component.OffsetSpeed)
                {
                    vectorOffset = vectorOffset.Normalized() * component.OffsetSpeed;
                }
                component.CurrentPosition += vectorOffset;
            }
        }
        return component.CurrentPosition;
    }
}
