using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Michsky.MUIP;
using UnityEngine;

public class PulseWidthProfileController : MonoBehaviour
{
    public enum ProfileState { Idle, Pending, Succeeded, Failed, Unknown }

    [Header("Required scene references")]
    [SerializeField] private Stimulation stimulation;
    [SerializeField] private CustomDropdown maximumPulseWidthDropdown;
    [SerializeField] private CustomDropdown ratioDropdown;
    [SerializeField] private ButtonManager profileStatus;
    [SerializeField] private NotificationManager notification;
    [SerializeField] private DeviceCommandQueue commandQueue;

    [Header("Initial profile")]
    [SerializeField] private int initialMaximumPulseWidth = 250;
    [SerializeField] private int initialRatio = 4;

    public int ActiveMaximumPulseWidth { get; private set; }
    public int ActiveRatio { get; private set; }
    public ProfileState State { get; private set; } = ProfileState.Idle;
    public string LastError { get; private set; }
    public int PendingMaximumPulseWidth => pendingMaximumPulseWidth;
    public int PendingRatio => pendingRatio;
    public int LastRequestedMaximumPulseWidth { get; private set; }
    public int LastRequestedRatio { get; private set; }
    public int FailedEventId { get; private set; }
    public float LastAttemptElapsedSeconds { get; private set; }
    public IReadOnlyList<int> AcknowledgedEventIds => acknowledgedEventIds;

    public event Action<int> ActiveMaximumChanged;
    public event Action HardwareStateBecameUnknown;

    private int pendingMaximumPulseWidth;
    private int pendingRatio;
    private readonly List<int> acknowledgedEventIds = new List<int>();
#if UNITY_EDITOR
    private Func<int, int, Task<string>> ratioUpdateOverride;
#endif

    private void Awake()
    {
        if (commandQueue == null) commandQueue = FindObjectOfType<DeviceCommandQueue>();
        ActiveMaximumPulseWidth = IsSupportedMaximum(initialMaximumPulseWidth)
            ? initialMaximumPulseWidth
            : 250;
        ActiveRatio = IsSupportedRatio(initialRatio) && IsCombinationValid(ActiveMaximumPulseWidth, initialRatio)
            ? initialRatio
            : HighestValidRatio(ActiveMaximumPulseWidth);
    }

    private void Start()
    {
        bool valid = ValidateReferences();
        RestoreDropdownSelections();
        RefreshStatus();
        if (!valid) enabled = false;
    }

    public void SelectMaximum250() => RequestMaximum(250);
    public void SelectMaximum500() => RequestMaximum(500);
    public void SelectMaximum1000() => RequestMaximum(1000);
    public void SelectRatio1() => RequestRatio(1);
    public void SelectRatio2() => RequestRatio(2);
    public void SelectRatio4() => RequestRatio(4);
    public void RequestMaximumPulseWidth(int maximum) => RequestMaximum(maximum);
    public void RequestRatioValue(int ratio) => RequestRatio(ratio);

    private void RequestMaximum(int maximum)
    {
        if (State == ProfileState.Pending || !IsSupportedMaximum(maximum))
        {
            RestoreDropdownSelections();
            return;
        }

        if (maximum == ActiveMaximumPulseWidth)
        {
            RestoreDropdownSelections();
            return;
        }

        if (IsCombinationValid(maximum, ActiveRatio))
        {
            CommitProfile(maximum, ActiveRatio);
            return;
        }

        QueueProfileChange(maximum, HighestValidRatio(maximum));
    }

    private void RequestRatio(int ratio)
    {
        if (State == ProfileState.Pending || !IsSupportedRatio(ratio))
        {
            RestoreDropdownSelections();
            return;
        }

        if (!IsCombinationValid(ActiveMaximumPulseWidth, ratio))
        {
            RestoreDropdownSelections();
            ShowNotification(
                "Invalid pulse-width ratio",
                $"{ActiveMaximumPulseWidth} us supports ratios up to 1:{HighestValidRatio(ActiveMaximumPulseWidth)}.");
            return;
        }

        if (ratio == ActiveRatio)
        {
            RestoreDropdownSelections();
            return;
        }

        QueueProfileChange(ActiveMaximumPulseWidth, ratio);
    }

    private void QueueProfileChange(int maximum, int ratio)
    {
        State = ProfileState.Pending;
        pendingMaximumPulseWidth = maximum;
        pendingRatio = ratio;
        LastRequestedMaximumPulseWidth = maximum;
        LastRequestedRatio = ratio;
        FailedEventId = 0;
        LastAttemptElapsedSeconds = 0f;
        acknowledgedEventIds.Clear();
        LastError = null;
        SetDropdownsInteractable(false);
        RefreshStatus();

        commandQueue.EnqueueRoutine(
            $"Apply pulse-width profile ({maximum} us, 1:{ratio})",
            () => ApplyRatioBatch(maximum, ratio),
            null,
            exception => FailPendingBatch(0, exception.Message, LastAttemptElapsedSeconds));
    }

    private IEnumerator ApplyRatioBatch(int requestedMaximum, int requestedRatio)
    {
        float batchStartedAt = Time.realtimeSinceStartup;
        for (int eventId = 1; eventId <= 3; eventId++)
        {
            Task<string> task;
            try
            {
#if UNITY_EDITOR
                task = ratioUpdateOverride != null
                    ? ratioUpdateOverride(eventId, requestedRatio)
                    : stimulation.UpdateEventRatioAsync(eventId, requestedRatio);
#else
                task = stimulation.UpdateEventRatioAsync(eventId, requestedRatio);
#endif
            }
            catch (Exception ex)
            {
                FailPendingBatch(eventId, ex.Message, Time.realtimeSinceStartup - batchStartedAt);
                yield break;
            }

            while (!task.IsCompleted) yield return null;
            float elapsed = Time.realtimeSinceStartup - batchStartedAt;

            if (task.IsCanceled)
            {
                FailPendingBatch(
                    eventId,
                    "No matching acknowledgement arrived within the DLL's approximately 2-second command window. The command may still have reached the device.",
                    elapsed);
                yield break;
            }
            if (task.IsFaulted)
            {
                string error = task.Exception != null
                    ? task.Exception.GetBaseException().Message
                    : "Unknown DLL error.";
                FailPendingBatch(eventId, error, elapsed);
                yield break;
            }
            if (!TryValidateAcknowledgement(task.Result, out string responseError))
            {
                FailPendingBatch(eventId, responseError, elapsed);
                yield break;
            }

            acknowledgedEventIds.Add(eventId);
        }

        LastAttemptElapsedSeconds = Time.realtimeSinceStartup - batchStartedAt;
        CommitProfile(requestedMaximum, requestedRatio);
        State = ProfileState.Succeeded;
        pendingMaximumPulseWidth = 0;
        pendingRatio = 0;
        SetDropdownsInteractable(true);
        RefreshStatus();
        yield return null;
        State = ProfileState.Idle;
        RefreshStatus();
    }

    private static bool TryValidateAcknowledgement(string response, out string error)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            error = "the DLL returned no acknowledgement.";
            return false;
        }

        string normalized = response.ToLowerInvariant();
        if (normalized.Contains("error") || normalized.Contains("fail") ||
            normalized.Contains("timeout") || normalized.Contains("nack"))
        {
            error = response;
            return false;
        }

        error = null;
        return true;
    }

    private void FailPendingBatch(int failedEventId, string error, float elapsedSeconds)
    {
        State = ProfileState.Unknown;
        FailedEventId = failedEventId;
        LastAttemptElapsedSeconds = elapsedSeconds;
        string acknowledged = acknowledgedEventIds.Count == 0
            ? "No events acknowledged."
            : $"Acknowledged events: {string.Join(", ", acknowledgedEventIds)}.";
        string failed = failedEventId > 0 ? $" Event {failedEventId} failed." : string.Empty;
        LastError = $"{acknowledged}{failed} {error} Elapsed: {elapsedSeconds:0.00}s. Hardware profile state is unknown.";
        pendingMaximumPulseWidth = 0;
        pendingRatio = 0;
        SetDropdownsInteractable(true);
        RefreshStatus();
        HardwareStateBecameUnknown?.Invoke();
        ShowNotification("Pulse-width profile state unknown", LastError);
    }

    private void CommitProfile(int maximum, int ratio)
    {
        bool maximumChanged = maximum != ActiveMaximumPulseWidth;
        ActiveMaximumPulseWidth = maximum;
        ActiveRatio = ratio;
        RestoreDropdownSelections();
        RefreshStatus();
        if (maximumChanged) ActiveMaximumChanged?.Invoke(maximum);
    }

    private void RestoreDropdownSelections()
    {
        SetDropdownIndex(maximumPulseWidthDropdown, MaximumToIndex(ActiveMaximumPulseWidth));
        SetDropdownIndex(ratioDropdown, RatioToIndex(ActiveRatio));
    }

    private static void SetDropdownIndex(CustomDropdown dropdown, int index)
    {
        if (dropdown == null || index < 0 || index >= dropdown.items.Count) return;
        dropdown.index = index;
        dropdown.SetDropdownIndex(index);
    }

    private void SetDropdownsInteractable(bool interactable)
    {
        if (maximumPulseWidthDropdown != null) maximumPulseWidthDropdown.isInteractable = interactable;
        if (ratioDropdown != null) ratioDropdown.isInteractable = interactable;
    }

    private void RefreshStatus()
    {
        if (profileStatus == null) return;
        string text = $"Active: {ActiveMaximumPulseWidth} us, 1:{ActiveRatio}";
        if (State == ProfileState.Pending)
            text += $" - Pending: {pendingMaximumPulseWidth} us, 1:{pendingRatio}";
        else if (State == ProfileState.Failed)
            text += " - Change failed";
        else if (State == ProfileState.Unknown)
            text = "Hardware profile unknown - select a profile to retry";
        profileStatus.buttonText = text;
        profileStatus.UpdateUI();
    }

    private void ShowNotification(string title, string description)
    {
        if (notification == null)
        {
            Debug.LogError(title + ": " + description);
            return;
        }
        notification.title = title;
        notification.description = description;
        notification.UpdateUI();
        notification.Open();
    }

    private bool ValidateReferences()
    {
        bool valid = true;
        if (stimulation == null) { Debug.LogError("PulseWidthProfileController: connect Stimulation in the Inspector.", this); valid = false; }
        if (commandQueue == null) { Debug.LogError("PulseWidthProfileController: connect Device Command Queue in the Inspector.", this); valid = false; }
        return valid;
    }

    private static bool IsCombinationValid(int maximum, int ratio) => maximum * ratio <= 1000;
    private static bool IsSupportedMaximum(int value) => value == 250 || value == 500 || value == 1000;
    private static bool IsSupportedRatio(int value) => value == 1 || value == 2 || value == 4;
    private static int HighestValidRatio(int maximum) => maximum <= 250 ? 4 : maximum <= 500 ? 2 : 1;
    private static int MaximumToIndex(int maximum) => maximum == 250 ? 0 : maximum == 500 ? 1 : 2;
    private static int RatioToIndex(int ratio) => ratio == 1 ? 0 : ratio == 2 ? 1 : 2;

#if UNITY_EDITOR
    public void SetRatioUpdateHandlerForTesting(Func<int, int, Task<string>> handler) => ratioUpdateOverride = handler;
#endif
}
