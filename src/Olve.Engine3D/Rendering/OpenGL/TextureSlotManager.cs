using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Textures;
using Olve.Utilities.Ids;
using Silk.NET.OpenGL;
using Texture = Olve.Engine3D.Rendering.Textures.Texture;

namespace Olve.Engine3D.Rendering.OpenGL;

/// <summary>
/// Manages texture unit binding. Exchanges domain texture IDs for bound texture slots.
/// RenderingId resolution is handled internally - callers only need Id&lt;Texture&gt;.
/// </summary>
public class TextureSlotManager(
    Provider<GL> glProvider,
    TextureRenderingManager textureRenderingManager,
    TextureEntityManager textureEntityManager)
{
    public const uint MaxTextureUnits = 8;

    /// <summary>
    /// Binds multiple textures to consecutive texture units.
    /// </summary>
    public Result<IReadOnlyList<TextureSlot>> BindTextures(IReadOnlyList<Id<Texture>> textureIds)
    {
        if (textureIds.Count > MaxTextureUnits)
            return new ResultProblem("Too many textures ({0}), max is {1}",
                textureIds.Count, MaxTextureUnits);

        var slots = new List<TextureSlot>(textureIds.Count);

        for (uint unit = 0; unit < textureIds.Count; unit++)
        {
            var textureId = textureIds[(int)unit];

            if (BindTextureInternal(textureId, unit).TryPickProblems(out var problems, out var slot))
                return problems;

            slots.Add(slot);
        }

        return slots;
    }

    /// <summary>
    /// Binds a single texture to the specified unit and returns its slot.
    /// </summary>
    public Result<TextureSlot> BindTexture(Id<Texture> textureId, uint unit = 0)
    {
        if (unit >= MaxTextureUnits)
            return new ResultProblem("Texture unit {0} exceeds max {1}", unit, MaxTextureUnits);

        return BindTextureInternal(textureId, unit);
    }

    private Result<TextureSlot> BindTextureInternal(Id<Texture> textureId, uint unit)
    {
        // Resolve: Id<Texture> → RenderingId<Texture>
        if (!textureRenderingManager.TryGetRenderingId(textureId, out var renderingId))
            return new ResultProblem("Texture not registered for rendering: {0}", textureId);

        // Resolve: RenderingId<Texture> → Texture2D (OpenGL handle)
        if (!textureEntityManager.TryGetRegistration(renderingId, out var reg))
            return new ResultProblem("Texture OpenGL registration not found: {0}", renderingId);

        // Bind to texture unit
        glProvider.Value.ActiveTexture(TextureUnit.Texture0 + (int)unit);
        glProvider.Value.BindTexture(TextureTarget.Texture2D, reg.Texture.Handle);

        return new TextureSlot(unit, reg.Texture);
    }

    public void UnbindAll(uint count)
    {
        for (uint unit = 0; unit < count; unit++)
        {
            glProvider.Value.ActiveTexture(TextureUnit.Texture0 + (int)unit);
            glProvider.Value.BindTexture(TextureTarget.Texture2D, 0);
        }
    }
}
