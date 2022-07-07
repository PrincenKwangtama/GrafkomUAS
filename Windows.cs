using System;
using System.Collections.Generic;
using System.IO;
using LearnOpenTK.Common;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using System.Drawing.Imaging;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;
//using Pertemuan7;

namespace GrafkomUAS
{
    class Windows : GameWindow
    {
        private Mesh mesh0;
        
        private Mesh mesh1;
        private Mesh mesh2;
        private Mesh mesh3;
        private Mesh mesh4;
        
        private Mesh mainchar;

        Dictionary<string, List<Material>> materials_dict = new Dictionary<string, List<Material>>();

        private Camera _camera;

        private Vector3 _objectPos;

        private Vector2 _lastMousePosition;
        private bool _firstMove = false;
        private bool postprocessing = false;

        //Light
        List<Light> lights = new List<Light>();

        //Frame Buffers
        int fbo;

        //Shader
        Shader shader;
        Shader screenShader;
        Shader skyboxShader;

        char _direction;

        //Quad Screen
        float[] quadVertices = { // vertex attributes for a quad that fills the entire screen in Normalized Device Coordinates.
        // positions   // texCoords
        -1.0f,  1.0f,  0.0f, 1.0f,
        -1.0f, -1.0f,  0.0f, 0.0f,
         1.0f, -1.0f,  1.0f, 0.0f,

        -1.0f,  1.0f,  0.0f, 1.0f,
         1.0f, -1.0f,  1.0f, 0.0f,
         1.0f,  1.0f,  1.0f, 1.0f
        };
        int _vao;
        int _vbo;
        int texColorBuffer;

        //Cubemap
        int cubemap;
        int _vao_cube;
        int _vbo_cube;
        float[] skyboxVertices = {
        // positions          
        -1.0f,  1.0f, -1.0f,
        -1.0f, -1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,
         1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f, -1.0f,

        -1.0f, -1.0f,  1.0f,
        -1.0f, -1.0f, -1.0f,
        -1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f,  1.0f,
        -1.0f, -1.0f,  1.0f,

         1.0f, -1.0f, -1.0f,
         1.0f, -1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,

        -1.0f, -1.0f,  1.0f,
        -1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f, -1.0f,  1.0f,
        -1.0f, -1.0f,  1.0f,

        -1.0f,  1.0f, -1.0f,
         1.0f,  1.0f, -1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
        -1.0f,  1.0f,  1.0f,
        -1.0f,  1.0f, -1.0f,

        -1.0f, -1.0f, -1.0f,
        -1.0f, -1.0f,  1.0f,
         1.0f, -1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,
        -1.0f, -1.0f,  1.0f,
         1.0f, -1.0f,  1.0f
    };

        public Windows(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        private Matrix4 generateArbRotationMatrix(Vector3 axis, Vector3 center, float degree)
        {
            var rads = MathHelper.DegreesToRadians(degree);

            var secretFormula = new float[4, 4] {
                { (float)Math.Cos(rads) + (float)Math.Pow(axis.X, 2) * (1 - (float)Math.Cos(rads)), axis.X* axis.Y * (1 - (float)Math.Cos(rads)) - axis.Z * (float)Math.Sin(rads),    axis.X * axis.Z * (1 - (float)Math.Cos(rads)) + axis.Y * (float)Math.Sin(rads),   0 },
                { axis.Y * axis.X * (1 - (float)Math.Cos(rads)) + axis.Z * (float)Math.Sin(rads),   (float)Math.Cos(rads) + (float)Math.Pow(axis.Y, 2) * (1 - (float)Math.Cos(rads)), axis.Y * axis.Z * (1 - (float)Math.Cos(rads)) - axis.X * (float)Math.Sin(rads),   0 },
                { axis.Z * axis.X * (1 - (float)Math.Cos(rads)) - axis.Y * (float)Math.Sin(rads),   axis.Z * axis.Y * (1 - (float)Math.Cos(rads)) + axis.X * (float)Math.Sin(rads),   (float)Math.Cos(rads) + (float)Math.Pow(axis.Z, 2) * (1 - (float)Math.Cos(rads)), 0 },
                { 0, 0, 0, 1}
            };
            var secretFormulaMatrix = new Matrix4(
                new Vector4(secretFormula[0, 0], secretFormula[0, 1], secretFormula[0, 2], secretFormula[0, 3]),
                new Vector4(secretFormula[1, 0], secretFormula[1, 1], secretFormula[1, 2], secretFormula[1, 3]),
                new Vector4(secretFormula[2, 0], secretFormula[2, 1], secretFormula[2, 2], secretFormula[2, 3]),
                new Vector4(secretFormula[3, 0], secretFormula[3, 1], secretFormula[3, 2], secretFormula[3, 3])
            );

            return secretFormulaMatrix;
        }

        protected override void OnLoad()
        {
            GL.ClearColor(0.2f, 0.2f, 0.5f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            //Shader
            shader = new Shader("../../../Shaders/shader.vert",
                "../../../Shaders/lighting.frag");
            shader.Use();

            //Screen Shader
            screenShader = new Shader("../../../Shaders/PostProcessing.vert",
                "../../../Shaders/PostProcessing.frag");
            screenShader.Use();
            screenShader.SetInt("screenTexture", 0);
            //Frame Buffers
            GL.GenFramebuffers(1, out fbo);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

            //Add Texture to Frame Buffer
            GL.GenTextures(1, out texColorBuffer);
            Console.WriteLine("TexColorBuffer: " + texColorBuffer);
            GL.BindTexture(TextureTarget.Texture2D, texColorBuffer);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, 800, 600, 0, PixelFormat.Rgb, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D
                , texColorBuffer, 0);
            //Render Buffer
            int rbo;
            GL.GenRenderbuffers(1, out rbo);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, 800, 600);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment,
                RenderbufferTarget.Renderbuffer, rbo);

            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) == FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine("Screen Frame Buffer Created");
            }
            else
            {
                Console.WriteLine("Screen Frame Buffer NOT complete");
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            //Initialize default material
            InitDefaultMaterial();
            //Create Cube Map
            CreateCubeMap();
            skyboxShader = new Shader("../../../Shaders/skybox.vert",
                "../../../Shaders/skybox.frag");

            //Vertices
            //Inisialiasi VBO
            _vbo_cube = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo_cube);
            GL.BufferData(BufferTarget.ArrayBuffer, skyboxVertices.Length * sizeof(float),
                skyboxVertices, BufferUsageHint.StaticDraw);

            //Inisialisasi VAO
            _vao_cube = GL.GenVertexArray();
            GL.BindVertexArray(_vao_cube);
            var vertexLocation = skyboxShader.GetAttribLocation("aPosition");
            GL.EnableVertexAttribArray(vertexLocation);
            GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

            skyboxShader.Use();
            skyboxShader.SetInt("skybox", 4);
            //Screen Quad
            GL.GenVertexArrays(1, out _vao);
            GL.GenBuffers(1, out _vbo);
            GL.BindVertexArray(_vbo);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

            //Light Position
            lights.Add(new PointLight(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.05f, 0.05f, 0.05f),
                new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f), 0.5f, 0.5f, 0.5f));
            lights.Add(new PointLight(new Vector3(0.5f, 8f, 0.5f), new Vector3(0.2f, 0.2f, 0.2f),
                new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f), 0.5f, 0.5f, 0.5f));
            lights.Add(new PointLight(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.05f, 0.05f, 0.05f),
                new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f), 0.5f, 0.5f, 0.5f));
            lights.Add(new DirectionLight(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.1f, 0.1f, 0.1f),
                new Vector3(1.0f, 1.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f), new Vector3(1f, 1f, -1f)));

            //Initialize Mesh here
            mesh0 = LoadObjFile("../../../Resources/p&fhome.obj");
            mesh0.setupObject(1.0f, 1.0f);
            mesh0.scale(5f);
            //_objectPos = mesh0.getTransform().ExtractTranslation();

            mesh1 = LoadObjFile("../../../Resources/robot.obj");
            mesh1.setupObject(1.0f, 1.0f);
            mesh1.rotate(0, 180, 0);
            mesh1.translate(new Vector3(0f, 0.25f, 0f));
            mesh1.scale(8f);

            mesh2 = LoadObjFile("../../../Resources/perry.obj");
            mesh2.setupObject(1.0f, 1.0f);
            mesh2.translate(new Vector3(-1f, 0.25f, 0f));
            mesh2.scale(2f);

            /*
            mesh3 = LoadObjFile("../../../Resources/dodocoSmall.obj");
            mesh3.setupObject(1.0f, 1.0f);
            mesh3.rotate(0f, 45f, 0f);
            mesh3.translate(new Vector3(-0.5f, 0f, 0f));
            mesh3.scale(3f);

            mesh4 = LoadObjFile("../../../Resources/dodocoTail.obj");
            mesh4.setupObject(1.0f, 1.0f);
            mesh4.translate(new Vector3(0.5f, 0f, 0f));
            mesh4.scale(2f);
            */

            mainchar = LoadObjFile("../../../Resources/phinFinal.obj");
            mainchar.setupObject(1.0f, 1.0f);
            //mainchar.translate(new Vector3(0,0,3f));
            mainchar.translate(new Vector3(-20f,0,-17f));
            mainchar.rotate(0f, 180f, 0f);
            _objectPos = mainchar.getTransform().ExtractTranslation();

            //lights[0].Position = new Vector3(-0.2f, 0.3f, 0.3f);
            //lights[0].Position = new Vector3(0.0f, 1.5f, -0.3f);
            //lights[1].Position = new Vector3(0.0f, 3f, -1.0f);
            //lights[2].Position = new Vector3(0.0f, 0.5f, 2.0f);

            lights[0].Position = new Vector3(0.0f, 1.5f, -0.3f);
            lights[1].Position = new Vector3(0.0f, 8f, -1.0f);
            lights[2].Position = new Vector3(0.0f, 0.5f, 2.0f);

            var _cameraPosInit = new Vector3(0,0.5f,3f);
            //var _cameraPosInit = new Vector3(-50f, 0.5f, 3f);
            _camera = new Camera(_cameraPosInit, Size.X / (float)Size.Y);
            //_camera.Fov = 90f;
            //_camera.Yaw -= 90f;
            //CursorGrabbed = true;
            base.OnLoad();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {   
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            if (GLFW.GetTime() > 0.02)
            {
                LampRevolution();
                GLFW.SetTime(0.0);
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            if (postprocessing)
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
                GL.Enable(EnableCap.DepthTest);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                //Textured Rendering
                GL.ActiveTexture(TextureUnit.Texture0);
                shader.Use();
                for (int i = 0; i < lights.Count; i++)
                {
                    mesh0.calculateTextureRender(_camera, lights[i], i);
                    
                    mesh1.calculateTextureRender(_camera, lights[i], i);
                    mesh2.calculateTextureRender(_camera, lights[i], i);
                    //mesh3.calculateTextureRender(_camera, lights[i], i);
                    //mesh4.calculateTextureRender(_camera, lights[i], i);
                    
                    mainchar.calculateTextureRender(_camera, lights[i], i);
                }

                GL.BindVertexArray(0);

                //Default FrameBuffer
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.Disable(EnableCap.DepthTest);
                GL.Clear(ClearBufferMask.ColorBufferBit);

                screenShader.Use();
                screenShader.SetInt("screenTexture", texColorBuffer);
                GL.BindVertexArray(_vao);
                GL.BindTexture(TextureTarget.Texture2D, texColorBuffer);
                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            }
            else
            {
                shader.Use();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.Enable(EnableCap.DepthTest);
                //GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                //Textured Rendering
                GL.ActiveTexture(TextureUnit.Texture0);
                for (int i = 0; i < lights.Count; i++)
                {
                    mesh0.calculateTextureRender(_camera, lights[i], i);
                    mesh1.calculateTextureRender(_camera, lights[i], i);
                    mesh2.calculateTextureRender(_camera, lights[i], i);
                    //mesh3.calculateTextureRender(_camera, lights[i], i);
                    //mesh4.calculateTextureRender(_camera, lights[i], i);
                    mainchar.calculateTextureRender(_camera, lights[i], i);
                }

                //Render Skybox
                GL.DepthFunc(DepthFunction.Lequal);
                //GL.DepthMask(false);
                skyboxShader.Use();
                Matrix4 skyview = _camera.GetViewMatrix().ClearTranslation().ClearScale();
                skyboxShader.SetMatrix4("view", skyview);
                
                Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(_camera.Fov),
                    Size.X / (float)Size.Y, 1f, 100f);
                skyboxShader.SetMatrix4("projection", projection);
                skyboxShader.SetInt("skybox", 4);
                GL.BindVertexArray(_vao_cube);
                GL.ActiveTexture(TextureUnit.Texture4);
                GL.BindTexture(TextureTarget.TextureCubeMap, cubemap);
                GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
                GL.DepthFunc(DepthFunction.Less);

            }

            SwapBuffers();

            base.OnRenderFrame(args);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            //Console.WriteLine("Camera Position" + _camera.Position + " Object Position: " + _objectPos);
            const float cameraSpeed = 3f;
            // Escape keyboard
            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }
            // Zoom in
            if (KeyboardState.IsKeyDown(Keys.I))
            {
                _camera.Fov -= 0.5f;
            }
            // Zoom out
            if (KeyboardState.IsKeyDown(Keys.O))
            {
                _camera.Fov += 0.5f;
            }

            // Maju (W)
            if (KeyboardState.IsKeyDown(Keys.W))
            {
                //_camera.Position += _camera.Front * cameraSpeed * (float)args.Time;
                //_objectPos += _camera.Position;
                rotateView(_direction, 'w');
                mainchar.translate(new Vector3(0, 0, -1) * cameraSpeed * (float)args.Time);
                Vector3 vec = mainchar.getTransform().ExtractTranslation();
                _camera.Position = new Vector3(vec.X - 3f, vec.Y + 2f, vec.Z + 3.5f);

            }
            // Mundur (S)
            if (KeyboardState.IsKeyDown(Keys.S))
            {
                //_camera.Position -= _camera.Front * cameraSpeed * (float)args.Time;
                //_objectPos += _camera.Position;
                rotateView(_direction, 's');
                mainchar.translate(-(new Vector3(0, 0, -1) * cameraSpeed * (float)args.Time));
                Vector3 vec = mainchar.getTransform().ExtractTranslation();
                _camera.Position = new Vector3(vec.X - 3f, vec.Y + 2f, vec.Z - 3.5f);
            }
            // Kiri (A)
            if (KeyboardState.IsKeyDown(Keys.A))
            {
                //_camera.Position -= new Vector3(1, 0, 0) * cameraSpeed * (float)args.Time;
                //_objectPos += _camera.Position;
                rotateView(_direction, 'a');
                mainchar.translate(-(new Vector3(1, 0, 0) * cameraSpeed * (float)args.Time));
                Vector3 vec = mainchar.getTransform().ExtractTranslation();
                _camera.Position = new Vector3(vec.X + 2f, vec.Y + 2f, vec.Z);
            }
            // Kanan (D)
            if (KeyboardState.IsKeyDown(Keys.D))
            {
                //_camera.Position += new Vector3(1, 0, 0) * cameraSpeed * (float)args.Time;
                //_objectPos += _camera.Position;
                rotateView(_direction, 'd');
                mainchar.translate((new Vector3(1, 0, 0) * cameraSpeed * (float)args.Time));
                Vector3 vec = mainchar.getTransform().ExtractTranslation();
                _camera.Position = new Vector3(vec.X - 7f, vec.Y + 2f, vec.Z);
            }

            // Naik (Q)
            if (KeyboardState.IsKeyDown(Keys.Q))
            {
                _camera.Position += _camera.Up * cameraSpeed * (float)args.Time;
            }
            // Turun (E)
            if (KeyboardState.IsKeyDown(Keys.E))
            {
                _camera.Position -= _camera.Up * cameraSpeed * (float)args.Time;
            }

            const float _rotationSpeed = 0.1f;

            if (!IsFocused)
            {
                return;
            }

            const float sensitivity = 0.2f;
            if (_firstMove)
            {
                _lastMousePosition = new Vector2(MouseState.X, MouseState.Y);
                _firstMove = false;
            }
            else
            {
                // Hitung selisih mouse position
                var deltaX = MouseState.X - _lastMousePosition.X;
                var deltaY = MouseState.Y - _lastMousePosition.Y;
                _lastMousePosition = new Vector2(MouseState.X, MouseState.Y);


                //YAW
                _objectPos *= 10;
                var axisYaw = new Vector3(0, 1, 0);
                _camera.Position -= mainchar.getTransform().ExtractTranslation();
                _camera.Yaw += deltaX * _rotationSpeed * sensitivity * 0.1f;
                _camera.Position = Vector3.Transform(_camera.Position,
                    generateArbRotationMatrix(axisYaw, mainchar.getTransform().ExtractTranslation(), deltaX).ExtractRotation());
                _camera.Position += mainchar.getTransform().ExtractTranslation();

                _camera._front = -Vector3.Normalize(_camera.Position - mainchar.getTransform().ExtractTranslation());
                _objectPos /= 10;

                //Pitch
                _objectPos *= 10;
                var axisPitch = new Vector3(1, 0, 0);
                _camera.Position -= mainchar.getTransform().ExtractTranslation();
                _camera.Pitch -= deltaY * _rotationSpeed * sensitivity * 0.1f;
                _camera.Position = Vector3.Transform(_camera.Position,
                    generateArbRotationMatrix(axisPitch, mainchar.getTransform().ExtractTranslation(), deltaY).ExtractRotation());
                _camera.Position += mainchar.getTransform().ExtractTranslation();

                _camera._front = -Vector3.Normalize(_camera.Position - mainchar.getTransform().ExtractTranslation());
                _objectPos /= 10;
                //_camera.Yaw += deltaX * sensitivity;
                //_camera.Pitch -= deltaY * sensitivity;
            }

            base.OnUpdateFrame(args);
        }

        public void rotateView(char curDir, char dstDir)
        {
            Console.WriteLine(curDir + " " + dstDir);

            if(curDir != dstDir)
            {
                /*
                if((curDir == 'd' && dstDir == 's') || (curDir == 's' && dstDir == 'a') 
                  || (curDir == 'a' && dstDir == 'w') || (curDir == 'w' && dstDir == 'd'))
                {
                    mainchar.translate(mainchar.getTransform().ExtractTranslation());
                    mainchar.rotate(0, 90, 0);
                    mainchar.translate(-mainchar.getTransform().ExtractTranslation());
                }
                else if((curDir == 'd' && dstDir == 'w') || (curDir == 'w' && dstDir == 'a')
                  || (curDir == 'a' && dstDir == 's') || (curDir == 's' && dstDir == 'd'))
                {
                    mainchar.translate(mainchar.getTransform().ExtractTranslation());
                    mainchar.rotate(0, -90, 0);
                    mainchar.translate(-mainchar.getTransform().ExtractTranslation());
                }
                else if((curDir == 'd' && dstDir == 'a') || (curDir == 'w' && dstDir == 's')
                  || (curDir == 'a' && dstDir == 'd') || (curDir == 's' && dstDir == 'w'))
                {
                    mainchar.translate(mainchar.getTransform().ExtractTranslation());
                    mainchar.rotate(0, 180, 0);
                    mainchar.translate(-mainchar.getTransform().ExtractTranslation());
                }
                _direction = dstDir;  
                */    
                
                /*
                Vector3 curTrans = mainchar.getTransform().ExtractTranslation();

                if ((curDir == 'd' && dstDir == 's') || (curDir == 's' && dstDir == 'a')
                  || (curDir == 'a' && dstDir == 'w') || (curDir == 'w' && dstDir == 'd'))
                {
                    mainchar.translateToZero();
                    mainchar.rotate(0, 90, 0);
                    mainchar.translate(curTrans);
                }
                else if ((curDir == 'd' && dstDir == 'w') || (curDir == 'w' && dstDir == 'a')
                  || (curDir == 'a' && dstDir == 's') || (curDir == 's' && dstDir == 'd'))
                {
                    mainchar.translateToZero();
                    mainchar.rotate(0, -90, 0);
                    mainchar.translate(curTrans);
                }
                else if ((curDir == 'd' && dstDir == 'a') || (curDir == 'w' && dstDir == 's')
                  || (curDir == 'a' && dstDir == 'd') || (curDir == 's' && dstDir == 'w'))
                {
                    mainchar.translateToZero();
                    mainchar.rotate(0, 180, 0);
                    mainchar.translate(curTrans);
                }
                _direction = dstDir; 
                */
            }                     
        }

        private void InitDefaultMaterial()
        {
            List<Material> materials = new List<Material>();
            Texture diffuseMap = Texture.LoadFromFile("../../../Resources/white.jpg");
            Texture textureMap = Texture.LoadFromFile("../../../Resources/white.jpg");
            materials.Add(new Material("Default", 128.0f, new Vector3(0.1f), new Vector3(1f), new Vector3(1f),
                    1.0f, diffuseMap, textureMap));

            materials_dict.Add("Default", materials);
        }

        public Mesh LoadObjFile(string path, bool usemtl = true)
        {
            Mesh mesh = new Mesh("../../../Shaders/shader.vert",
                "../../../Shaders/lighting.frag");
            List<Vector3> temp_vertices = new List<Vector3>();
            List<Vector3> temp_normals = new List<Vector3>();
            List<Vector3> temp_textureVertices = new List<Vector3>();
            List<uint> temp_vertexIndices = new List<uint>();
            List<uint> temp_normalsIndices = new List<uint>();
            List<uint> temp_textureIndices = new List<uint>();
            List<string> temp_name = new List<string>();
            List<String> temp_materialsName = new List<string>();
            string current_materialsName = "";
            string material_library = "";
            int mesh_count = 0;
            int mesh_created = 0;

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Unable to open /"+  path + "/, does not exist.");
            }

            using (StreamReader streamReader = new StreamReader(path))
            {
                while (!streamReader.EndOfStream)
                {
                    List<string> words = new List<string>(streamReader.ReadLine().Split(' '));
                    words.RemoveAll(s => s == string.Empty);

                    if (words.Count == 0)
                        continue;
                    string type = words[0];

                    words.RemoveAt(0);

                    switch (type)
                    {
                        //Render tergantung nama dan objek apa sehingga bisa buat hirarki
                        case "o":
                            if(mesh_count > 0)
                            {
                                Mesh mesh_tmp = new Mesh();
                                //Attach Shader
                                mesh_tmp.setShader(shader);
                                mesh_tmp.setDepthShader(skyboxShader);
                                for (int i = 0; i < temp_vertexIndices.Count; i++)
                                {
                                    uint vertexIndex = temp_vertexIndices[i];
                                    mesh_tmp.AddVertices(temp_vertices[(int)vertexIndex - 1]);
                                }
                                for (int i = 0; i < temp_textureIndices.Count; i++)
                                {
                                    uint textureIndex = temp_textureIndices[i];
                                    mesh_tmp.AddTextureVertices(temp_textureVertices[(int)textureIndex - 1]);
                                }
                                for (int i = 0; i < temp_normalsIndices.Count; i++)
                                {
                                    uint normalIndex = temp_normalsIndices[i];
                                    mesh_tmp.AddNormals(temp_normals[(int)normalIndex - 1]);
                                }
                                mesh_tmp.setName(temp_name[mesh_created]);

                                //Material
                                if(usemtl)
                                {
                                    
                                    List<Material> mtl = materials_dict[material_library];
                                    for (int i = 0; i < mtl.Count; i++)
                                    {
                                        if (mtl[i].Name == current_materialsName)
                                        {
                                            mesh_tmp.setMaterial(mtl[i]);
                                        }
                                    }
                                }
                                else
                                {
                                    List<Material> mtl = materials_dict["Default"];
                                    for (int i = 0; i < mtl.Count; i++)
                                    {
                                        if (mtl[i].Name == "Default")
                                        {
                                            mesh_tmp.setMaterial(mtl[i]);
                                        }
                                    }
                                }
                                
                                
                                if(mesh_count == 1)
                                {
                                    mesh = mesh_tmp;
                                }
                                else
                                {
                                    mesh.child.Add(mesh_tmp);
                                }

                                mesh_created++;
                            }
                            temp_name.Add(words[0]);
                            mesh_count++;
                            break;
                        case "v":
                            temp_vertices.Add(new Vector3(float.Parse(words[0]) / 10, float.Parse(words[1]) / 10, float.Parse(words[2]) / 10));
                            break;

                        case "vt":
                            temp_textureVertices.Add(new Vector3(float.Parse(words[0]), float.Parse(words[1]),
                                                            words.Count < 3 ? 0 : float.Parse(words[2])));
                            break;

                        case "vn":
                            temp_normals.Add(new Vector3(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2])));
                            break;
                        case "mtllib":
                            if(usemtl)
                            {
                                string resourceName = "../../../Resources/" + words[0];
                                string nameWOExt = words[0].Split(".")[0];
                                Console.WriteLine(nameWOExt);
                                materials_dict.Add(nameWOExt, LoadMtlFile(resourceName));
                                material_library = nameWOExt;
                            }
                            
                            break;
                        case "usemtl":
                            if(usemtl)
                            {
                                current_materialsName = words[0];
                            }

                            break;
                        // face
                        case "f":
                            foreach (string w in words)
                            {
                                if (w.Length == 0)
                                    continue;

                                string[] comps = w.Split('/');
                                for (int i = 0; i < comps.Length; i++)
                                {
                                    if (i == 0)
                                    {
                                        if (comps[0].Length > 0)
                                        {
                                            temp_vertexIndices.Add(uint.Parse(comps[0]));
                                        }

                                    }
                                    else if (i == 1)
                                    {
                                        if (comps[1].Length > 0)
                                        {
                                            temp_textureIndices.Add(uint.Parse(comps[1]));
                                        }

                                    }
                                    else if (i == 2)
                                    {
                                        if (comps[2].Length > 0)
                                        {
                                            temp_normalsIndices.Add(uint.Parse(comps[2]));
                                        }

                                    }
                                }

                            }
                            break;

                        default:
                            break;
                    }
                }
            }
            if (mesh_created < mesh_count)
            {

                Mesh mesh_tmp = new Mesh();
                //Attach Shader
                mesh_tmp.setShader(shader);
                mesh_tmp.setDepthShader(skyboxShader);
                for (int i = 0; i < temp_vertexIndices.Count; i++)
                {
                    uint vertexIndex = temp_vertexIndices[i];
                    mesh_tmp.AddVertices(temp_vertices[(int)vertexIndex - 1]);
                }
                for (int i = 0; i < temp_textureIndices.Count; i++)
                {
                    uint textureIndex = temp_textureIndices[i];
                    mesh_tmp.AddTextureVertices(temp_textureVertices[(int)textureIndex - 1]);
                }
                for (int i = 0; i < temp_normalsIndices.Count; i++)
                {
                    uint normalIndex = temp_normalsIndices[i];
                    mesh_tmp.AddNormals(temp_normals[(int)normalIndex - 1]);
                }
                mesh_tmp.setName(temp_name[mesh_created]);

                //Material
                if (usemtl)
                {

                    List<Material> mtl = materials_dict[material_library];
                    for (int i = 0; i < mtl.Count; i++)
                    {
                        if (mtl[i].Name == current_materialsName)
                        {
                            mesh_tmp.setMaterial(mtl[i]);
                        }
                    }
                }
                else
                {
                    List<Material> mtl = materials_dict["Default"];
                    for (int i = 0; i < mtl.Count; i++)
                    {
                        if (mtl[i].Name == "Default")
                        {
                            mesh_tmp.setMaterial(mtl[i]);
                        }
                    }
                }


                if (mesh_count == 1)
                {
                    mesh = mesh_tmp;
                }
                else
                {
                    mesh.child.Add(mesh_tmp);
                }

                mesh_created++;
            }
            return mesh;
        }
        public List<Material> LoadMtlFile(string path)
        {
            Console.WriteLine("Load MTL file");
            List <Material> materials = new List<Material>();
            List<string> name = new List<string>();
            List<float> shininess = new List<float>();
            List<Vector3> ambient = new List<Vector3>();
            List<Vector3> diffuse = new List<Vector3>();
            List<Vector3> specular = new List<Vector3>();
            List<float> alpha = new List<float>();
            List<string> map_kd = new List<string>();
            List<string> map_ka = new List<string>();

            //komputer ngecek, apakah file bisa diopen atau tidak
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Unable to open /" + path + "/, does not exist.");
            }
            //lanjut ke sini
            using (StreamReader streamReader = new StreamReader(path))
            {
                while (!streamReader.EndOfStream)
                {
                    List<string> words = new List<string>(streamReader.ReadLine().Split(' '));
                    words.RemoveAll(s => s == string.Empty);

                    if (words.Count == 0)
                        continue;

                    string type = words[0];

                    words.RemoveAt(0);
                    switch (type)
                    {
                        case "newmtl":
                            if(map_kd.Count < name.Count)
                            {
                                map_kd.Add("white.jpg");
                            }
                            if(map_ka.Count < name.Count)
                            {
                                map_ka.Add("white.jpg");
                            }
                            name.Add(words[0]);
                            break;
                        //Shininess
                        case "Ns":
                            shininess.Add(float.Parse(words[0]));
                            break;
                        case "Ka":
                            ambient.Add(new Vector3(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2])));
                            break;
                        case "Kd":
                            diffuse.Add(new Vector3(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2])));
                            break;
                        case "Ks":
                            specular.Add(new Vector3(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2])));
                            break;
                        case "d":
                            alpha.Add(float.Parse(words[0]));
                            break;
                        case "map_Kd":
                            map_kd.Add(words[0]);
                            break;
                        case "map_Ka":
                            map_ka.Add(words[0]);
                            break;
                        default:
                            break;
                    }
                }
            }

            if (map_kd.Count < name.Count)
            {
                map_kd.Add("white.jpg");
            }
            if (map_ka.Count < name.Count)
            {
                map_ka.Add("white.jpg");
            }

            Dictionary<string, Texture> texture_map_Kd = new Dictionary<string, Texture>();
            for(int i = 0; i < map_kd.Count; i++)
            {
                if(!texture_map_Kd.ContainsKey(map_kd[i]))
                {
                    Console.WriteLine("List of map_Kd key: " + map_kd[i]);
                    texture_map_Kd.Add(map_kd[i],
                        Texture.LoadFromFile("../../../Resources/" + map_kd[i]));
                }
            }

            Dictionary<string, Texture> texture_map_Ka = new Dictionary<string, Texture>();
            for (int i = 0; i < map_ka.Count; i++)
            {
                if (!texture_map_Ka.ContainsKey(map_ka[i]))
                {
                    texture_map_Ka.Add(map_ka[i],
                        Texture.LoadFromFile("../../../Resources/" + map_ka[i]));
                }
            }

            for (int i = 0; i < name.Count; i++)
            {
                materials.Add(new Material(name[i], shininess[i], ambient[i], diffuse[i], specular[i], 
                    alpha[i], texture_map_Kd[map_kd[i]], texture_map_Ka[map_ka[i]]));
            }

            return materials;
        }

        //CubeMap
        public void CreateCubeMap()
        {
            string[] skyboxPath =
            {
                "../../../Resources/Skybox/danvilesky.png",
                "../../../Resources/Skybox/danvilesky.png",
                "../../../Resources/Skybox/danvilesky.png",
                "../../../Resources/Skybox/danvilesky.png",
                "../../../Resources/Skybox/danvilesky.png",
                "../../../Resources/Skybox/danvilesky.png",
            };

            GL.GenTextures(1, out cubemap);
            GL.BindTexture(TextureTarget.TextureCubeMap, cubemap);
            Console.WriteLine("Cubemap: " + cubemap);
            for (int i = 0; i < skyboxPath.Length; i++)
            {
                using (var image = new Bitmap(skyboxPath[i]))
                {
                    Console.WriteLine(skyboxPath[i] + " LOADED");
                    
                    var data = image.LockBits(
                        new Rectangle(0, 0, image.Width, image.Height),
                        ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i,
                        0,
                        PixelInternalFormat.Rgb,
                        1280,
                        1280,
                        0,
                        PixelFormat.Bgra,
                        PixelType.UnsignedByte,
                        data.Scan0);
                }
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
            }
        }
        //Animation
        public void LampRevolution()
        {
            //lights[0].Position = lamp0.getTransform().ExtractTranslation();
            //lamp0.rotate(0f, 1.0f, 0.0f);
            //lights[1].Position = lamp1.getTransform().ExtractTranslation();
            //lamp1.rotate(0f, -1f, 0.0f);
            ////lights[2].Position = lamp2.getTransform().ExtractTranslation();
            ////lamp2.rotate(1f, 0f, 0f);
        }
    }
}