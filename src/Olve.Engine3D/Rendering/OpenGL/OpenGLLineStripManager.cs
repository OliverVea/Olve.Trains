using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLLineStripManager(Provider<GL> glProvider) : IOpenGLEntityManager<LineStripData, OpenGLLineStripManager.Registration>
{
    private const int PositionFields = 3;
    private const int ColorFields = 3;
    private const int VertexFields = PositionFields + ColorFields;

    public readonly record struct Registration(VAO VAO, VBO VBO);

    public Result<Registration> Register(LineStripData entityData)
    {
        if (entityData.Validate().TryPickProblems(out var problems))
        {
            return problems;
        }

        var vao = glProvider.Value.CreateVertexArray();
        glProvider.Value.BindVertexArray(vao);

        var vbo = glProvider.Value.CreateBuffer();
        glProvider.Value.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

        BufferHelper.WithSpan<float>(entityData.VertexCount * VertexFields, vertices =>
        {
            entityData.Positions.CopyTo(vertices, VertexFields);
            entityData.Colors.CopyTo(vertices, VertexFields, offset: 3);

            glProvider.Value.BufferData(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.DynamicDraw);
        });

        glProvider.Value.VertexAttribPointer(0, PositionFields, VertexAttribPointerType.Float, false, VertexFields * sizeof(float), 0);
        glProvider.Value.EnableVertexAttribArray(0);

        glProvider.Value.VertexAttribPointer(1, ColorFields, VertexAttribPointerType.Float, false, VertexFields * sizeof(float), PositionFields * sizeof(float));
        glProvider.Value.EnableVertexAttribArray(1);

        glProvider.Value.BindVertexArray(0);
        glProvider.Value.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        return new Registration(new VAO(vao), new VBO(vbo, (uint)entityData.VertexCount));
    }

    public Result Update(Registration registration, LineStripData entityData)
    {
        if (entityData.Validate().TryPickProblems(out var problems))
        {
            return problems;
        }

        glProvider.Value.BindBuffer(BufferTargetARB.ArrayBuffer, registration.VBO.Handle);

        BufferHelper.WithSpan<float>(entityData.VertexCount * VertexFields, vertices =>
        {
            entityData.Positions.CopyTo(vertices, VertexFields);
            entityData.Colors.CopyTo(vertices, VertexFields, offset: 3);

            glProvider.Value.BufferData(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.DynamicDraw);
        });

        glProvider.Value.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        return Result.Success();
    }

    public Result Unregister(Registration registration)
    {
        glProvider.Value.DeleteVertexArray(registration.VAO.Handle);
        glProvider.Value.DeleteBuffer(registration.VBO.Handle);

        return Result.Success();
    }
}