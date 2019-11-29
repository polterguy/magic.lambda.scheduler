﻿/*
 * Magic, Copyright(c) Thomas Hansen 2019, thomas@gaiasoul.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using magic.node;
using magic.signals.contracts;

namespace magic.lambda.scheduler.utilities
{
    /// <summary>
    /// The background service responsible for scheduling and evaluating tasks.
    ///
    /// Notice, you should make sure you resolve this as a singleton if you are
    /// using an IoC container. Also notice that no tasks will evaluate before
    /// you explicitly somehow invoke Start on your instance.
    /// 
    /// You are also responsible to make sure all operations on instance is synchronized.
    /// </summary>
    public sealed class Scheduler : IDisposable
    {
        readonly IServiceProvider _services;
        readonly ILogger _logger;
        readonly Synchronizer<Jobs> _tasks;

        /// <summary>
        /// Creates a new background service, responsible for scheduling and
        /// evaluating tasks that have been scheduled for future evaluation.
        /// </summary>
        /// <param name="services">Service provider to resolve ISignaler.</param>
        /// <param name="logger">Logging provider necessary to be able to log tasks that are
        /// not executed successfully.</param>
        /// <param name="tasksFile">The path to your tasks file,
        /// declaring what tasks your application has scheduled for future
        /// evaluation.</param>
        /// <param name="autoStart">If true, will start service immediately automatically.</param>
        public Scheduler(IServiceProvider services, ILogger logger, string tasksFile, bool autoStart)
        {
            // Need to store service provider to be able to create ISignaler during task execution.
            _services = services ?? throw new ArgumentNullException(nameof(services));

            // Storing logger in case of exceptions during job execution.
            _logger = logger;

            // Making sure we're able to evaluate tasks if autoStart is true.
            if (autoStart)
                Running = true;

            // Loading jobs, and initializing them.
            var jobs = new Jobs(tasksFile);
            foreach (var idx in jobs.List())
            {
                //idx.EnsureTimer(async (x) => await ExecuteTask(x));
            }

            // Making sure we have synchronized access to jobs further down the roda.
            _tasks = new Synchronizer<Jobs>(jobs);
        }

        /// <summary>
        /// Returns true if scheduler is running.
        /// </summary>
        public bool Running { get; private set; }

        /// <summary>
        /// Starts your scheduler. You must invoke this method in order to
        /// start your scheduler.
        /// </summary>
        public void Start()
        {
            Running = true;
        }

        /// <summary>
        /// Stops your scheduler, such that no more tasks will be evaluated.
        /// </summary>
        public void Stop()
        {
            Running = false;
        }

        /// <summary>
        /// Creates a new task.
        ///
        /// Notice, will delete any previously created tasks with the same name.
        /// </summary>
        /// <param name="job">Task to add.</param>
        public void Add(Job job)
        {
            _tasks.Write((tasks) => tasks.Add(job));
        }

        /// <summary>
        /// Lists all tasks in task manager, in order of evaluation, such that
        /// the first task in queue will be the first task returned.
        /// </summary>
        /// <returns>All tasks listed in chronological order of evaluation.</returns>
        public IEnumerable<Job> ListTasks()
        {
            return _tasks.Read((tasks) => tasks.List().ToList());
        }

        /// <summary>
        /// Returns a previously created task to caller.
        /// </summary>
        /// <param name="name">Name of task you wish to retrieve.</param>
        /// <returns>A node representing your task.</returns>
        public Job Get(string name)
        {
            // Getting task with specified name.
            return _tasks.Read((tasks) => tasks.GetTask(name));
        }

        /// <summary>
        /// Deletes an existing task from your task manager.
        /// </summary>
        /// <param name="name">Name of task to delete.</param>
        public void DeleteTask(string name)
        {
            _tasks.Write((tasks) => tasks.DeleteTask(name));
        }

        #region [ -- Interface implementations -- ]

        /// <summary>
        /// Disposes the TaskList.
        /// </summary>
        public void Dispose()
        {
            _tasks.Dispose();
        }

        #endregion

        #region [ -- Private helper methods -- ]

        async Task ExecuteTask(Job job)
        {
            if (!Running)
            {
                // Postponing into the future.
                job.EnsureTimer(async (x) => await ExecuteTask(x));
                return;
            }

            try
            {
                // Retrieving task and its lambda object, and evaluating it.
                var lambda = job.Lambda.Clone();
                var signaler = _services.GetService(typeof(ISignaler)) as ISignaler;
                await signaler.SignalAsync("wait.eval", lambda);
            }
            catch (Exception err)
            {
                // Making sure we log exception using preferred ILogger instance.
                _logger?.LogError(job.Name, err);
            }
            finally
            {
                if (job.Repeats)
                {
                    job.Due = job.CalculateNextDue();
                    job.EnsureTimer(async (x) => await ExecuteTask(x));
                }
                else
                {
                    _tasks.Write((tasks) =>
                    {
                        tasks.DeleteTask(job.Name);
                    });
                }
            }
        }

        #endregion
    }
}