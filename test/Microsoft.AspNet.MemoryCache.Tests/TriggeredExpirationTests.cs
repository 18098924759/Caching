﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.MemoryCache.Tests
{
    public class TriggeredExpirationTests
    {
        [Fact]
        public void AddTriggerRegisters()
        {
            var cache = new MemoryCache();
            string key = "myKey";
            var obj = new object();
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
            cache.Set(key, context =>
            {
                context.AddExpirationTrigger(trigger);
                return obj;
            });

            Assert.True(trigger.IsExpiredWasCalled);
            Assert.True(trigger.ActiveExpirationCallbacksWasCalled);
            Assert.NotNull(trigger.Registration);
            Assert.NotNull(trigger.Registration.RegisteredCallback);
            Assert.NotNull(trigger.Registration.RegisteredState);
            Assert.False(trigger.Registration.Disposed);
        }

        [Fact]
        public void AddLazyTriggerDoesntRegister()
        {
            var cache = new MemoryCache();
            string key = "myKey";
            var obj = new object();
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = false };
            cache.Set(key, context =>
            {
                context.AddExpirationTrigger(trigger);
                return obj;
            });

            Assert.True(trigger.IsExpiredWasCalled);
            Assert.True(trigger.ActiveExpirationCallbacksWasCalled);
            Assert.Null(trigger.Registration);
        }

        [Fact]
        public void FireTriggerRemovesItem()
        {
            var cache = new MemoryCache();
            string key = "myKey";
            var obj = new object();
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
            cache.Set(key, context =>
            {
                context.AddExpirationTrigger(trigger);
                return obj;
            });

            trigger.Fire();

            var found = cache.TryGetValue(key, out obj);
            Assert.False(found);
        }

        [Fact]
        public void ExpireLazyTriggerRemovesItemOnNextAccess()
        {
            var cache = new MemoryCache();
            string key = "myKey";
            var obj = new object();
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = false };
            cache.Set(key, context =>
            {
                context.AddExpirationTrigger(trigger);
                return obj;
            });
            var found = cache.TryGetValue(key, out obj);
            Assert.True(found);

            trigger.IsExpired = true;

            found = cache.TryGetValue(key, out obj);
            Assert.False(found);
        }

        [Fact]
        public void RemoveItemDisposesRegistration()
        {
            var cache = new MemoryCache();
            string key = "myKey";
            var obj = new object();
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
            cache.Set(key, context =>
            {
                context.AddExpirationTrigger(trigger);
                return obj;
            });
            cache.Remove(key);

            Assert.NotNull(trigger.Registration);
            Assert.True(trigger.Registration.Disposed);
        }

        [Fact]
        public void AddExpiredTriggerNeverCaches()
        {
            var cache = new MemoryCache();
            string key = "myKey";
            var obj = new object();
            var trigger = new TestTrigger() { IsExpired = true };
            var result = cache.Set(key, context =>
            {
                context.AddExpirationTrigger(trigger);
                return obj;
            });
            Assert.Same(obj, result); // The created item should be returned, but not cached.

            Assert.True(trigger.IsExpiredWasCalled);
            Assert.False(trigger.ActiveExpirationCallbacksWasCalled);
            Assert.Null(trigger.Registration);

            result = cache.Get(key);
            Assert.Null(result); // It wasn't cached
        }

        internal class TestTrigger() : IExpirationTrigger
        {
            private bool _isExpired;
            private bool _activeExpirationCallbacks;

            public bool IsExpired
            {
                get
                {
                    IsExpiredWasCalled = true;
                    return _isExpired;
                }
                set
                {
                    _isExpired = value;
                }
            }

            public bool IsExpiredWasCalled { get; set; }

            public bool ActiveExpirationCallbacks
            {
                get
                {
                    ActiveExpirationCallbacksWasCalled = true;
                    return _activeExpirationCallbacks;
                }
                set
                {
                    _activeExpirationCallbacks = value;
                }
            }

            public bool ActiveExpirationCallbacksWasCalled { get; set; }

            public TriggerCallbackRegistration Registration { get; set; }

            public IDisposable RegisterExpirationCallback(Action<object> callback, object state)
            {
                Registration = new TriggerCallbackRegistration()
                {
                    RegisteredCallback = callback,
                    RegisteredState = state,
                };
                return Registration;
            }

            public void Fire()
            {
                IsExpired = true;
                if (Registration != null && !Registration.Disposed)
                {
                    Registration.RegisteredCallback(Registration.RegisteredState);
                }
            }
        }

        public class TriggerCallbackRegistration : IDisposable
        {
            public Action<object> RegisteredCallback { get; set; }

            public object RegisteredState { get; set; }

            public bool Disposed { get; set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}