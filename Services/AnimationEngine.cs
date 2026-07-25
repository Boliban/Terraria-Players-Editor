namespace Terraria_Players_Editor.Services;

/// <summary>
/// Centralized animation manager using a single high-resolution Timer (~60fps).
/// Replaces piecemeal Timer instances with a unified system for
/// smooth float/color transitions with configurable easing functions.
/// </summary>
public class AnimationEngine : IDisposable
{
    private static readonly Lazy<AnimationEngine> _instance = new(() => new AnimationEngine());
    public static AnimationEngine Instance => _instance.Value;

    private readonly System.Windows.Forms.Timer _timer;
    private readonly List<Animation> _active = new();
    private readonly List<Animation> _pendingRemoval = new();
    private bool _updating;

    private AnimationEngine()
    {
        _timer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60fps
        _timer.Tick += OnTick;
    }

    /// <summary>Animate a float value from start to end.</summary>
    public Animation Animate(float from, float to, int durationMs,
        EasingFunction easing, Action<float> onUpdate, Action? onComplete = null)
    {
        var anim = new Animation
        {
            StartFloat = from,
            EndFloat = to,
            DurationMs = durationMs,
            Easing = easing,
            OnUpdateFloat = onUpdate,
            OnComplete = onComplete,
            IsColorAnimation = false
        };
        StartAnimation(anim);
        return anim;
    }

    /// <summary>Animate a Color value from start to end.</summary>
    public Animation AnimateColor(Color from, Color to, int durationMs,
        EasingFunction easing, Action<Color> onUpdate, Action? onComplete = null)
    {
        var anim = new Animation
        {
            StartColor = from,
            EndColor = to,
            DurationMs = durationMs,
            Easing = easing,
            OnUpdateColor = onUpdate,
            OnComplete = onComplete,
            IsColorAnimation = true
        };
        StartAnimation(anim);
        return anim;
    }

    private void StartAnimation(Animation anim)
    {
        anim.StartTime = Environment.TickCount;
        anim.ElapsedMs = 0f;
        anim.IsCompleted = false;

        lock (_active)
        {
            _active.Add(anim);
        }

        if (!_timer.Enabled)
            _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_updating) return;
        _updating = true;

        long now = Environment.TickCount;
        _pendingRemoval.Clear();

        Animation[] snapshot;
        lock (_active)
        {
            snapshot = _active.ToArray();
        }

        foreach (var anim in snapshot)
        {
            anim.ElapsedMs = now - anim.StartTime;
            float t = anim.DurationMs > 0
                ? Math.Clamp(anim.ElapsedMs / (float)anim.DurationMs, 0f, 1f)
                : 1f;

            float easedT = ApplyEasing(t, anim.Easing);

            if (anim.IsColorAnimation)
            {
                var c = InterpolateColor(anim.StartColor, anim.EndColor, easedT);
                anim.OnUpdateColor?.Invoke(c);
            }
            else
            {
                var v = anim.StartFloat + (anim.EndFloat - anim.StartFloat) * easedT;
                anim.OnUpdateFloat?.Invoke(v);
            }

            if (t >= 1f)
            {
                anim.IsCompleted = true;
                _pendingRemoval.Add(anim);
                anim.OnComplete?.Invoke();
            }
        }

        if (_pendingRemoval.Count > 0)
        {
            lock (_active)
            {
                foreach (var a in _pendingRemoval)
                    _active.Remove(a);
            }
        }

        // Stop the timer when no animations are active (save CPU)
        lock (_active)
        {
            if (_active.Count == 0 && _timer.Enabled)
                _timer.Stop();
        }

        _updating = false;
    }

    private static float ApplyEasing(float t, EasingFunction easing)
    {
        return easing switch
        {
            EasingFunction.Linear => t,
            EasingFunction.EaseOutCubic => 1f - MathF.Pow(1f - t, 3f),
            EasingFunction.EaseInOutCubic => t < 0.5f
                ? 4f * t * t * t
                : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f,
            EasingFunction.EaseOutQuad => 1f - (1f - t) * (1f - t),
            EasingFunction.EaseInOutQuad => t < 0.5f
                ? 2f * t * t
                : 1f - MathF.Pow(-2f * t + 2f, 2f) / 2f,
            EasingFunction.EaseOutBack => EaseOutBackCore(t),
            _ => t,
        };
    }

    private static float EaseOutBackCore(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * MathF.Pow(t - 1f, 3f) + c1 * MathF.Pow(t - 1f, 2f);
    }

    private static Color InterpolateColor(Color a, Color b, float t)
    {
        return Color.FromArgb(
            ClampByte(a.A + (b.A - a.A) * t),
            ClampByte(a.R + (b.R - a.R) * t),
            ClampByte(a.G + (b.G - a.G) * t),
            ClampByte(a.B + (b.B - a.B) * t));
    }

    private static byte ClampByte(float v) => (byte)Math.Clamp((int)v, 0, 255);

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        lock (_active)
        {
            _active.Clear();
        }
    }
}

/// <summary>An active or completed animation.</summary>
public class Animation
{
    public long StartTime;
    public float ElapsedMs;
    public int DurationMs;
    public bool IsCompleted;
    public bool IsColorAnimation;

    public float StartFloat;
    public float EndFloat;
    public Color StartColor;
    public Color EndColor;

    public EasingFunction Easing;
    public Action<float>? OnUpdateFloat;
    public Action<Color>? OnUpdateColor;
    public Action? OnComplete;

    /// <summary>Cancel this animation immediately (no onComplete callback).</summary>
    public void Cancel()
    {
        IsCompleted = true;
        // It will be cleaned up on the next tick
    }
}

/// <summary>Easing functions for animation interpolation.</summary>
public enum EasingFunction
{
    Linear,
    EaseOutCubic,
    EaseInOutCubic,
    EaseOutQuad,
    EaseInOutQuad,
    EaseOutBack,
}
