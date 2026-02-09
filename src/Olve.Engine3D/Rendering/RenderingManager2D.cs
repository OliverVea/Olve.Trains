    using Olve.Engine3D.Assets.Entities;
    using Olve.Engine3D.Rendering.EntityManagers;
    using Olve.Engine3D.Rendering.OpenGL;
    using Olve.Engine3D.Rendering.OpenGL.Handles;
    using Olve.Engine3D.Rendering.Parameters;
    using Olve.Engine3D.Rendering.Shaders;
    using Olve.Utilities.Ids;
    using Silk.NET.OpenGL;

    namespace Olve.Engine3D.Rendering;

    public class RenderingManager2D(
        Provider<GL> glProvider,
        OpenGLQuadRenderingManager openGLQuadRenderingManager,
        OpenGLInstancedBufferManager instancedBufferManager,
        OpenGLShaderManager openGLShaderManager,
        ShaderEntityManager shaderEntityManager)
    {
        private RenderingInstanceId NextInstanceId() => new(Id.New());

        private readonly SortedList<RenderingInstanceId, Instance> Instances = new();

        private const int ErrorCounterThreshold = 20;
        private int _errorCounter;

        /// <summary>
        /// Instance stores geometry and optional per-entity shader parameters.
        /// </summary>
        private readonly record struct Instance(
            RenderingInstanceId InstanceId,
            RenderingId<ShaderData> ShaderId,
            float Depth,
            VAO VAO,
            VBO InstanceVBO,
            IShaderParameters? ShaderParameters = null);

        public Result<RenderingInstanceId> Register<T>(
            RenderingId<ShaderData> shaderId,
            T instanceData,
            float depth,
            IShaderParameters? shaderParameters = null) where T : IInstanceData
        {
            if (shaderEntityManager.GetRegistration(shaderId).TryPickProblems(out var problems, out _))
                return problems.Prepend("Failed to get shader data");

            // TODO: investigate this
            Span<float> buf = stackalloc float[T.FloatCount];
            instanceData.WriteTo(buf);

            if (instancedBufferManager.CreateInstanceBuffer(buf, T.ConfigureAttributes, BufferUsageARB.DynamicDraw)
                .TryPickProblems(out problems, out var reg))
                return problems.Prepend("Failed to create OpenGL instance buffer");

            var id = NextInstanceId();
            Instances.Add(id, new Instance(id, shaderId, depth, reg.VAO, reg.InstanceVBO, shaderParameters));
            return id;
        }

        public Result Deregister(RenderingInstanceId instanceId)
        {
            var idx = Instances.IndexOfKey(instanceId);
            if (idx == -1)
                return new ResultProblem("Instance with id '{0}' is not registered", instanceId);

            var inst = Instances.GetValueAtIndex(idx);
            instancedBufferManager.DeleteBuffers(new OpenGLInstancedBufferManager.Registration(inst.VAO, inst.InstanceVBO));

            Instances.RemoveAt(idx);
            return Result.Success();
        }

        public Result Update<T>(
            RenderingInstanceId instanceId,
            T instanceData,
            float depth,
            IShaderParameters? shaderParameters = null) where T : IInstanceData
        {
            var idx = Instances.IndexOfKey(instanceId);
            if (idx == -1)
                return new ResultProblem("Instance with id '{0}' is not registered", instanceId);

            var old = Instances.GetValueAtIndex(idx);
            instancedBufferManager.DeleteBuffers(new OpenGLInstancedBufferManager.Registration(old.VAO, old.InstanceVBO));

            Span<float> buf = stackalloc float[T.FloatCount];
            instanceData.WriteTo(buf);

            if (instancedBufferManager.CreateInstanceBuffer(buf, T.ConfigureAttributes, BufferUsageARB.DynamicDraw)
                .TryPickProblems(out var problems, out var reg))
                return problems.Prepend("Failed to upload updated instance data");

            Instances.SetValueAtIndex(idx, old with
            {
                VAO = reg.VAO,
                InstanceVBO = reg.InstanceVBO,
                Depth = depth,
                ShaderParameters = shaderParameters ?? old.ShaderParameters
            });

            return Result.Success();
        }

        /// <summary>
        /// Updates only the shader parameters of an existing instance without changing geometry.
        /// </summary>
        public Result UpdateShaderParameters(RenderingInstanceId instanceId, IShaderParameters? shaderParameters)
        {
            var idx = Instances.IndexOfKey(instanceId);
            if (idx == -1)
                return new ResultProblem("Instance not found: {0}", instanceId);

            var old = Instances.GetValueAtIndex(idx);
            Instances.SetValueAtIndex(idx, old with { ShaderParameters = shaderParameters });

            return Result.Success();
        }

        public Result Render(IShader shader)
        {
            if (shader.RenderingId == default)
            {
                return new ResultProblem("Shader ID is not set");
            }

            if (Instances.Count == 0)
            {
                return Result.Success();
            }

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

            if (RenderInstances(shader, shaderRegistration.ShaderProgram).TryPickProblems(out problems))
                return problems.Prepend("Failed to render GUI rectangles with shader '{0}'", shader.ShaderData.Name);

            // Restore depth testing state
            glProvider.Value.Enable(GLEnum.DepthTest);
            glProvider.Value.DepthMask(true);

            return Result.Success();
        }

        private Result RenderInstances(IShader shader, ShaderProgram shaderProgram)
        {
            ResultProblemCollection? problems;
            try
            {
                foreach (var instance in Instances.Values.OrderByDescending(x => x.Depth))
                {
                    if (instance.ShaderId != shader.RenderingId)
                    {
                        continue;
                    }

                    // Apply per-entity shader parameter overrides if present
                    if (instance.ShaderParameters is { } entityParams)
                    {
                        var renderingParams = entityParams.ToRenderingParameters();
                        if (openGLShaderManager.ApplyParameters(shaderProgram, renderingParams)
                            .TryPickProblems(out problems))
                        {
                            return problems.Prepend("Failed to apply entity shader parameters");
                        }
                    }

                    if (openGLQuadRenderingManager.LoadQuadsInOpenGL(instance.VAO)
                        .TryPickProblems(out problems))
                    {
                        return problems.Prepend("Failed to bind rectangle instance");
                    }

                    if (openGLQuadRenderingManager.RenderQuad(instance.VAO)
                        .TryPickProblems(out problems))
                    {
                        return problems.Prepend("Failed to draw rectangle instance");
                    }

                    glProvider.Value.BindVertexArray(0);
                }
            }
            catch (Exception e)
            {
                return new ResultProblem(e, "Failed to render rectangles");
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
