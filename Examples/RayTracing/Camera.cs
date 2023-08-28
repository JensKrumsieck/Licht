using System.Numerics;
using Catalyst.Engine;
using Silk.NET.Input;
namespace RayTracing;

public class Camera
{
    private readonly float _verticalFoV;
    private readonly float _nearClip;
    private readonly float _farClip;

    private uint _viewportHeight;
    private uint _viewportWidth;

    public Vector3 Position;
    private Vector3 _forwardDirection;

    private Matrix4x4 _projection;
    private Matrix4x4 _invProjection;
    private Matrix4x4 _view;
    private Matrix4x4 _invView;
    private Vector3[] _rayDirections;
    
    private Vector2 _lastMousePos;
    private const float RotationSpeed = .3f;
    private readonly IMouse _mouse;
    private readonly IKeyboard _keyboard;
    
    public Camera(float verticalFoV, float nearClip, float farClip)
    {
        _verticalFoV = verticalFoV;
        _nearClip = nearClip;
        _farClip = farClip;

        Position = Vector3.UnitZ * 6f;
        _forwardDirection =  -Vector3.UnitZ;
        
        var input = Application.GetInput();
        _mouse = input.Mice[0];
        _keyboard = input.Keyboards[0];
        
        RecalculateView();
        RecalculateProjection();
        RecalculateRayDirections();
    }
    
    public bool OnUpdate(float deltaTime)
    {
        var mousePos = _mouse.Position;
        var delta = (_lastMousePos - mousePos) * .002f;
        _lastMousePos = mousePos;
        if (!_mouse.IsButtonPressed(MouseButton.Right))
        {
            _mouse.Cursor.CursorMode = CursorMode.Normal;
            return false;
        }
        _mouse.Cursor.CursorMode = CursorMode.Disabled;
        var moved = false;
        var rightDir = Vector3.Cross(_forwardDirection, Vector3.UnitY);
        const float speed = 5.0f;

        if (_keyboard.IsKeyPressed(Key.W))
        {
            Position += speed * _forwardDirection * deltaTime;
            moved = true;
        }
        else if (_keyboard.IsKeyPressed(Key.S))
        {
            Position -= speed * _forwardDirection * deltaTime;
            moved = true;
        }
        else if (_keyboard.IsKeyPressed(Key.A))
        {
            Position += speed * rightDir * deltaTime;
            moved =true;
        }
        else if (_keyboard.IsKeyPressed(Key.D))
        {
            Position -= speed * rightDir * deltaTime;
            moved =true;
        } 
        else if (_keyboard.IsKeyPressed(Key.Q))
        {
            Position -= speed * Vector3.UnitY * deltaTime;
            moved =true;
        } 
        else if (_keyboard.IsKeyPressed(Key.E))
        {
            Position += speed * Vector3.UnitY * deltaTime;
            moved =true;
        }

        if (delta.X != 0.0f || delta.Y != 0.0f)
        {
            var pitchDelta = delta.Y * RotationSpeed;
            var yawDelta = delta.X * RotationSpeed;
            var q1 = Quaternion.CreateFromAxisAngle(rightDir, -pitchDelta);
            var q2 = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -yawDelta);
            var q = Quaternion.Normalize(Utils.CrossProduct(q1, q2));
            _forwardDirection = Vector3.Transform(_forwardDirection, q);
            moved = true;
        }
        
        if(!moved) return false;
        RecalculateView();
        RecalculateRayDirections();

        return moved;
    }
    public void OnResize(uint width, uint height)
    {
        if(width == _viewportWidth && height == _viewportHeight) return;
        _viewportWidth = width;
        _viewportHeight = height;
        
        RecalculateProjection();
        RecalculateRayDirections();
    }

    public Vector3 GetRayDirection(int index) => _rayDirections[index];

    private void RecalculateView()
    {
        _view = Matrix4x4.CreateLookAt(Position, Position + _forwardDirection, Vector3.UnitY);
        Matrix4x4.Invert(_view, out _invView);
    }

    private void RecalculateProjection()
    {
        _projection = Matrix4x4.CreatePerspectiveFieldOfView(_verticalFoV * MathF.PI / 180f,
            (float) _viewportWidth / _viewportHeight, _nearClip, _farClip);
        Matrix4x4.Invert(_projection, out _invProjection);
    }

    private void RecalculateRayDirections()
    {
        if(_rayDirections?.Length != (int)(_viewportHeight * _viewportWidth))
            Array.Resize(ref _rayDirections, (int)(_viewportHeight * _viewportWidth));
        
        for (var y = 0; y < _viewportHeight; y++)
        {
            for (var x = 0; x < _viewportWidth; x++)
            {
                var coord = new Vector2((float) x / _viewportWidth, (float) y / _viewportHeight);
                coord = coord * 2f - Vector2.One;
                var target = Vector4.Transform(new Vector4(coord.X, coord.Y, 1, 1), _invProjection);
                var targetXYZ = new Vector3(target.X, target.Y, target.Z);
                var rayDir = Vector4.Transform(new Vector4(Vector3.Normalize(targetXYZ / target.W), 0), _invView);
                _rayDirections[x + y * _viewportWidth] = new Vector3(rayDir.X, rayDir.Y, rayDir.Z);
            }
        }
    }
    
}
