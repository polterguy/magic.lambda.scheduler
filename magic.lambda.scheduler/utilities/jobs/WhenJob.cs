﻿/*
 * Magic, Copyright(c) Thomas Hansen 2019, thomas@gaiasoul.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Linq;
using magic.node;

namespace magic.lambda.scheduler.utilities.jobs
{
    /// <summary>
    /// Class wrapping a single job, with its due date,
    /// and its associated lambda object to be executed when job is due.
    ///
    /// Notice, this type of job is only execueted onde, for then to be deleted
    /// from the scheduler after execution.
    /// </summary>
    public class WhenJob : Job
    {
        /// <summary>
        /// Constructor creating a job that is to be executed only once,
        /// and then deleted.
        /// </summary>
        /// <param name="name">The name of your job.</param>
        /// <param name="description">Description for your job.</param>
        /// <param name="lambda">Actual lambda object to be executed when job is due.</param>
        /// <param name="when">Date of when job should be executed.</param>
        public WhenJob(
            string name, 
            string description, 
            Node lambda,
            DateTime when)
            : base(name, description, lambda)
        {
            if (when.AddMilliseconds(250) < DateTime.Now)
                throw new ArgumentException($"Due date of job must be some time in the future.");

            Due = when;
        }

        /// <summary>
        /// Returns whether or not job should be repeated, which is always false
        /// for this type of job.
        /// </summary>
        public override bool Repeats => false;

        #region [ -- Overridden abstract base class methods -- ]

        /// <summary>
        /// Returns the node representation of the job.
        /// </summary>
        /// <returns>A node representing the declaration of the job as when created.</returns>
        public override Node GetNode()
        {
            var result = base.GetNode();
            result.Add(new Node("when", Due));
            return result;
        }

        /// <summary>
        /// Calculates the next due date for the job.
        /// 
        /// Notice, do not invoke this method for this type of job, since it will throw an exception,
        /// since job is not repeating, and its only execution date should have been supplied when
        /// the job was created.
        /// </summary>
        protected override DateTime CalculateNextDue()
        {
            return Due;
        }

        #endregion
    }
}
