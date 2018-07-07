using ColossalFramework.UI;
using CSUtil.Commons;
using CSUtil.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TrafficManager.TrafficLight;
using UnityEngine;
using static TrafficManager.UI.SubTools.TimedTrafficLightsTool;

namespace TrafficManager.UI.SubTools.TimedTrafficLights {
	public class UIShowLightsPanel : UITimedTrafficLightsContentPanel {
		private UIScrollablePanel stepsPanel;
		private UIScrollbar stepsScrollbar;

		public const float ROW_BORDER = 5f;
		public const float ROW_HEIGHT = 24f;

		public override void Start() {
			base.Start();
			
			stepsPanel = AddUIComponent<UIScrollablePanel>();
			stepsPanel.autoSize = false;
			stepsPanel.clipChildren = true;
			stepsPanel.builtinKeyNavigation = true;
			stepsPanel.scrollWithArrowKeys = true;
			stepsPanel.scrollWheelDirection = UIOrientation.Vertical;
			stepsPanel.cachedName = "TMPE_StepsPanel";
			stepsPanel.relativePosition = Vector2.zero;
			stepsPanel.width = width - 24f;
			stepsPanel.height = 300;
			stepsPanel.anchor = UIAnchorStyle.Left | UIAnchorStyle.Top | UIAnchorStyle.Right;

			stepsScrollbar = AddUIComponent<UIScrollbar>();
			stepsScrollbar.incrementAmount = 35f;
			stepsScrollbar.autoSize = false;
			stepsScrollbar.cachedName = "TMPE_StepsScrollbar";
			stepsScrollbar.relativePosition = new Vector2(width - 22f, 0);
			stepsScrollbar.size = new Vector2(20f, stepsPanel.height);
			stepsScrollbar.anchor = UIAnchorStyle.Top | UIAnchorStyle.Right;
			stepsScrollbar.orientation = UIOrientation.Vertical;

			UISlicedSprite track = stepsScrollbar.AddUIComponent<UISlicedSprite>();
			track.autoSize = false;
			track.cachedName = "TMPE_StepsScrollbarTrack";
			track.relativePosition = Vector2.zero;
			track.size = track.parent.size;
			track.spriteName = "ScrollbarTrack";
			track.anchor = UIAnchorStyle.All;

			UISlicedSprite thumb = track.AddUIComponent<UISlicedSprite>();
			thumb.autoSize = false;
			thumb.cachedName = "TMPE_StepsScrollbarThumb";
			thumb.relativePosition = new Vector2(2f, 0f);
			thumb.width = thumb.parent.width - 4f;
			thumb.spriteName = "ScrollbarThumb";
			thumb.minimumSize = new Vector2(thumb.parent.width - 4f, 10f);
			thumb.anchor = UIAnchorStyle.Top | UIAnchorStyle.Left | UIAnchorStyle.Right;

			stepsScrollbar.trackObject = track;
			stepsScrollbar.thumbObject = thumb;
			stepsPanel.verticalScrollbar = stepsScrollbar;

			//panel.relativePosition = new Vector2(CONTENT_BORDER, CAPTION_HEIGHT + CONTENT_BORDER);
			//panel.size = new Vector2(size.x - 2f * CONTENT_BORDER, size.y - CAPTION_HEIGHT - 2f * CONTENT_BORDER);
			//panel.anchor = UIAnchorStyle.All;
		}

		public override void Update() {
			base.Update();

			if (Tool.MasterTimedTrafficLights == null) {
				foreach (UIComponent comp in stepsPanel.components) {
					stepsPanel.RemoveUIComponent(comp);
				}
				return;
			}

			int numSteps = Tool.MasterTimedTrafficLights.NumSteps();
			for (int i = 0; i < Math.Min(stepsPanel.components.Count, numSteps); ++i) {
				UIStepRow stepRow = (UIStepRow)stepsPanel.components[i];
				stepRow.relativePosition = new Vector2(0, ROW_HEIGHT * (float)i - stepsScrollbar.value);
				stepRow.StepIndex = i;
			}

			for (int i = stepsPanel.components.Count; i < numSteps; ++i) {
				ITimedTrafficLightsStep step = Tool.MasterTimedTrafficLights.GetStep(i);
				UIStepRow stepRow = stepsPanel.AddUIComponent<UIStepRow>();

				stepRow.cachedName = "TMPE_StepRow";
				stepRow.relativePosition = new Vector2(0, ROW_HEIGHT * (float)i);
				stepRow.size = new Vector2(stepsPanel.width, ROW_HEIGHT);
				stepRow.anchor = UIAnchorStyle.All;
				stepRow.StepIndex = i;
			}

			while (stepsPanel.components.Count > numSteps) {
				stepsPanel.components[stepsPanel.components.Count - 1].Destroy();
				stepsPanel.components.RemoveAt(stepsPanel.components.Count - 1);
			}
		}

		public override void InitializeContent() {
			Window.width = 400;
			Window.height = 500;
		}

		public class UIStepRow : UICellRow {
			public int StepIndex { get; set; } = -1;

			public override bool Odd {
				get {
					return (StepIndex % 2) == 0;
				}
			}

			public override float Width {
				get {
					return parent.width;
				}
			}

			public override float Height {
				get {
					return ROW_HEIGHT;
				}
			}

			public override float Margin {
				get {
					return 3f;
				}
			}

			public override IList<UICellDefinition> CellDefinitions {
				get {
					return cellDefinitions;
				}
			}

			private readonly IList<UICellDefinition> cellDefinitions = new List<UICellDefinition> {
				new UICellDefinition(),
				new UICellDefinition(UICellWidthMode.Absolute, 75f),
				new UICellDefinition(UICellWidthMode.Absolute, UIToolButton.BUTTON_SIZE),
				new UICellDefinition(UICellWidthMode.Absolute, UIToolButton.BUTTON_SIZE),
				new UICellDefinition(UICellWidthMode.Absolute, 10f),
				new UICellDefinition(UICellWidthMode.Absolute, UIToolButton.BUTTON_SIZE),
				new UICellDefinition(UICellWidthMode.Absolute, UIToolButton.BUTTON_SIZE),
				new UICellDefinition(UICellWidthMode.Absolute, UIToolButton.BUTTON_SIZE)
			};

			public UILabel NameLabel { get; private set; }
			public UILabel TimeLabel { get; private set; }
			public UIStepUpButton StepUpButton { get; private set; }
			public UIStepDownButton StepDownButton { get; private set; }
			public UIViewStepButton ViewStepButton { get; private set; }
			public UIEditStepButton EditStepButton { get; private set; }
			public UIRemoveStepButton RemoveStepButton { get; private set; }

			public override void Start() {
				Log._Debug($"UIStepRow.Start() called.");

				base.Start();

				ITimedTrafficLightsStep step = UISelectNodePanel.Instance.Tool.MasterTimedTrafficLights.GetStep(StepIndex);

				UIPanel namePanel = (UIPanel)components[0];
				NameLabel = namePanel.AddUIComponent<UILabel>();
				NameLabel.autoSize = false;
				NameLabel.textAlignment = UIHorizontalAlignment.Center;
				NameLabel.verticalAlignment = UIVerticalAlignment.Middle;

				UIPanel timePanel = (UIPanel)components[1];
				TimeLabel = timePanel.AddUIComponent<UILabel>();
				TimeLabel.autoSize = false;
				TimeLabel.textAlignment = UIHorizontalAlignment.Center;
				TimeLabel.verticalAlignment = UIVerticalAlignment.Middle;

				UIPanel stepUpPanel = (UIPanel)components[2];
				StepUpButton = stepUpPanel.AddUIComponent<UIStepUpButton>();

				UIPanel stepDownPanel = (UIPanel)components[3];
				StepDownButton = stepDownPanel.AddUIComponent<UIStepDownButton>();

				// TODO 4 is a quick & dirty spacer element

				UIPanel viewStepPanel = (UIPanel)components[5];
				ViewStepButton = viewStepPanel.AddUIComponent<UIViewStepButton>();

				UIPanel editStepPanel = (UIPanel)components[6];
				EditStepButton = editStepPanel.AddUIComponent<UIEditStepButton>();

				UIPanel removeStepPanel = (UIPanel)components[7];
				RemoveStepButton = removeStepPanel.AddUIComponent<UIRemoveStepButton>();
			}

			public override void Update() {
				base.Update();

				ITimedTrafficLights ttl = UISelectNodePanel.Instance.Tool.MasterTimedTrafficLights;
				//Log._Debug($"UIStepRow.Update() called: ttl={ttl != null}");
				ITimedTrafficLightsStep step = ttl.GetStep(StepIndex);

				//Log._Debug($"UIStepRow.Update() called: step={step != null} NameLabel={NameLabel} TimeLabel={TimeLabel}");

				Color stepColor = GetStepColor();

				NameLabel.text = Translation.GetString("State") + " " + StepIndex; // TODO custom naming
				NameLabel.size = new Vector2(NameLabel.parent.width, 18f);
				NameLabel.relativePosition = new Vector2(0, (NameLabel.parent.height - NameLabel.height) / 2f);
				NameLabel.textColor = stepColor;

				TimeLabel.text = ttl.IsStarted() && ttl.CurrentStep == StepIndex ? $"{step.MinTimeRemaining()} / {step.MaxTimeRemaining()}" : $"{step.MinTime} / {step.MaxTime}";
				TimeLabel.size = new Vector2(TimeLabel.parent.width, 18f);
				TimeLabel.relativePosition = new Vector2(0, (TimeLabel.parent.height - TimeLabel.height) / 2f);
				TimeLabel.textColor = stepColor;

				StepUpButton.CenterToParent();
				StepDownButton.CenterToParent();
				ViewStepButton.CenterToParent();
				EditStepButton.CenterToParent();
				RemoveStepButton.CenterToParent();
			}

			protected Color GetStepColor() {
				TimedTrafficLightsTool tool = UISelectNodePanel.Instance.Tool;
				ITimedTrafficLights ttl = tool.MasterTimedTrafficLights;
				ITimedTrafficLightsStep step = ttl.GetStep(StepIndex);

				if (ttl.IsStarted()) {
					if (ttl.CurrentStep != StepIndex) {
						return Color.white;
					}

					float metric;

					if (!ttl.IsInTestMode()) {
						if (step.IsInEndTransition()) {
							return Color.yellow;
						} else if (step.IsEndTransitionDone()) {
							return Color.red;
						}
					}

					if (step.ShouldGoToNextStep(out metric)) {
						return ttl.IsInTestMode() ? Color.red : Color.yellow;
					} else {
						return Color.green;
					}
				} else if (tool.Mode == TimedTrafficLightsTool.TimedTrafficLightsToolMode.ViewStep && tool.SelectedStepIndex == StepIndex) {
					return Color.green;
				}
				return Color.white;
			}
		}

		public abstract class UIStepRowButton : UIToolButton {
			public UIStepRow Row {
				get {
					return (UIStepRow)parent.parent;
				}
			}
		}

		public class UIStepUpButton : UIStepRowButton {
			public override ButtonFunction Function {
				get {
					return ButtonFunction.Up;
				}
			}

			public override ButtonFunctionStates FunctionState {
				get {
					return (Row == null || Row.StepIndex <= 0) ? ButtonFunctionStates.Disabled : ButtonFunctionStates.Default;
				}
			}

			public override string Tooltip {
				get {
					return Translation.GetString("up");
				}
			}

			public override bool Visible {
				get {
					return true;
				}
			}

			public override void HandleClick(UIMouseEventParameter p) {
				base.HandleClick(p);

				UISelectNodePanel.Instance.Tool.MoveStepUp(Row.StepIndex);
			}
		}

		public class UIStepDownButton : UIStepRowButton {
			public override ButtonFunction Function {
				get {
					return ButtonFunction.Down;
				}
			}

			public override ButtonFunctionStates FunctionState {
				get {
					if (UIShowLightsPanel.Instance.Tool.MasterTimedTrafficLights == null || Row == null || Row.StepIndex < 0) {
						return ButtonFunctionStates.Disabled;
					}

					return Row.StepIndex >= UIShowLightsPanel.Instance.Tool.MasterTimedTrafficLights.NumSteps() - 1 ? ButtonFunctionStates.Disabled : ButtonFunctionStates.Default;
				}
			}

			public override string Tooltip {
				get {
					return Translation.GetString("down");
				}
			}

			public override bool Visible {
				get {
					return true;
				}
			}

			public override void HandleClick(UIMouseEventParameter p) {
				base.HandleClick(p);

				UISelectNodePanel.Instance.Tool.MoveStepDown(Row.StepIndex);
			}
		}

		public class UIViewStepButton : UIStepRowButton {
			public override ButtonFunction Function {
				get {
					return ButtonFunction.View;
				}
			}

			public override ButtonFunctionStates FunctionState {
				get {
					TimedTrafficLightsTool tool = UIShowLightsPanel.Instance.Tool;
					ITimedTrafficLights ttl = tool.MasterTimedTrafficLights;

					if (ttl == null || Row == null || Row.StepIndex < 0 ||
						(tool.Mode == TimedTrafficLightsToolMode.ViewStep && tool.SelectedStepIndex == Row.StepIndex) || ttl.IsStarted()) {
						return ButtonFunctionStates.Disabled;
					}

					return ButtonFunctionStates.Default;
				}
			}

			public override string Tooltip {
				get {
					return Translation.GetString("View");
				}
			}

			public override bool Visible {
				get {
					return true;
				}
			}

			public override void HandleClick(UIMouseEventParameter p) {
				base.HandleClick(p);

				Log._Debug($"UIViewStepButton.HandleClick(): StepIndex={Row.StepIndex}");
				UISelectNodePanel.Instance.Tool.ViewStep(Row.StepIndex);
			}
		}

		public class UIEditStepButton : UIStepRowButton {
			public override ButtonFunction Function {
				get {
					return ButtonFunction.Edit;
				}
			}

			public override ButtonFunctionStates FunctionState {
				get {
					if (UIShowLightsPanel.Instance.Tool.MasterTimedTrafficLights == null || Row == null || Row.StepIndex < 0) {
						return ButtonFunctionStates.Disabled;
					}

					return ButtonFunctionStates.Default;
				}
			}

			public override string Tooltip {
				get {
					return Translation.GetString("Edit");
				}
			}

			public override bool Visible {
				get {
					return true;
				}
			}

			public override void HandleClick(UIMouseEventParameter p) {
				base.HandleClick(p);

				UISelectNodePanel.Instance.Tool.EditStep(Row.StepIndex);
			}
		}

		public class UIRemoveStepButton : UIStepRowButton {
			public override ButtonFunction Function {
				get {
					return ButtonFunction.Delete;
				}
			}

			public override ButtonFunctionStates FunctionState {
				get {
					if (UIShowLightsPanel.Instance.Tool.MasterTimedTrafficLights == null || Row == null || Row.StepIndex < 0) {
						return ButtonFunctionStates.Disabled;
					}

					return ButtonFunctionStates.Default;
				}
			}

			public override string Tooltip {
				get {
					return Translation.GetString("Delete");
				}
			}

			public override bool Visible {
				get {
					return true;
				}
			}

			public override void HandleClick(UIMouseEventParameter p) {
				base.HandleClick(p);

				UISelectNodePanel.Instance.Tool.RemoveStep(Row.StepIndex);
			}
		}
	}
}
