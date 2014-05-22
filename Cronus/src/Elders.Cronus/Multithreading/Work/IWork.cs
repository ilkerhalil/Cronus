﻿using System;

namespace Elders.Cronus.Multithreading.Work
{
    /// <summary>
    /// Defines async periodical work
    /// </summary>
    public interface IWork
    {
        DateTime ScheduledStart { get; }
        void Start();
        void Stop();
    }
}