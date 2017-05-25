﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PointSelectionCommand.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Provides a command that shows a rectangle when the mouse is dragged and raises an event returning the models contained in the rectangle
//   when the mouse button is released.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HelixToolkit.Wpf
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    /// <summary>
    /// Provides a command that raises an event returning the hit models at the mouse location when the mouse button is clicked.
    /// </summary>
    public class PointSelectionCommand : SelectionCommand
    {
        /// <summary>
        /// The position
        /// </summary>
        private Point position;

        /// <summary>
        /// Initializes a new instance of the <see cref="PointSelectionCommand" /> class.
        /// </summary>
        /// <param name="viewport">The viewport.</param>
        /// <param name="eventHandler">The selection event handler.</param>
        public PointSelectionCommand(Viewport3D viewport, EventHandler<ModelsSelectedEventArgs> eventHandler)
            : base(viewport, eventHandler)
        {
        }

        /// <summary>
        /// Occurs when the manipulation is started.
        /// </summary>
        /// <param name="e">The <see cref="ManipulationEventArgs"/> instance containing the event data.</param>
        protected override void Started(ManipulationEventArgs e)
        {
            base.Started(e);
            this.position = e.CurrentPosition;

            var selectedModels = this.Viewport.FindHits(this.position).Select(hit => hit.Model).ToList();
            this.OnModelsSelected(new ModelsSelectedByPointEventArgs(selectedModels, this.position));
        }

        /// <summary>
        /// The customized complete operation when the manipulation is completed.
        /// </summary>
        /// <param name="e">
        /// The <see cref="ManipulationEventArgs"/> instance containing the event data.
        /// </param>
        protected override void Completed(ManipulationEventArgs e)
        {
            // do not raise event here
        }

        /// <summary>
        /// Gets the cursor for the gesture.
        /// </summary>
        /// <returns>A cursor.</returns>
        protected override Cursor GetCursor()
        {
            return Cursors.Arrow;
        }
    }
}