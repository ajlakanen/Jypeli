#region MIT License
/*
 * Copyright (c) 2005-2008 Jonathan Mark Porter. http://physics2d.googlepages.com/
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to deal 
 * in the Software without restriction, including without limitation the rights to 
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of 
 * the Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be 
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
 * PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE 
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 */
#endregion

using Jypeli.Physics;

namespace Jypeli
{
    /// <summary>
    /// A collision ignorer that uses reference comparison. 
    /// All Bodies with the same instance of this ignorer then they will not collide.
    /// </summary>
    public class ObjectIgnorer : Ignorer
    {
        public override bool BothNeeded
        {
            get { return false; }
        }

        /// <summary>
        /// Voivatko kappaleet t�rm�t� toisiinsa
        /// </summary>
        /// <param name="thisBody"></param>
        /// <param name="otherBody"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool CanCollide( IPhysicsBody thisBody, IPhysicsBody otherBody, Ignorer other )
        {
            return other!= this;
        }
    }
}