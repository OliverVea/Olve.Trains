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

    public Result<TextureSlot> BindTexture(Id<Texture> textureId)
    {
        return BindTextureInternal(textureId, 0);
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
