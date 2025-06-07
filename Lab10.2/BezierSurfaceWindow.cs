using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Lab10._2
{
    public class BezierSurfaceWindow : GameWindow
    {
        private readonly Vector3[] _controlPoints = new Vector3[16];
        private float _rotationX, _rotationY;
        private bool _wireframeMode;
        private bool _showControlPoints = true;

        private int _vao, _vbo;
        private Shader _surfaceShader, _controlPointShader;
        private int _surfacePatchVao;

        public BezierSurfaceWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings) { }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.ProgramPointSize);

            InitializeControlPoints();
            SetupShaders();
            SetupSurfacePatch();
            SetupControlPoints();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var model = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(_rotationY)) *
                       Matrix4.CreateRotationY(MathHelper.DegreesToRadians(_rotationX));
            var view = Matrix4.LookAt(new Vector3(7, 7, 7), Vector3.Zero, Vector3.UnitY);
            var projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, (float)Size.X / Size.Y, 0.1f, 100f);
            var mvp = model * view * projection;

            _surfaceShader.Use();
            _surfaceShader.SetMatrix4("mvp", mvp);

            for (int i = 0; i < 16; i++)
                _surfaceShader.SetVector3($"controlPoints[{i}]", _controlPoints[i]);

            GL.BindVertexArray(_surfacePatchVao);
            GL.PolygonMode(TriangleFace.FrontAndBack, _wireframeMode ? PolygonMode.Line : PolygonMode.Fill);
            GL.DrawElements(PrimitiveType.Triangles, 6 * 20 * 20, DrawElementsType.UnsignedInt, 0);

            if (_showControlPoints)
            {
                _controlPointShader.Use();
                _controlPointShader.SetMatrix4("mvp", mvp);

                GL.BindVertexArray(_vao);
                GL.DrawArrays(PrimitiveType.Points, 0, _controlPoints.Length);
            }

            SwapBuffers();
        }

        private void InitializeControlPoints()
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    float x = (i - 1.5f) * 0.5f;
                    float z = (j - 1.5f) * 0.5f;
                    float y = (i == 1 || i == 2) && (j == 1 || j == 2) ? 1.5f : 0.0f;
                    _controlPoints[i * 4 + j] = new Vector3(x, y, z);
                }
            }
        }

        private void SetupShaders()
        {
            _surfaceShader = new Shader(
                @"#version 460 core
                layout (location = 0) in vec2 uv;
                uniform mat4 mvp;
                uniform vec3 controlPoints[16];
                
                vec3 EvaluateBezierSurface(vec2 uv) {
                    vec3 result = vec3(0);
                    for (int i = 0; i < 4; i++) {
                        for (int j = 0; j < 4; j++) {
                            float bu = pow(1-uv.x, 3-i) * pow(uv.x, i) * 
                                     (i == 0 ? 1 : 3) * (i == 3 ? 1 : (3-i));
                            float bv = pow(1-uv.y, 3-j) * pow(uv.y, j) * 
                                     (j == 0 ? 1 : 3) * (j == 3 ? 1 : (3-j));
                            result += bu * bv * controlPoints[i*4 + j];
                        }
                    }
                    return result;
                }
                
                void main() {
                    vec3 pos = EvaluateBezierSurface(uv);
                    gl_Position = mvp * vec4(pos, 1.0);
                }",

                @"#version 460 core
                out vec4 FragColor;
                void main() {
                    FragColor = vec4(0.3, 0.5, 0.8, 1.0);
                }");

            _controlPointShader = new Shader(
                @"#version 460 core
                layout (location = 0) in vec3 position;
                uniform mat4 mvp;
                void main() {
                    gl_Position = mvp * vec4(position, 1.0);
                    gl_PointSize = 8.0;
                }",

                @"#version 460 core
                out vec4 FragColor;
                void main() {
                    FragColor = vec4(1.0, 0.0, 0.0, 1.0);
                }");
        }

        private void SetupSurfacePatch()
        {
            const int resolution = 20;
            List<Vector2> uvs = [];
            List<int> indices = [];

            for (int i = 0; i <= resolution; i++)
            {
                for (int j = 0; j <= resolution; j++)
                {
                    float u = i / (float)resolution;
                    float v = j / (float)resolution;

                    uvs.Add(new Vector2(u, v));

                    if (i < resolution && j < resolution)
                    {
                        int idx = i * (resolution + 1) + j;

                        indices.Add(idx);
                        indices.Add(idx + 1);
                        indices.Add(idx + resolution + 1);

                        indices.Add(idx + 1);
                        indices.Add(idx + resolution + 2);
                        indices.Add(idx + resolution + 1);
                    }
                }
            }

            _surfacePatchVao = GL.GenVertexArray();

            GL.BindVertexArray(_surfacePatchVao);

            int vbo = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, uvs.Count * Vector2.SizeInBytes, uvs.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(0);

            int ebo = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(int), indices.ToArray(), BufferUsageHint.StaticDraw);
            GL.BindVertexArray(0);
        }

        private void SetupControlPoints()
        {
            _vao = GL.GenVertexArray();

            GL.BindVertexArray(_vao);

            _vbo = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, _controlPoints.Length * Vector3.SizeInBytes, _controlPoints, BufferUsageHint.DynamicDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(0);
            GL.BindVertexArray(0);
        } 

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            var keyboard = KeyboardState;

            if (keyboard.IsKeyDown(Keys.Left))
                _rotationX += 0.1f;
            if (keyboard.IsKeyDown(Keys.Right))
                _rotationX -= 0.1f;
            if (keyboard.IsKeyDown(Keys.Up))
                _rotationY += 0.1f;
            if (keyboard.IsKeyDown(Keys.Down))
                _rotationY -= 0.1f;
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Keys.W)
                _wireframeMode = !_wireframeMode;

            if (e.Key == Keys.P)
                _showControlPoints = !_showControlPoints;
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
        }
    }
}
