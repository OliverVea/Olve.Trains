namespace Olve.Engine3D.Utilities;

public static class Ray
{
    /*
    private static GL Gl => GameManager.GL;

    public static Result Render(this Ray3D<float> ray3D, GLShader glShader, Matrix4X4<float> viewMatrix, Matrix4X4<float> projectionMatrix, float distance = 1000f)
    {
        SetUniforms(glShader.ShaderProgram, viewMatrix, projectionMatrix);

        ReadOnlySpan<float> points =
        [
            ray3D.Origin.X, ray3D.Origin.Y, ray3D.Origin.Z,
            ray3D.Origin.X + ray3D.Direction.X * distance,
            ray3D.Origin.Y + ray3D.Direction.Y * distance,
            ray3D.Origin.Z + ray3D.Direction.Z * distance
        ];

        // Generate and bind VAO
        uint vao = Gl.GenVertexArray();
        Gl.BindVertexArray(vao);

        // Generate and bind VBO
        uint vbo = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        Gl.BufferData(BufferTargetARB.ArrayBuffer, points, BufferUsageARB.StaticDraw);

        // Enable vertex attribute
        Gl.EnableVertexAttribArray(0);
        Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, IntPtr.Zero);

        // Draw the line
        Gl.DrawArrays(PrimitiveType.Lines, 0, 2);

        // Disable attribute and cleanup
        Gl.DisableVertexAttribArray(0);
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        Gl.BindVertexArray(0);

        // Check for errors
        var error = Gl.GetError();
        if (error != GLEnum.NoError)
        {
            return new ResultProblem("OpenGL error: '{0}'", error);
        }

        return Result.Success();
    }

    private static void SetUniforms(ShaderProgram shaderProgram, Matrix4X4<float> view, Matrix4X4<float> projection)
    {
        Span<float> buffer = stackalloc float[16];
        Gl.UseProgram(shaderProgram.Handle);

        CopyTo(view, buffer);
        var viewLocation = Gl.GetUniformLocation(shaderProgram.Handle, "view");
        Gl.UniformMatrix4(viewLocation, 1, false, buffer);

        CopyTo(projection, buffer);
        var projectionLocation = Gl.GetUniformLocation(shaderProgram.Handle, "projection");
        Gl.UniformMatrix4(projectionLocation, 1, false, buffer);
    }

    private static void CopyTo(Matrix4X4<float> matrix, Span<float> buffer)
    {
        buffer[0] = matrix.M11;
        buffer[1] = matrix.M12;
        buffer[2] = matrix.M13;
        buffer[3] = matrix.M14;
        buffer[4] = matrix.M21;
        buffer[5] = matrix.M22;
        buffer[6] = matrix.M23;
        buffer[7] = matrix.M24;
        buffer[8] = matrix.M31;
        buffer[9] = matrix.M32;
        buffer[10] = matrix.M33;
        buffer[11] = matrix.M34;
        buffer[12] = matrix.M41;
        buffer[13] = matrix.M42;
        buffer[14] = matrix.M43;
        buffer[15] = matrix.M44;
    }
    */
}