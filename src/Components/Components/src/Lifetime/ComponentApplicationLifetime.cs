// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Manages the lifetime of a component application.
    /// </summary>
    public class ComponentApplicationLifetime
    {
        private bool _stateIsPersisted;
        private List<Func<Task>> _pauseCallbacks = new();
        private readonly Dictionary<string, byte[]> _currentState = new();

        /// <summary>
        /// Initializes a new instance of <see cref="ComponentApplicationLifetime"/>.
        /// </summary>
        public ComponentApplicationLifetime()
        {
            State = new ComponentApplicationState(_currentState, _pauseCallbacks);
        }

        /// <summary>
        /// Gets the <see cref="ComponentApplicationState"/> associated with the <see cref="ComponentApplicationLifetime"/>.
        /// </summary>
        public ComponentApplicationState State { get; }

        /// <summary>
        /// Restores the component application state from the given <see cref="IComponentApplicationStateStore"/>.
        /// </summary>
        /// <param name="store">The <see cref="IComponentApplicationStateStore"/> to restore the application state from.</param>
        /// <returns>A <see cref="Task"/> that will complete when the state has been restored.</returns>
        public Task RestoreStateAsync(IComponentApplicationStateStore store)
        {
            var data = store.GetPersistedState();
            State.InitializeExistingState(data);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Persists the component application state into the given <see cref="IComponentApplicationStateStore"/>.
        /// </summary>
        /// <param name="store">The <see cref="IComponentApplicationStateStore"/> to restore the application state from.</param>
        /// <param name="renderer">The <see cref="Renderer"/> that components are being rendered.</param>
        /// <returns>A <see cref="Task"/> that will complete when the state has been restored.</returns>
        public Task PersistStateAsync(IComponentApplicationStateStore store, Renderer renderer)
        {
            if (State == null)
            {
                throw new InvalidOperationException("ComponentApplicationLifetimeNotInitialized.");
            }
            if (_stateIsPersisted)
            {
                throw new InvalidOperationException("State already persisted.");
            }
            if (_pauseCallbacks.Count > 0)
            {
                throw new InvalidOperationException("Not all registered instances have been paused.");
            }
            _stateIsPersisted = true;

            return renderer.Dispatcher.InvokeAsync(PauseAndPersistState);

            async Task PauseAndPersistState()
            {
                await PauseAsync();

                var data = new ReadOnlyDictionary<string, byte[]>(_currentState);
                await store.PersistStateAsync(data);
            }
        }

        internal async Task PauseAsync()
        {
            foreach (var callback in _pauseCallbacks)
            {
                await callback();
            }
        }

        internal void AddRegistration(Func<Task> callback)
        {
            _pauseCallbacks.Add(callback);
        }
    }
}