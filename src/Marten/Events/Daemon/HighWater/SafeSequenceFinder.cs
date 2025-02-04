using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Marten.Services;
using Npgsql;
using Weasel.Postgresql;

namespace Marten.Events.Daemon.HighWater
{
    internal class SafeSequenceFinder: ISingleQueryHandler<long?>
    {
        private readonly NpgsqlCommand _findSafeSequence;
        private readonly NpgsqlParameter _safeTimestamp;
        private readonly NpgsqlParameter _safeSequenceId;

        public SafeSequenceFinder(EventGraph graph)
        {
            _findSafeSequence = new NpgsqlCommand($@"select min(seq_id) from {graph.DatabaseSchemaName}.mt_events where mt_events.seq_id > :sequence_id and mt_events.timestamp >= :timestamp");
            _safeTimestamp = _findSafeSequence.AddNamedParameter("timestamp", DateTimeOffset.MinValue);
            _safeSequenceId = _findSafeSequence.AddNamedParameter("sequence_id", 0);
        }

        public long SafeSequenceId
        {
            set => _safeSequenceId.Value = value;
        }

        public DateTimeOffset SafeTimestamp
        {
            set
            {
                _safeTimestamp.Value = value;
            }
        }

        public NpgsqlCommand BuildCommand()
        {
            return _findSafeSequence;
        }

        public async Task<long?> HandleAsync(DbDataReader reader, CancellationToken token)
        {
            if (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                if (await reader.IsDBNullAsync(0, token).ConfigureAwait(false))
                {
                    return null;
                }

                return await reader.GetFieldValueAsync<long>(0, token).ConfigureAwait(false);
            }

            return null;
        }
    }
}
