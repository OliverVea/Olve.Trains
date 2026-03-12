using Olve.Engine3D.Rendering;
using Olve.Engine3D.Rendering.Instancing;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.Geometry;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Utilities;
using Olve.Results;
using Silk.NET.OpenGL;

namespace Olve.Trains.RenderPassPrototype;

/// <summary>
/// Sketch of the modified RenderingManager with multi-pass support.
/// Shows how the existing flat group loop becomes a nested pass→group loop.
///
/// Changes from the current RenderingManager:
/// - Constructor gains RenderPassManager, FramebufferManager, ScreenPass
/// - RenderAll() outer loop iterates ordered passes
/// - Framebuffer bind + clear before each pass's group loop
/// - Screen blit after all passes
/// - Inner group loop is nearly identical to the current implementation
/// </summary>
public class RenderingManagerSketch(
    Provider<GL> glProvider,
    OpenGLShaderManager openGLShaderManager,
    ShaderEntityManager shaderEntityManager,
    GeometryManager geometryManager,
    RenderingGroupManager groupManager,
    RenderPassManager passManager,
    FramebufferManager framebufferManager,
    ScreenPass screenPass)
{
    private const int ErrorCounterThreshold = 20;
    private int _errorCounter;

    public Result RenderAll()
    {
        var gl = glProvider.Value;

        // ── Pass loop (new) ──────────────────────────────────────────────
        foreach (var pass in passManager.GetOrderedPasses())
        {
            BindFramebuffer(gl, pass.FramebufferId);
            pass.Clear.Apply(gl);

            // ── Group loop (same as current, but scoped to this pass) ────
            foreach (var group in groupManager.GetGroupsForPass(pass.PassId))
            {
                // In real engine: group would be GroupData, not GroupInfo.
                // The inner loop body is identical to current RenderingManager —
                // dirty check, instance count skip, shader load, state apply, draw.
                //
                // Sketch only — real impl uses GroupData with VAO/VBO/instances.

                group.RenderState.Apply(gl);

                var shader = group.Shader;
                if (shader.RenderingId == default) continue;

                if (shaderEntityManager.GetRegistration(shader.RenderingId)
                    .TryPickProblems(out var problems, out var shaderRegistration))
                {
                    return problems.Prepend("Failed to get shader registration for '{0}'", shader.ShaderData.Name);
                }

                var shaderParameters = shader.MakeParameters();
                if (openGLShaderManager.LoadShaderInOpenGL(shaderRegistration.ShaderProgram, shaderParameters)
                    .TryPickProblems(out problems))
                {
                    return problems.Prepend("Failed to load shader '{0}'", shader.ShaderData.Name);
                }

                if (group.GroupParameters is { } groupParams)
                {
                    if (openGLShaderManager.ApplyParameters(shaderRegistration.ShaderProgram, groupParams.ToRenderingParameters())
                        .TryPickProblems(out problems))
                    {
                        return problems.Prepend("Failed to apply group parameters for shader '{0}'", shader.ShaderData.Name);
                    }
                }

                // ... draw call (elided — identical to current, uses geometryManager) ...
                _ = geometryManager; // used in real impl for geometry lookup + draw

                if (group.GroupParameters is not null)
                {
                    if (openGLShaderManager.ApplyParameters(shaderRegistration.ShaderProgram, shaderParameters)
                        .TryPickProblems(out problems))
                    {
                        return problems.Prepend("Failed to restore shader parameters for shader '{0}'", shader.ShaderData.Name);
                    }
                }
            }
        }

        // ── Screen blit (new) ────────────────────────────────────────────
        if (screenPass.Source is { } sourceTexture)
        {
            BlitToScreen(gl, sourceTexture);
        }

        // ── Final state reset + error check (same as current) ────────────
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        gl.DepthMask(true);
        gl.Disable(GLEnum.Blend);

        _errorCounter++;
        if (_errorCounter >= ErrorCounterThreshold)
        {
            var error = gl.GetError();
            if (error != GLEnum.NoError)
                return new ResultProblem("OpenGL error: {0}", error);

            _errorCounter = 0;
        }

        return Result.Success();
    }

    // ── New methods (would live on FramebufferManager or a GL helper) ────

    private void BindFramebuffer(GL gl, Olve.Utilities.Ids.Id framebufferId)
    {
        // Look up the GL FBO handle from framebufferManager using the Id.
        // In the real engine, FramebufferManager would maintain Id → GLuint FBO mapping.
        _ = framebufferManager; // used in real impl
        // gl.BindFramebuffer(FramebufferTarget.Framebuffer, fboHandle);
    }

    private static void BlitToScreen(GL gl, Olve.Engine3D.Rendering.Textures.UntypedTextureId sourceTexture)
    {
        // Bind default framebuffer, draw a fullscreen quad with the source texture.
        // This replaces the current implicit "everything renders to FBO 0" assumption.
        _ = sourceTexture;
        // gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        // ... bind source texture, draw fullscreen quad ...
    }
}
