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


using System;
using Jypeli.Physics;

namespace Jypeli
{
    /// <summary>
    /// A collision ignorer that uses group numbers to do collision ignoring.
    /// If 2 objects are members of the same group then they will not collide.
    /// </summary>
    public class GroupIgnorer : Ignorer
    {
        GroupCollection groups;
        public GroupIgnorer()
        {
            this.groups = new GroupCollection();
        }
        protected GroupIgnorer(GroupIgnorer copy)
            : base(copy)
        {
            this.groups = new GroupCollection(copy.groups);
        }
        public override bool BothNeeded
        {
            get { return false; }
        }
        public GroupCollection Groups { get { return groups; } }
        public bool CanCollide(GroupIgnorer other)
        {
            if (other == null) { throw new ArgumentNullException("other"); }
            return CanCollideInternal(other);
        }
        private bool CanCollideInternal(GroupIgnorer other)
        {
            return !GroupCollection.Intersect(groups, other.groups);
        }
        public override bool CanCollide( IPhysicsBody thisBody, IPhysicsBody otherBody, Ignorer other )
        {
            GroupIgnorer value = other as GroupIgnorer;
            return
                value == null ||
                CanCollideInternal(value);
        }
        public virtual object Clone()
        {
            return new GroupIgnorer(this);
        }
    }
}