using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializes slow device-configuration commands so they cannot overlap with
/// one another. Momentary stimulation commands intentionally bypass this queue.
/// </summary>
public sealed class DeviceCommandQueue : MonoBehaviour
{
    private sealed class QueuedCommand
    {
        public string Label;
        public Func<IEnumerator> Routine;
        public Action Completed;
        public Action<Exception> Failed;
    }

    [SerializeField, Min(0f)] private float intervalBetweenCommands = 0.1f;

    private readonly Queue<QueuedCommand> commands = new Queue<QueuedCommand>();
    private Coroutine worker;

    public bool IsBusy => worker != null;
    public int PendingCount => commands.Count + (worker != null ? 1 : 0);
    public string CurrentLabel { get; private set; } = string.Empty;

    public event Action QueueChanged;

    public void EnqueueAction(string label, Action action, Action completed = null, Action<Exception> failed = null)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        EnqueueRoutine(label, () => RunAction(action), completed, failed);
    }

    public void EnqueueRoutine(string label, Func<IEnumerator> routine, Action completed = null, Action<Exception> failed = null)
    {
        if (routine == null) throw new ArgumentNullException(nameof(routine));
        commands.Enqueue(new QueuedCommand
        {
            Label = string.IsNullOrWhiteSpace(label) ? "Device command" : label,
            Routine = routine,
            Completed = completed,
            Failed = failed
        });
        QueueChanged?.Invoke();
        if (worker == null) worker = StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        while (commands.Count > 0)
        {
            QueuedCommand command = commands.Dequeue();
            CurrentLabel = command.Label;
            QueueChanged?.Invoke();

            Exception failure = null;
            IEnumerator routine = null;
            try { routine = command.Routine(); }
            catch (Exception exception) { failure = exception; }

            while (failure == null && routine != null)
            {
                object yielded = null;
                bool hasNext;
                try
                {
                    hasNext = routine.MoveNext();
                    if (hasNext) yielded = routine.Current;
                }
                catch (Exception exception)
                {
                    failure = exception;
                    break;
                }

                if (!hasNext) break;
                yield return yielded;
            }

            if (failure == null) command.Completed?.Invoke();
            else command.Failed?.Invoke(failure);

            if (intervalBetweenCommands > 0f)
                yield return new WaitForSecondsRealtime(intervalBetweenCommands);
        }

        CurrentLabel = string.Empty;
        worker = null;
        QueueChanged?.Invoke();
    }

    private static IEnumerator RunAction(Action action)
    {
        action();
        yield return null;
    }
}
