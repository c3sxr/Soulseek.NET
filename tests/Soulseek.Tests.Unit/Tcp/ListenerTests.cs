﻿// <copyright file="ListenerTests.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Tests.Unit.Tcp
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Tcp;
    using Xunit;

    public class ListenerTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates properly"), AutoData]
        internal void Instantiates_Properly(int port, ConnectionOptions options)
        {
            var l = new Listener(port, options);

            Assert.Equal(port, l.Port);
            Assert.Equal(options, l.ConnectionOptions);

            Assert.False(l.Listening);
        }

        [Trait("Category", "Start")]
        [Theory(DisplayName = "Start starts listening"), AutoData]
        internal void Start_Starts_Listening(int port, ConnectionOptions options)
        {
            var l = new Listener(port, options);

            var first = l.Listening;

            l.Start();

            Assert.False(first);
            Assert.True(l.Listening);
        }

        [Trait("Category", "Stop")]
        [Theory(DisplayName = "Stop stops listening"), AutoData]
        internal void Stop_Stops_Listening(int port, ConnectionOptions options)
        {
            var l = new Listener(port, options);

            l.Start();

            var first = l.Listening;

            l.Stop();

            Assert.True(first);
            Assert.False(l.Listening);
        }
    }
}
