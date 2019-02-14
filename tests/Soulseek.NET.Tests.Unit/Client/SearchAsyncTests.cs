﻿// <copyright file="SearchAsyncTests.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Unit.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.NET.Exceptions;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Messages;
    using Soulseek.NET.Messaging.Tcp;
    using Xunit;

    public class SearchAsyncTests
    {
        [Trait("Category", "SearchAsync")]
        [Fact(DisplayName = "SearchAsync throws InvalidOperationException when not connected")]
        public async Task SearchAsync_Throws_InvalidOperationException_When_Not_Connected()
        {
            var s = new SoulseekClient();

            var ex = await Record.ExceptionAsync(async () => await s.SearchAsync("foo", 0));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("Connected", ex.Message, StringComparison.InvariantCultureIgnoreCase);
        }

        [Trait("Category", "SearchAsync")]
        [Fact(DisplayName = "SearchAsync throws InvalidOperationException when not logged in")]
        public async Task SearchAsync_Throws_InvalidOperationException_When_Not_Logged_In()
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected);

            var ex = await Record.ExceptionAsync(async () => await s.SearchAsync("foo", 0));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("logged in", ex.Message, StringComparison.InvariantCultureIgnoreCase);
        }

        [Trait("Category", "SearchAsync")]
        [Theory(DisplayName = "SearchAsync throws ArgumentException given bad search text")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData(" ")]
        public async Task SearchAsync_Throws_ArgumentException_Given_Bad_Search_Text(string search)
        {
            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var ex = await Record.ExceptionAsync(async () => await s.SearchAsync(search, 0));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("searchText", ((ArgumentException)ex).ParamName);
        }

        [Trait("Category", "SearchAsync")]
        [Theory(DisplayName = "SearchAsync throws ArgumentException given a token in use"), AutoData]
        public async Task SearchAsync_Throws_ArgumentException_Given_A_Token_In_Use(string text, int token)
        {
            var dict = new ConcurrentDictionary<int, Search>();
            dict.TryAdd(token, new Search(text, token, new SearchOptions()));

            var s = new SoulseekClient();
            s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);
            s.SetProperty("ActiveSearches", dict);

            var ex = await Record.ExceptionAsync(async () => await s.SearchAsync(text, token));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("token", ((ArgumentException)ex).ParamName);
        }

        [Trait("Category", "SearchInternalAsync")]
        [Theory(DisplayName = "SearchInternalAsync returns completed search"), AutoData]
        public async Task SearchInternalAsync_Returns_Completed_Search(string searchText, int token)
        {
            var options = new SearchOptions();
            var response = new SearchResponse("username", token, 1, 1, 1, 0, new List<File>() { new File(1, "foo", 1, "bar", 0) });

            var search = new Search(searchText, token, options);
            search.State = SearchStates.InProgress;
            search.SetProperty("ResponseList", new List<SearchResponse>() { response });

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.WaitIndefinitely<Search>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromResult(search));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>()))
                .Returns(Task.CompletedTask);

            var s = new SoulseekClient("127.0.0.1", 1, messageWaiter: waiter.Object, serverConnection: conn.Object);

            IReadOnlyCollection<SearchResponse> responses = null;
            var ex = await Record.ExceptionAsync(async () => responses = await s.InvokeMethod<Task<IReadOnlyCollection<SearchResponse>>>("SearchInternalAsync", searchText, token, options, null, true));

            var res = responses.ToList()[0];

            Assert.Null(ex);

            Assert.Equal(response.Username, res.Username);
            Assert.Equal(response.Token, res.Token);
        }

        [Trait("Category", "SearchInternalAsync")]
        [Theory(DisplayName = "SearchInternalAsync adds search to ActiveSearches"), AutoData]
        public async Task SearchInternalAsync_Adds_Search_To_ActiveSearches(string searchText, int token)
        {
            var options = new SearchOptions();
            var response = new SearchResponse("username", token, 1, 1, 1, 0, new List<File>() { new File(1, "foo", 1, "bar", 0) });

            var search = new Search(searchText, token, options);
            search.State = SearchStates.InProgress;
            search.SetProperty("ResponseList", new List<SearchResponse>() { response });

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.WaitIndefinitely<Search>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromResult(search));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>()))
                .Returns(Task.CompletedTask);

            var s = new SoulseekClient("127.0.0.1", 1, messageWaiter: waiter.Object, serverConnection: conn.Object);

            await s.InvokeMethod<Task<IReadOnlyCollection<SearchResponse>>>("SearchInternalAsync", searchText, token, options, null, true);

            var active = s.GetProperty<ConcurrentDictionary<int, Search>>("ActiveSearches");

            Assert.Single(active);
            Assert.True(active.ContainsKey(token));
            Assert.Equal(token, active[token].Token);
        }

        [Trait("Category", "SearchInternalAsync")]
        [Theory(DisplayName = "SearchInternalAsync returns default when waitForCompletion is false"), AutoData]
        public async Task SearchInternalAsync_Returns_Default_When_WaitForCompletion_Is_False(string searchText, int token)
        {
            var options = new SearchOptions();
            var response = new SearchResponse("username", token, 1, 1, 1, 0, new List<File>() { new File(1, "foo", 1, "bar", 0) });

            var search = new Search(searchText, token, options);
            search.State = SearchStates.InProgress;
            search.SetProperty("ResponseList", new List<SearchResponse>() { response });

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.WaitIndefinitely<Search>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromResult(search));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>()))
                .Returns(Task.CompletedTask);

            var s = new SoulseekClient("127.0.0.1", 1, messageWaiter: waiter.Object, serverConnection: conn.Object);

            var result = await s.InvokeMethod<Task<IReadOnlyCollection<SearchResponse>>>("SearchInternalAsync", searchText, token, options, null, false);

            Assert.Null(result);
        }

        [Trait("Category", "SearchInternalAsync")]
        [Theory(DisplayName = "SearchInternalAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task SearchInternalAsync_Throws_OperationCanceledException_On_Cancellation(string searchText, int token)
        {
            var options = new SearchOptions();
            var response = new SearchResponse("username", token, 1, 1, 1, 0, new List<File>() { new File(1, "foo", 1, "bar", 0) });

            var search = new Search(searchText, token, options);
            search.State = SearchStates.InProgress;
            search.SetProperty("ResponseList", new List<SearchResponse>() { response });

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.WaitIndefinitely<Search>(It.IsAny<WaitKey>(), null))
                .Returns(Task.FromException<Search>(new OperationCanceledException()));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteMessageAsync(It.IsAny<Message>()))
                .Returns(Task.CompletedTask);

            var s = new SoulseekClient("127.0.0.1", 1, messageWaiter: waiter.Object, serverConnection: conn.Object);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task<IReadOnlyCollection<SearchResponse>>>("SearchInternalAsync", searchText, token, options, null, true));

            Assert.NotNull(ex);
            Assert.IsType<SearchException>(ex);
            Assert.IsType<OperationCanceledException>(ex.InnerException);
        }
    }
}
