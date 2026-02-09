using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Textures;
using Olve.Engine3D.Utilities;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class TextureSlotManager(
    Provider<GL> glProvider,
    TextureEntityManager textureEntityManager)
{
    public const int MaxTextureUnits = 8;
    private readonly RotatingIndex _slotIndex = new(MaxTextureUnits);

    public Result<IReadOnlyList<TextureSlot>> BindTextures(IReadOnlyList<UntypedTextureId> textureIds)
    {
        if (textureIds.Count > MaxTextureUnits)
        {
            return new ResultProblem("Too many textures ({0}), max is {1}", textureIds.Count, MaxTextureUnits);
        }

        var slots = new List<TextureSlot>(textureIds.Count);

        var textureIdsWithSlotIndices = textureIds.Zip(_slotIndex.GetMultiple(textureIds.Count));
        foreach (var textureIdAndSlotIndex in textureIdsWithSlotIndices)
        {
            var (textureId, slotIndex) = textureIdAndSlotIndex;

            if (BindTextureInternal(textureId, slotIndex).TryPickProblems(out var problems, out var slot))
                return problems;

            slots.Add(slot);
        }

        return slots;
    }

    public Result<TextureSlot> BindTexture(UntypedTextureId textureId) => BindTextureInternal(textureId, _slotIndex.GetNext());

    private Result<TextureSlot> BindTextureInternal(UntypedTextureId textureId, int slotIndex)
    {
        if (!textureEntityManager.TryGetRegistration(textureId, out var openGlTexture))
        {
            return new ResultProblem("Texture OpenGL registration not found: {0}", textureId);
        }

        glProvider.Value.ActiveTexture(TextureUnit.Texture0 + slotIndex);
        glProvider.Value.BindTexture(TextureTarget.Texture2D, openGlTexture.Handle);

        return new TextureSlot(slotIndex, openGlTexture);
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
