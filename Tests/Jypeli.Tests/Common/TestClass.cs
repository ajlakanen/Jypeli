﻿using Jypeli.Tests.Game;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jypeli.Tests.Common
{
    public abstract class TestClass
    {
        protected MockGame game;
        [SetUp]
        public void Setup()
        {
            game = new MockGame();
        }

        [TearDown]
        public void TearDown()
        {
            game.Exit();
            game.Dispose();
            game = null;
        }

    }
}
