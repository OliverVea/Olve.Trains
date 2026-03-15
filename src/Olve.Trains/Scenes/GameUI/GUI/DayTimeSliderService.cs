using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;
using Olve.Generated.Layouts;

namespace Olve.Trains.Scenes.GameUI.GUI;

public class DayTimeSliderService(
    DayTimeManager dayTimeManager,
    DeltaTimeService deltaTimeService,
    GuiElementService guiElementService,
    GuiSliderService guiSliderService,
    GuiAnchorService guiAnchorService) : ISceneService
{
    private static readonly Layouts.DayTimePanel Panel = Layouts.BuildDayTimePanel();

    private Id<GuiElementRegistrations> _registrationId;
    private Id<GuiAnchor> _anchorId;

    public Result Load()
    {
        if (guiAnchorService.RegisterAnchor(AnchorPosition.MiddleRight, GrowthDirection.Left)
            .TryPickProblems(out var problems, out _anchorId))
        {
            return problems;
        }

        guiSliderService.OnValueChanged.Subscribe(OnSliderValueChanged);

        Panel.TimeSlider.Thumb.StyleKey = new StyleKey(nameof(Styles.SliderThumbStyle));
        Panel.TimeScaleSlider.Thumb.StyleKey = new StyleKey(nameof(Styles.SliderThumbStyle));

        return guiElementService
            .RegisterElementAndChildren(_anchorId, Panel.Root)
            .TryPickProblems(out problems, out _registrationId) ? problems : Result.Success();
    }

    public Result Unload()
    {
        guiSliderService.OnValueChanged.Unsubscribe(OnSliderValueChanged);
        guiElementService.UnregisterElementAndChildren(_registrationId);
        guiAnchorService.UnregisterAnchor(_anchorId);

        return Result.Success();
    }

    public Result Update()
    {
        Panel.TimeSlider.Value = dayTimeManager.CurrentTime.Value;

        return Result.Success();
    }

    private void OnSliderValueChanged(GuiSliderService.SliderValueChangedMessage message)
    {
        if (message.SliderId == Panel.TimeSlider.Id)
        {
            dayTimeManager.CurrentTime = new DayTime(message.NewValue);
        }
        else if (message.SliderId == Panel.TimeScaleSlider.Id)
        {
            var step = message.NewValue;
            var timeScale = step <= -3 ? 0f : MathF.Pow(1.5f, step);
            deltaTimeService.TimeScale = timeScale;

            var label = timeScale == 0f ? "Speed: 0x" : $"Speed: {timeScale:G3}x";
            Panel.TimeScaleLabel.Content = label;
        }
    }
}
