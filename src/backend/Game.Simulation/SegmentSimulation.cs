using Game.Domain;
namespace Game.Simulation;

// Pace, tyre penalties and resolved variability are seconds per lap; attributes use AttributeSeconds.
public record SegmentInputs(double BaselineLapSeconds, double Speed, double Cornering,
    double Pace, double AttributeSeconds, double TyrePenaltySeconds = 0, double VariationSeconds = 0);
public record SegmentProjection(double TravelSeconds, double SpeedMetresPerSecond,
    double PositionMetres, double ExitAfterSeconds);
public static class SegmentSimulation
{
    public static SegmentProjection Project(TrackSegment segment, SegmentInputs inputs,
        double positionMetres = 0, double elapsedSeconds = 0)
    {
        segment.Validate();
        var values = new[] { inputs.BaselineLapSeconds, inputs.Speed, inputs.Cornering,
            inputs.Pace, inputs.AttributeSeconds, inputs.TyrePenaltySeconds, inputs.VariationSeconds,
            positionMetres, elapsedSeconds };
        if (values.Any(v => !double.IsFinite(v)) || inputs.BaselineLapSeconds <= 0
            || inputs.AttributeSeconds < 0 || positionMetres < 0 || positionMetres > segment.Length || elapsedSeconds < 0)
            throw new ArgumentException("Invalid effective segment inputs.");
        var share = segment.BaselineTime / inputs.BaselineLapSeconds;
        var seconds = Math.Max(share, segment.BaselineTime + share *
            (inputs.TyrePenaltySeconds + inputs.VariationSeconds - inputs.AttributeSeconds *
                (inputs.Pace + inputs.Speed * segment.SpeedDemand + inputs.Cornering * segment.CorneringDemand)));
        var speed = segment.Length / seconds;
        if (!double.IsFinite(seconds) || !double.IsFinite(speed) || speed <= 0)
            throw new ArgumentException("Derived segment trajectory must be finite and positive.");
        return new(seconds, speed, Math.Min(segment.Length, positionMetres + elapsedSeconds * speed),
            (segment.Length - positionMetres) / speed);
    }

    // Gap is physical metres forward to the defender; horizon ends at either segment exit.
    public static double? PredictCatch(SegmentProjection attacker, SegmentProjection defender, double gapMetres)
    {
        if (!double.IsFinite(gapMetres) || gapMetres < 0) throw new ArgumentException("Invalid physical gap.");
        var relative = attacker.SpeedMetresPerSecond - defender.SpeedMetresPerSecond;
        if (relative <= 0) return null;
        var seconds = gapMetres / relative;
        return seconds <= Math.Min(attacker.ExitAfterSeconds, defender.ExitAfterSeconds) ? seconds : null;
    }
}
