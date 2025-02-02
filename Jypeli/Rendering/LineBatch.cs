﻿using System;
using System.Diagnostics;
using Jypeli.Rendering;

using Matrix = System.Numerics.Matrix4x4;
using Vector3 = System.Numerics.Vector3;

namespace Jypeli
{
    internal class LineBatch
    {
        VertexPositionColorTexture[] vertexBuffer;
        IShader shader;
        Matrix matrix;
        int iVertexBuffer = 0;
        bool beginHasBeenCalled = false;
        public bool LightingEnabled = true;

        internal void Initialize()
        {
            int vertexBufferSize = Game.GraphicsDevice.BufferSize;
            vertexBuffer = new VertexPositionColorTexture[vertexBufferSize];
           
            shader = Graphics.BasicColorShader;
        }

        public void Begin( ref Matrix matrix )
        {
            Debug.Assert( !beginHasBeenCalled );
            beginHasBeenCalled = true;

            this.matrix = matrix;
            iVertexBuffer = 0;
        }

        public void End()
        {
            Debug.Assert( beginHasBeenCalled );
            Flush();
            beginHasBeenCalled = false;
        }

        private void Flush()
        {
            if ( iVertexBuffer > 0 )
            {
                shader.Use();

                shader.SetUniform("world", matrix * Graphics.ViewProjectionMatrix);

                Game.GraphicsDevice.DrawPrimitives(PrimitiveType.OpenGLLines, vertexBuffer, (uint)iVertexBuffer);
            }

            iVertexBuffer = 0;
        }

        public void Draw(Vector startPoint, Vector endPoint, Color color)
        {
            if ((iVertexBuffer + 2) > vertexBuffer.Length)
            {
                Flush();
            }
            vertexBuffer[iVertexBuffer++] = new VertexPositionColorTexture(new Vector3((float)startPoint.X, (float)startPoint.Y, 0f),color, Vector.Zero);
            vertexBuffer[iVertexBuffer++] = new VertexPositionColorTexture(new Vector3((float)endPoint.X, (float)endPoint.Y, 0f), color, Vector.Zero);
        }
    }
}
