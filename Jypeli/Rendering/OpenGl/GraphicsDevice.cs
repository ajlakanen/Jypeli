﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SixLabors.ImageSharp.PixelFormats;

namespace Jypeli.Rendering.OpenGl
{
    /// <summary>
    /// OpenGL renderöintilaite
    /// </summary>
    public unsafe class GraphicsDevice : IGraphicsDevice
    {
        /// <inheritdoc/>
        public GL Gl;

        private BufferObject<VertexPositionColorTexture> Vbo;
        private BufferObject<uint> Ebo;
        private VertexArrayObject<VertexPositionColorTexture, uint> Vao;

        private BasicLightRenderer bl;

        /// <inheritdoc/>
        public int BufferSize { get; } = 16384;
        private uint[] Indices;
        private VertexPositionColorTexture[] Vertices;

        /// <inheritdoc/>
        public string Name { get; internal set; }
        /// <inheritdoc/>
        public string Version { get => throw new NotImplementedException(); }

        private IRenderTarget SelectedRendertarget;

        /// <inheritdoc/>
        public GraphicsDevice(IView window)
        {
            Indices = new uint[BufferSize * 2];
            Vertices = new VertexPositionColorTexture[BufferSize];

            Create(window);
        }

        /// <summary>
        /// Alustaa näyttökortin käyttöön
        /// </summary>
        /// <param name="window">Pelin ikkuna</param>
        public void Create(IView window)
        {
            Gl = GL.GetApi(window);
            try
            {
                Gl.DebugMessageCallback(PrintError, null);
            }
            catch
            {
                Debug.WriteLine("DebugMessageCallback not available");
            }

            Name = window.API.API.ToString();
            Ebo = new BufferObject<uint>(Gl, Indices, BufferTargetARB.ElementArrayBuffer);
            Vbo = new BufferObject<VertexPositionColorTexture>(Gl, Vertices, BufferTargetARB.ArrayBuffer);
            Vao = new VertexArrayObject<VertexPositionColorTexture, uint>(Gl, Vbo, Ebo);

            Vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, (uint)sizeof(VertexPositionColorTexture), 0);
            Vao.VertexAttributePointer(1, 4, VertexAttribPointerType.Float, (uint)sizeof(VertexPositionColorTexture), 12);
            Vao.VertexAttributePointer(2, 2, VertexAttribPointerType.Float, (uint)sizeof(VertexPositionColorTexture), 28);

            bl = new BasicLightRenderer(this);
        }

        private void PrintError(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userParam)
        {
            Debug.WriteLine($"ERROR {source}: {type}, {id}, {severity}, {length}, {message}, {userParam}");
        }

        /// <inheritdoc/>
        public IShader CreateShader(string vert, string frag)
        {
            return new Shader(Gl, vert, frag);
        }

        /// <inheritdoc/>
        public IShader CreateShaderFromInternal(string vertPath, string fragPath)
        {
            return CreateShader(Game.ResourceContent.LoadInternalText($"Shaders.{Name}.{vertPath}"), Game.ResourceContent.LoadInternalText($"Shaders.{Name}.{fragPath}"));
        }

        /// <inheritdoc/>
        public void DrawIndexedPrimitives(PrimitiveType primitivetype, VertexPositionColorTexture[] vertexBuffer, uint numIndices, uint[] indexBuffer)
        {
            Gl.Enable(GLEnum.Blend);
            Gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

            Ebo.UpdateBuffer(0, indexBuffer);
            Vbo.UpdateBuffer(0, vertexBuffer);

            Vao.Bind();

            Gl.DrawElements((GLEnum)primitivetype, numIndices, DrawElementsType.UnsignedInt, null);
        }

        /// <inheritdoc/>
        public void DrawPrimitives(PrimitiveType primitivetype, VertexPositionColorTexture[] vertexBuffer, uint numIndices, bool normalized = false)
        {
            Gl.Enable(GLEnum.Blend);
            Gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

            Vbo.UpdateBuffer(0, vertexBuffer);

            Vao.Bind();

            Gl.DrawArrays((GLEnum)primitivetype, 0, numIndices);
        }

        /// <inheritdoc/>
        public void DrawPrimitivesInstanced(PrimitiveType primitivetype, VertexPositionColorTexture[] textureVertices, uint count, uint instanceCount, bool normalized = false)
        {
            Gl.DrawArraysInstanced((GLEnum)primitivetype, 0, count, instanceCount);
        }

        public void DrawLights(Matrix4x4 matrix)
        {
            if (Game.Lights.Count == 0)
                return;
            bl.Draw(matrix);

            SetRenderTarget(Game.Screen.RenderTarget);

            Graphics.LightPassTextureShader.Use();
            Graphics.LightPassTextureShader.SetUniform("world", Matrix4x4.Identity);

            Graphics.LightPassTextureShader.SetUniform("texture0", 0);
            Graphics.LightPassTextureShader.SetUniform("texture1", 1);

            Graphics.LightPassTextureShader.SetUniform("ambientLight", Game.Instance.Level.AmbientLight.ToNumerics());

            Game.Screen.RenderTarget.TextureSlot(0);
            Game.Screen.RenderTarget.BindTexture();

            BasicLightRenderer.RenderTarget.TextureSlot(1);
            BasicLightRenderer.RenderTarget.BindTexture();

            DrawPrimitives(PrimitiveType.OpenGlTriangles, Graphics.TextureVertices, 6, true);
        }

        /// <inheritdoc/>
        public void Clear(Color bgColor)
        {
            Gl.ClearColor(bgColor.RedComponent / 255f, bgColor.GreenComponent / 255f, bgColor.BlueComponent / 255f, bgColor.AlphaComponent / 255f);
            Gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
        }

        /// <inheritdoc/>
        public void SetRenderTarget(IRenderTarget renderTarget)
        {
            if (renderTarget is null)
                Gl.BindFramebuffer(GLEnum.Framebuffer, 0);
            else
                renderTarget.Bind();
            SelectedRendertarget = renderTarget;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Vbo.Dispose();
            Ebo.Dispose();
            Vao.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public IRenderTarget CreateRenderTarget(uint width, uint height)
        {
            return new RenderTarget(this, width, height);
        }

        /// <inheritdoc/>
        public void LoadImage(Image image)
        {
            fixed (void* data = &MemoryMarshal.GetReference(image.image.GetPixelRowSpan(0)))
            {
                image.handle = Gl.GenTexture();
                BindTexture(image);

                Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)image.Width, (uint)image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);

                GLEnum scaling = image.Scaling == ImageScaling.Linear ? GLEnum.Linear : GLEnum.Nearest;

                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)scaling);
                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)scaling);

                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
            }
        }

        /// <inheritdoc/>
        public void UpdateTextureData(Image image)
        {
            fixed (void* data = &MemoryMarshal.GetReference(image.image.GetPixelRowSpan(0)))
            {
                BindTexture(image);

                Gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (uint)image.Width, (uint)image.Height, PixelFormat.Rgba, PixelType.UnsignedByte, data);

                GLEnum scaling = image.Scaling == ImageScaling.Linear ? GLEnum.Linear : GLEnum.Nearest;

                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)scaling);
                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)scaling);
                Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.Repeat);
            }
        }

        /// <inheritdoc/>
        public void UpdateTextureScaling(Image image)
        {
            GLEnum scaling = image.Scaling == ImageScaling.Linear ? GLEnum.Linear : GLEnum.Nearest;
            BindTexture(image);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)scaling); // TODO: Entä jos halutaan vain muuttaa skaalausta, ilman kuvan datan muuttamista?
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)scaling);
        }

        /// <inheritdoc/>
        public void BindTexture(Image image)
        {
            // Jos kuvaa ei ole vielä viety näytönohjaimelle.
            if (image.handle == 0)
                LoadImage(image);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2D, image.handle);
        }

        /// <inheritdoc/>
        public void ResizeWindow(Vector newSize)
        {
            Gl.Viewport(new System.Drawing.Size((int)newSize.X, (int)newSize.Y));
        }

        /// <inheritdoc/>
        public void SetTextureToRepeat(Image image)
        {
            BindTexture(image);

            Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.Repeat);
            Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.Repeat);
        }

        /// <inheritdoc/>
        public void GetScreenContents(void* ptr)
        {
            if(SelectedRendertarget == null)
                Gl.ReadPixels(0, 0, (uint)Game.Screen.Width, (uint)Game.Screen.Height, GLEnum.Rgba, GLEnum.UnsignedByte, ptr);
            else
                Gl.ReadPixels(0, 0, (uint)SelectedRendertarget.Width, (uint)SelectedRendertarget.Height, GLEnum.Rgba, GLEnum.UnsignedByte, ptr);
        }

        /// <inheritdoc/>
        public void GetScreenContentsToImage(Image img)
        {
            img.image.TryGetSinglePixelSpan(out Span<Rgba32> ptr);
            fixed(void* p = ptr)
                GetScreenContents(p);
        }

        /// <inheritdoc/>
        public int GetMaxTextureSize()
        {
            return Gl.GetInteger(GLEnum.MaxTextureSize);
        }
    }
}
