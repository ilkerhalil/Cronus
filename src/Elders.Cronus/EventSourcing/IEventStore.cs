using System;
using System.Collections.Generic;
using Elders.Cronus.DomainModelling;

namespace Elders.Cronus.EventSourcing
{
    public interface IEventStore
    {
        /// <summary>
        /// Opens an empty stream.
        /// </summary>
        /// <param name="getCommit">How to get a single <see cref="DomainMessageCommit"/> </param>
        /// <param name="commitCondition">When to commit the stream. IEventStream param holds the current state of the stream. DomainMessageCommit param holds the result of getCommit.</param>
        /// <param name="postCommit">What to do after a successful commit.</param>
        /// <param name="closeStreamCondition">When to close the stream.</param>
        void UseStream(Func<DomainMessageCommit> getCommit, Func<IEventStream, DomainMessageCommit, bool> commitCondition, Action<List<IEvent>> postCommit, Func<IEventStream, bool> closeStreamCondition, Action<DomainMessageCommit> onPersistError);

        string BoundedContext { get; }
        IEnumerable<IEvent> GetEventsFromStart(int batchPerQuery = 1);
    }
}