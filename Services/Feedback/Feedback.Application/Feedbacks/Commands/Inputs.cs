namespace Feedback.Application.Feedbacks.Commands;

/// <summary>Вкладені вхідні об'єкти команд (знімок речі та температурна нотатка).</summary>
public record ItemSnapshotInput(long ItemId, string Name, string? Slot = null);

public record TemperatureNoteInput(double Felt, double Forecast);
