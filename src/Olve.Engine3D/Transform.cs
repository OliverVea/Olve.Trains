namespace Olve.Engine3D;

public class Transform
{
    private bool _dirty = true;
    private Matrix4X4<float> _world = Matrix4X4<float>.Identity;
    private Vector3D<float> _position = Vector3D<float>.Zero;
    private Quaternion<float> _rotation = Quaternion<float>.Identity;

    public Vector3D<float> Position
    {
        get => _position;
        set
        {
            _position = value;
            _dirty = true;
        }
    }

    public Quaternion<float> Rotation
    {
        get => _rotation;
        set
        {
            _rotation = value;
            _dirty = true;
        }
    }

    public Matrix4X4<float> World
    {
        get
        {
            if (_dirty)
            {
                _world = Matrix4X4.CreateFromQuaternion(_rotation) * Matrix4X4.CreateTranslation(_position);
                _dirty = false;
            }

            return _world;
        }
    }
}