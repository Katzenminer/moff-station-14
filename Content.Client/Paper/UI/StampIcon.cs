using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Paper.UI;

/// <summary>
///     Displays a per-job rubber stamp mark on a piece of paper, drawn from
///     the stamp overlay sprite sheet rather than as colored text.
/// </summary>
public sealed class StampIcon : TextureRect
{
    private static readonly ProtoId<ShaderPrototype> PaperStamp = "PaperStamp";
    private static readonly ResPath StampsRsiPath = new("/Textures/_Moffstation/Objects/Misc/paper_stamps_ui.rsi");

    private readonly SpriteSystem _spriteSystem;
    private readonly ShaderInstance? _stampShader;

    /// Allows an additional orientation to be applied to this control.
    public float Orientation;

    public string RsiState
    {
        set => Texture = _spriteSystem.Frame0(new SpriteSpecifier.Rsi(StampsRsiPath, value));
    }

    public StampIcon()
    {
        _spriteSystem = IoCManager.Resolve<IEntityManager>().System<SpriteSystem>();

        var prototypes = IoCManager.Resolve<IPrototypeManager>();
        _stampShader = prototypes.Index(PaperStamp).InstanceUnique();

        Stretch = StretchMode.Keep;
        // Source art is 192x96 (2:1 landscape); scale down to a reasonable on-screen stamp size.
        TextureScale = new Vector2(0.75f, 0.75f);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var offset = new Vector2(PixelPosition.X * MathF.Cos(Orientation) - PixelPosition.Y * MathF.Sin(Orientation),
                PixelPosition.Y * MathF.Cos(Orientation) + PixelPosition.X * MathF.Sin(Orientation));

        _stampShader?.SetParameter("objCoord", GlobalPosition * UIScale * new Vector2(1, -1));
        handle.UseShader(_stampShader);
        handle.SetTransform(GlobalPixelPosition - PixelPosition + offset, Orientation, Vector2.One);
        base.Draw(handle);

        // Restore a sane transform+shader
        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(null);
    }
}
