/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2021, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;

namespace magic.lambda.scheduler.utilities
{
    /// <summary>
    /// Common interface for repetition pattern instances.
    /// </summary>
    public interface IPattern
    {
        /// <summary>
        /// Calculates the next date and time for when the task is to be executed.
        /// </summary>
        /// <returns>Date and time when task should be executed.</returns>
        DateTime Next();

        /// <summary>
        /// Returns the string representation of the repetition pattern.
        /// </summary>
        /// <value>String representation for repetition pattern.</value>
        string Value { get; }
    }
}
