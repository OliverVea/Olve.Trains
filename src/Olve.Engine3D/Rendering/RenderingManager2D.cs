    using Olve.Engine3D.Rendering.Entities;
    using Olve.Engine3D.Rendering.EntityManagers;
    using Olve.Engine3D.Rendering.OpenGL;
    using Olve.Engine3D.Rendering.OpenGL.Handles;
    using Olve.Engine3D.Rendering.Shaders;
    using Olve.Utilities.Ids;
    using Silk.NET.OpenGL;

    namespace Olve.Engine3D.Rendering;

    public class RenderingManager2D(
        Provider<GL> glProvider,
        OpenGLQuadRenderingManager openGLQuadRenderingManager,
        OpenGLRectangleManager rectangleGLManager,
        OpenGLShaderManager openGLShaderManager,
        ShaderEntityManager shaderEntityManager,
        TextureEntityManager textureEntityManager)
    {
        private RenderingInstanceId NextInstanceId() => new(Id.New());

        protected readonly SortedList<RenderingInstanceId, Instance> Instances = new();

        private const int ErrorCounterThreshold = 20;
        private int _errorCounter;

        protected readonly record struct Instance(
            RenderingInstanceId InstanceId,
            RenderingId<ShaderData> ShaderId,
            float Depth,
            VAO VAO,
            VBO InstanceVBO,
            RenderingId<TextureData>? TextureId = null);

        public Result<RenderingInstanceId> RegisterRectangle(
            RenderingId<ShaderData> shaderId,
            RectangleData rectangle)
        {
            if (rectangle.Validate().TryPickProblems(out var problems))
                return problems.Prepend("Invalid TexturedRectangleData");

            if (shaderEntityManager.GetRegistration(shaderId).TryPickProblems(out problems, out _))
                return problems.Prepend("Failed to get shader data");

            if (textureEntityManager.GetRegistration(rectangle.TextureId).TryPickProblems(out problems, out _))
                return problems.Prepend("Failed to get texture data for TextureId '{0}'", rectangle.TextureId);

            if (rectangleGLManager.Register(rectangle).TryPickProblems(out problems, out var reg))
                return problems.Prepend("Failed to create OpenGL registration for textured rectangle");

            if (openGLQuadRenderingManager.AttachUnitQuad(reg.VAO).TryPickProblems(out problems))
            {
                rectangleGLManager.Unregister(reg);
                return problems.Prepend("Failed to attach unit quad to VAO");
            }

            var id = NextInstanceId();
            Instances.Add(id, new Instance(id, shaderId, rectangle.Depth, reg.VAO, reg.InstanceVBO, rectangle.TextureId));
            return id;
        }

        public Result DeregisterRectangle(RenderingInstanceId instanceId)
        {
            var idx = Instances.IndexOfKey(instanceId);
            if (idx == -1)
                return new ResultProblem("Textured rectangle with id '{0}' is not registered", instanceId);

            var inst = Instances.GetValueAtIndex(idx);
            _ = rectangleGLManager.Unregister(new OpenGLRectangleManager.Registration(inst.VAO, inst.InstanceVBO));

            Instances.RemoveAt(idx);
            return Result.Success();
        }

        public Result UpdateRectangle(RenderingInstanceId instanceId, RectangleData rectangle)
        {
            if (rectangle.Validate().TryPickProblems(out var problems))
                return problems.Prepend("Invalid TexturedRectangleData");

            var idx = Instances.IndexOfKey(instanceId);
            if (idx == -1)
                return new ResultProblem("Textured rectangle with id '{0}' is not registered", instanceId);

            if (textureEntityManager.GetRegistration(rectangle.TextureId).TryPickProblems(out problems, out _))
                return problems.Prepend("Failed to get texture data for TextureId '{0}'", rectangle.TextureId);

            var old = Instances.GetValueAtIndex(idx);
            rectangleGLManager.Unregister(new OpenGLRectangleManager.Registration(old.VAO, old.InstanceVBO));

            if (rectangleGLManager.Register(rectangle).TryPickProblems(out problems, out var reg))
                return problems.Prepend("Failed to upload updated textured rectangle data");

            if (openGLQuadRenderingManager.AttachUnitQuad(reg.VAO).TryPickProblems(out problems))
            {
                rectangleGLManager.Unregister(reg);
                return problems.Prepend("Failed to attach unit quad after update");
            }

            Instances.SetValueAtIndex(idx, old with { VAO = reg.VAO, InstanceVBO = reg.InstanceVBO, Depth = rectangle.Depth, TextureId = rectangle.TextureId });
            return Result.Success();
        }

        public Result Render(IShader shader)
        {
            if (shader.RenderingId == default)
                return new ResultProblem("Shader ID is not set");

            if (Instances.Count == 0)
                return Result.Success();

            if (shaderEntityManager.GetRegistration(shader.RenderingId)
                .TryPickProblems(out var problems, out var shaderRegistration))
            {
                return problems.Prepend("Failed to get shader registration for shader '{0}' ('{1}').",
                    shader.ShaderData.Name, shader.RenderingId);
            }

            if (shader.BlendState.IsTransparent) { glProvider.Value.Enable(GLEnum.Blend); }
            else { glProvider.Value.Disable(GLEnum.Blend); }

            switch (shader.BlendState.Blend)
            {
                case BlendMode.None:
                    break;
                case BlendMode.Alpha:
                    glProvider.Value.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
                    break;
                case BlendMode.Premultiplied:
                    glProvider.Value.BlendFunc(GLEnum.One, GLEnum.OneMinusSrcAlpha);
                    break;
                case BlendMode.Additive:
                    glProvider.Value.BlendFunc(GLEnum.SrcAlpha, GLEnum.One);
                    break;
            }

            glProvider.Value.Disable(GLEnum.DepthTest);
            glProvider.Value.DepthMask(false);

            var parameters = shader.MakeParameters();

            if (openGLShaderManager.LoadShaderInOpenGL(shaderRegistration.ShaderProgram, parameters)
                .TryPickProblems(out problems))
            {
                return problems.Prepend("Failed to load shader '{0}' into OpenGL", shader.ShaderData.Name);
            }

            if (RenderInstances(shader.RenderingId).TryPickProblems(out problems))
                return problems.Prepend("Failed to render GUI rectangles with shader '{0}'", shader.ShaderData.Name);

            // Restore depth testing state
            glProvider.Value.Enable(GLEnum.DepthTest);
            glProvider.Value.DepthMask(true);

            return Result.Success();
        }

        private Result RenderInstances(RenderingId<ShaderData> shaderRenderingId)
        {
            ResultProblemCollection? problems;
            try
            {
                foreach (var inst in Instances.Values.OrderByDescending(x => x.Depth))
                {
                    if (inst.ShaderId != shaderRenderingId)
                        continue;

                    // Bind texture if this is a textured rectangle
                    if (inst.TextureId.HasValue)
                    {
                        if (textureEntityManager.GetRegistration(inst.TextureId.Value)
                            .TryPickProblems(out problems, out var textureReg))
                        {
                            return problems.Prepend("Failed to get texture registration for instance");
                        }

                        glProvider.Value.ActiveTexture(TextureUnit.Texture0);
                        glProvider.Value.BindTexture(TextureTarget.Texture2D, textureReg.Texture.Handle);
                    }

                    if (openGLQuadRenderingManager.LoadQuadsInOpenGL(inst.VAO)
                        .TryPickProblems(out problems))
                    {
                        return problems.Prepend("Failed to bind rectangle instance");
                    }

                    if (openGLQuadRenderingManager.RenderQuad(inst.VAO)
                        .TryPickProblems(out problems))
                    {
                        return problems.Prepend("Failed to draw rectangle instance");
                    }

                    // Unbind texture and VAO
                    if (inst.TextureId.HasValue)
                    {
                        glProvider.Value.BindTexture(TextureTarget.Texture2D, 0);
                    }
                    glProvider.Value.BindVertexArray(0);
                }
            }
            catch (Exception e)
            {
                return new ResultProblem(e, "Failed to render GUI rectangles");
            }

            // Periodic GL error check
            _errorCounter++;
            if (_errorCounter >= ErrorCounterThreshold)
            {
                var error = glProvider.Value.GetError();
                _errorCounter = 0;
                if (error != GLEnum.NoError)
                    return new ResultProblem("OpenGL error: {0}", error);
            }

            return Result.Success();
        }
    }
