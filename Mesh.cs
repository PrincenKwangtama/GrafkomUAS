using LearnOpenTK.Common;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;

namespace GrafkomUAS
{
    class Mesh
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector3> textureVertices = new List<Vector3>();

        Material material;

        string name;
        int _vbo;
        int _vao;

        Matrix4 transform;
        Matrix4 view;
        Matrix4 projection;

        Shader _shader;
        Shader _depthShader;
        //
        bool blinn = false;
        bool gamma = false;
        private Texture _diffuseMap;
        private Texture _specularMap;


        //Hirarki pada parent
        public List<Mesh> child = new List<Mesh>();
        public Mesh()
        {

        }
        public Mesh(string vertPath, string fragPath)
        {
            _shader = new Shader(vertPath, fragPath);
        }
        public void setupObject(float sizeX, float sizeY)
        {
            //Inisialisasi Transformasi
            transform = Matrix4.Identity;

            //Vertices
            //Inisialiasi VBO
            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * Vector3.SizeInBytes,
                vertices.ToArray(), BufferUsageHint.StaticDraw);

            //Inisialisasi VAO
            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);
            var vertexLocation = _shader.GetAttribLocation("aPosition");
            GL.EnableVertexAttribArray(vertexLocation);
            GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

            //Normals
            //Inisialiasi VBO
            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            if (normals.Count < vertices.Count)
            {
                GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * Vector3.SizeInBytes,
               vertices.ToArray(), BufferUsageHint.StaticDraw);
            }
            else
            {
                GL.BufferData(BufferTarget.ArrayBuffer, normals.Count * Vector3.SizeInBytes,
                normals.ToArray(), BufferUsageHint.StaticDraw);
            }


            var normalLocation = _shader.GetAttribLocation("aNormal");
            GL.EnableVertexAttribArray(normalLocation);
            GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

            //Textures
            //Inisialiasi VBO
            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            if (textureVertices.Count < vertices.Count)
            {
                GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * Vector3.SizeInBytes,
               vertices.ToArray(), BufferUsageHint.StaticDraw);
            }
            else
            {
                GL.BufferData(BufferTarget.ArrayBuffer, textureVertices.Count * Vector3.SizeInBytes,
                textureVertices.ToArray(), BufferUsageHint.StaticDraw);
            }
            var texCoordLocation = _shader.GetAttribLocation("aTexCoords");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);




            //Camera
            view = Matrix4.CreateTranslation(1.0f, 0.0f, 3.0f);
            projection = Matrix4.CreateOrthographic(800, 600, 0.1f, 100.0f);

            //Diffuse and specular map
            if (material != null)
            {
                _diffuseMap = material.Map_Kd;
                _specularMap = material.Map_Ka;
            }

            foreach (var meshobj in child)
            {
                meshobj.setupObject(sizeX, sizeY);
            }
        }
        public void render(Camera _camera, List<Light> lights)
        {
            //render itu akan selalu terpanggil setiap frame
            GL.BindVertexArray(_vao);
            if (material != null)
            {
                _diffuseMap.Use(TextureUnit.Texture0);
                _specularMap.Use(TextureUnit.Texture1);
            }

            _shader.Use();
            _shader.SetMatrix4("transform", transform);
            _shader.SetMatrix4("view", _camera.GetViewMatrix());
            _shader.SetMatrix4("projection", _camera.GetProjectionMatrix());
            _shader.SetVector3("viewPos", _camera.Position);
            ////material settings
            if (material != null)
            {
                _shader.SetInt("material.diffuse_sampler", 0);
                _shader.SetInt("material.specular_sampler", 1);
                _shader.SetVector3("material.ambient", material.Ambient);
                _shader.SetVector3("material.diffuse", material.Diffuse);
                _shader.SetVector3("material.specular", material.Specular);
                _shader.SetFloat("material.shininess", material.Shininess);
            }
            else
            {
                _shader.SetInt("material.diffuse_sampler", 0);
                _shader.SetInt("material.specular_sampler", 1);
                _shader.SetVector3("material.ambient", new Vector3(0.1f));
                _shader.SetVector3("material.diffuse", new Vector3(1.0f));
                _shader.SetVector3("material.specular", new Vector3(1.0f));
                _shader.SetFloat("material.shininess", 128.0f);
            }

            //Multiple Lights 
            for (int i = 0; i < lights.Count; i++)
            {
                PointLight pointLight = (PointLight)lights[i];


                //Process Lighting Shader
                _shader.SetVector3("lights[" + i + "].position", pointLight.Position);
                //_shader.SetVector3("lights[" + i + "].direction", new Vector3(-0.2f, -1.0f, -0.3f));
                _shader.SetVector3("lights[" + i + "].ambient", pointLight.Ambient);
                _shader.SetVector3("lights[" + i + "].diffuse", pointLight.Diffuse);
                _shader.SetVector3("lights[" + i + "].specular", pointLight.Specular);
                _shader.SetFloat("lights[" + i + "].linear", pointLight.Linear);
                _shader.SetFloat("lights[" + i + "].constant", pointLight.Constant);
                _shader.SetFloat("lights[" + i + "].quadratic", pointLight.Quadratic);
                //Use F1 key to toggle, default phong
                _shader.SetBool("blinn", blinn);
                _shader.SetBool("gamma", gamma);


            }

            GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);

            //ada disini
            foreach (var meshobj in child)
            {
                meshobj.render(_camera, lights);
            }
        }
        public void calculateDepthRender(Camera _camera, Light light, int i)
        {
            GL.BindVertexArray(_vao);
            if (material != null)
            {
                _diffuseMap.Use(TextureUnit.Texture0);
                //_specularMap.Use(TextureUnit.Texture1);
            }
            _depthShader.SetMatrix4("model", transform);
            GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);

            foreach (var meshobj in child)
            {
                meshobj.calculateDepthRender(_camera, light, i);
            }
        }
        public void calculateTextureRender(Camera _camera, Light light, int i)
        {
            //render itu akan selalu terpanggil setiap frame
            GL.BindVertexArray(_vao);
            if (material != null)
            {
                _diffuseMap.Use(TextureUnit.Texture0);
                _specularMap.Use(TextureUnit.Texture1);
            }

            //_shader.Use();
            _shader.SetMatrix4("transform", transform);
            _shader.SetMatrix4("view", _camera.GetViewMatrix());
            _shader.SetMatrix4("projection", _camera.GetProjectionMatrix());
            _shader.SetVector3("viewPos", _camera.Position);
            ////material settings
            if (material != null)
            {
                _shader.SetInt("material.diffuse_sampler", 0);
                _shader.SetInt("material.specular_sampler", 1);
                _shader.SetVector3("material.ambient", material.Ambient);
                _shader.SetVector3("material.diffuse", material.Diffuse);
                _shader.SetVector3("material.specular", material.Specular);
                _shader.SetFloat("material.shininess", material.Shininess);
            }
            else
            {
                _shader.SetInt("material.diffuse_sampler", 0);
                _shader.SetInt("material.specular_sampler", 1);
                _shader.SetVector3("material.ambient", new Vector3(0.1f));
                _shader.SetVector3("material.diffuse", new Vector3(1.0f));
                _shader.SetVector3("material.specular", new Vector3(1.0f));
                _shader.SetFloat("material.shininess", 128.0f);
            }
            //Process Lighting Shader
            _shader.SetVector3("lights[" + i + "].position", light.Position);


            _shader.SetVector3("lights[" + i + "].ambient", light.Ambient);
            _shader.SetVector3("lights[" + i + "].diffuse", light.Diffuse);
            _shader.SetVector3("lights[" + i + "].specular", light.Specular);
            if (light.GetType() == typeof(PointLight))
            {
                //Point Light Calculation
                //Console.WriteLine("Point Light");
                PointLight pointLight = (PointLight)light;
                _shader.SetBool("lights[" + i + "].useDirection", false);
                _shader.SetFloat("lights[" + i + "].linear", pointLight.Linear);
                _shader.SetFloat("lights[" + i + "].constant", pointLight.Constant);
                _shader.SetFloat("lights[" + i + "].quadratic", pointLight.Quadratic);
            }
            else if (light.GetType() == typeof(DirectionLight))
            {
                //Directional Light Calculation
                DirectionLight directionLight = (DirectionLight)light;
                //Console.WriteLine("Directional Light");
                _shader.SetBool("lights[" + i + "].useDirection", true);
                _shader.SetVector3("lights[" + i + "].direction", directionLight.Direction);

            }
            //Use F1 key to toggle, default phong
            //Option
            _shader.SetBool("blinn", blinn);
            _shader.SetBool("gamma", gamma);

            GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);

            //ada disini
            foreach (var meshobj in child)
            {
                meshobj.calculateTextureRender(_camera, light, i);
            }
        }

        //TRANSFORMASI
        public Matrix4 getTransform()
        {
            return transform;
        }
        public void rotate(float angleX, float angleY, float angleZ)
        {
            //rotate parentnya
            //sumbu X
            transform = transform * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(angleX));
            //sumbu Y
            transform = transform * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(angleY));
            //sumbu Z
            transform = transform * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(angleZ));
            //rotate childnya
            foreach (var meshobj in child)
            {
                meshobj.rotate(angleX, angleY, angleZ);
            }
        }
        public void scale(float scale)
        {
            transform = transform * Matrix4.CreateScale(scale);
            foreach (var meshobj in child)
            {
                meshobj.scale(scale);
            }
        }
        public void translate(Vector3 translation)
        {
            transform = transform * Matrix4.CreateTranslation(translation);
            foreach (var meshobj in child)
            {
                meshobj.translate(translation);
            }
        }

        //SETTER GETTER
        public List<Vector3> getVertices()
        {
            return vertices;
        }
        public void setVertices(List<Vector3> vertices)
        {
            this.vertices = vertices;
        }
        public List<Vector3> getNormals()
        {
            return normals;
        }
        public void setNormals(List<Vector3> normals)
        {
            this.normals = normals;
        }
        public List<Vector3> getTextureVertices()
        {
            return textureVertices;
        }
        public void setTextureVertices(List<Vector3> textureVertices)
        {
            this.textureVertices = textureVertices;
        }
        public void setMaterial(Material material)
        {
            this.material = material;
        }
        public Material getMaterial()
        {
            return material;
        }
        public void setName(string name)
        {
            this.name = name;
        }
        public string getName()
        {
            return name;
        }
        public void AddVertices(Vector3 vec)
        {
            vertices.Add(vec);
        }
        public void AddTextureVertices(Vector3 vec)
        {
            textureVertices.Add(vec);
        }
        public void AddNormals(Vector3 vec)
        {
            normals.Add(vec);
        }

        public int getVertexBufferObject()
        {
            return _vbo;
        }

        public int getVertexArrayObject()
        {
            return _vao;
        }

        public void setShader(Shader shader)
        {
            this._shader = shader;
        }
        public Shader getShader()
        {
            return _shader;
        }

        public void setDepthShader(Shader shader)
        {
            this._depthShader = shader;
        }
        public Shader getDepthShader()
        {
            return _depthShader;
        }

        public void setDiffuseMap(Texture tex)
        {
            _diffuseMap = tex;
            //Give all the diffuse map
            foreach (var meshobj in child)
            {
                meshobj.setDiffuseMap(tex);
            }
        }

        public void setDiffuseMap(string filepath)
        {
            _diffuseMap = Texture.LoadFromFile(filepath);
            //Give all the specular map
            foreach (var meshobj in child)
            {
                meshobj.setDiffuseMap(filepath);
            }
        }

        public void setSpecularMap(string filepath)
        {
            _specularMap = Texture.LoadFromFile(filepath);
            //Give all the specular map
            foreach (var meshobj in child)
            {
                meshobj.setSpecularMap(filepath);
            }
        }

        public void setBlinn(bool b)
        {
            blinn = b;
        }

        public bool getBlinn()
        {
            return blinn;
        }
        public void setGamma(bool b)
        {
            gamma = b;
        }

        public bool getGamma()
        {
            return gamma;
        }
    }
}
