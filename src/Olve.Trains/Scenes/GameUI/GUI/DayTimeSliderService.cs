using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;
using Olve.Generated.Layouts;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.GameUI.GUI;

public class DayTimeSliderService(
    DayTimeManager dayTimeManager,
    DeltaTimeService deltaTimeService,
    GuiElementService guiElementService,
    GuiSliderService guiSliderService,
    GuiCheckboxService guiCheckboxService,
    GuiAnchorService guiAnchorService,
    KeyboardManager keyboardManager) : ISceneService
{
    // Index:  0      1      2     3    4  5  6  7   8
    // Scale: (removed 0) 1/8  1/4   1/2    1  2  4  8  16
    private static readonly float[] ScaleSteps = [0.125f, 0.25f, 0.5f, 1f, 2f, 4f, 8f, 16f];
    private static readonly string[] ScaleLabels = ["Speed: 1/8x", "Speed: 1/4x", "Speed: 1/2x", "Speed: 1x", "Speed: 2x", "Speed: 4x", "Speed: 8x", "Speed: 16x"];

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
        guiCheckboxService.OnValueChanged.Subscribe(OnCheckboxValueChanged);

        Panel.TimeSlider.Thumb.StyleKey = new StyleKey(nameof(Styles.SliderThumbStyle));
        Panel.TimeScaleSlider.Thumb.StyleKey = new StyleKey(nameof(Styles.SliderThumbStyle));
        Panel.TimePassingCheckbox.Background.StyleKey = new StyleKey(nameof(Styles.CheckboxBackgroundStyle));

        return guiElementService
            .RegisterElementAndChildren(_anchorId, Panel.Root)
            .TryPickProblems(out problems, out _registrationId) ? problems : Result.Success();
    }

    public Result Unload()
    {
        guiSliderService.OnValueChanged.Unsubscribe(OnSliderValueChanged);
        guiCheckboxService.OnValueChanged.Unsubscribe(OnCheckboxValueChanged);
        guiElementService.UnregisterElementAndChildren(_registrationId);
        guiAnchorService.UnregisterAnchor(_anchorId);

        return Result.Success();
    }

    public Result<Pass> Input()
    {
        if (keyboardManager.State.IsKeyPressed(Key.P))
        {
            Panel.TimePassingCheckbox.IsChecked = !Panel.TimePassingCheckbox.IsChecked;
            ApplyCheckboxState();
        }

        return Pass.Pass;
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
            var index = (int)MathF.Round(message.NewValue) - 1;
            index = int.Clamp(index, 0, ScaleSteps.Length - 1);

            ApplyTimeScale(index);
        }
    }

    private void OnCheckboxValueChanged(GuiCheckboxService.CheckboxValueChangedMessage message)
    {
        if (message.CheckboxId != Panel.TimePassingCheckbox.Id)
        {
            return;
        }

        ApplyCheckboxState();
    }

    private void ApplyCheckboxState()
    {
        if (Panel.TimePassingCheckbox.IsChecked)
        {
            var index = (int)MathF.Round(Panel.TimeScaleSlider.Value) - 1;
            index = int.Clamp(index, 0, ScaleSteps.Length - 1);
            ApplyTimeScale(index);
        }
        else
        {
            deltaTimeService.TimeScale = 0f;
            Panel.TimeScaleLabel.Content = "Paused";
        }
    }

    private void ApplyTimeScale(int index)
    {
        deltaTimeService.TimeScale = ScaleSteps[index];
        Panel.TimeScaleLabel.Content = ScaleLabels[index];
    }
}
