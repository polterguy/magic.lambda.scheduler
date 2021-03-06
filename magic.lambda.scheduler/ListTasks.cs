﻿/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2021, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System.Linq;
using System.Threading.Tasks;
using magic.node;
using magic.node.extensions;
using magic.signals.contracts;
using magic.lambda.scheduler.utilities;

namespace magic.lambda.scheduler
{
    /// <summary>
    /// [tasks.list] slot that will return the names of all tasks in the system.
    /// </summary>
    [Slot(Name = "tasks.list")]
    public class ListTasks : ISlotAsync
    {
        readonly IScheduler _scheduler;

        /// <summary>
        /// Creates a new instance of your slot.
        /// </summary>
        /// <param name="scheduler">Scheduler service to use.</param>
        public ListTasks(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        /// <summary>
        /// Slot implementation.
        /// </summary>
        /// <param name="signaler">Signaler that raised signal.</param>
        /// <param name="input">Arguments to slot.</param>
        public async Task SignalAsync(ISignaler signaler, Node input)
        {
            var jobs = await _scheduler.ListTasks(
                input.GetEx<string>(),
                input.Children.FirstOrDefault(x => x.Name == "offset")?.GetEx<long>() ?? 0,
                input.Children.FirstOrDefault(x => x.Name == "limit")?.GetEx<long>() ?? 10);
            input.Clear();
            if (jobs.Any())
            {
                input.AddRange(jobs);
                foreach (var idx in input.Children)
                {
                    var desc = idx.Children.First(x => x.Name == "description");
                    if (desc.Value == null)
                        desc.UnTie();
                    idx.Children.First(x => x.Name == "hyperlambda").UnTie();
                }
            }
        }
    }
}
