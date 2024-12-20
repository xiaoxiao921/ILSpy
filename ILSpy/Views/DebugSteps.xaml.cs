﻿using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.IL.Transforms;
using ICSharpCode.ILSpy.Docking;
using ICSharpCode.ILSpy.Util;
using ICSharpCode.ILSpy.ViewModels;

using TomsToolbox.Wpf.Composition.Mef;

namespace ICSharpCode.ILSpy
{
	[DataTemplate(typeof(DebugStepsPaneModel))]
	[PartCreationPolicy(CreationPolicy.NonShared)]
	public partial class DebugSteps : UserControl
	{
		static readonly ILAstWritingOptions writingOptions = new ILAstWritingOptions {
			UseFieldSugar = true,
			UseLogicOperationSugar = true
		};

		public static ILAstWritingOptions Options => writingOptions;

#if DEBUG
		ILAstLanguage language;
#endif
		public DebugSteps()
		{
			InitializeComponent();

#if DEBUG
			MessageBus<LanguageSettingsChangedEventArgs>.Subscribers += (sender, e) => LanguageSettings_PropertyChanged(sender, e);

			MainWindow.Instance.SelectionChanged += SelectionChanged;
			writingOptions.PropertyChanged += WritingOptions_PropertyChanged;

			if (SettingsService.Instance.SessionSettings.LanguageSettings.Language is ILAstLanguage l)
			{
				l.StepperUpdated += ILAstStepperUpdated;
				language = l;
				ILAstStepperUpdated(null, null);
			}
#endif
		}

		private void WritingOptions_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			DecompileAsync(lastSelectedStep);
		}

		private void SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Dispatcher.Invoke(() => {
				tree.ItemsSource = null;
				lastSelectedStep = int.MaxValue;
			});
		}

		private void LanguageSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
#if DEBUG
			if (e.PropertyName == "Language")
			{
				if (language != null)
				{
					language.StepperUpdated -= ILAstStepperUpdated;
				}
				if (SettingsService.Instance.SessionSettings.LanguageSettings.Language is ILAstLanguage l)
				{
					l.StepperUpdated += ILAstStepperUpdated;
					language = l;
					ILAstStepperUpdated(null, null);
				}
			}
#endif
		}

		private void ILAstStepperUpdated(object sender, EventArgs e)
		{
#if DEBUG
			if (language == null)
				return;
			Dispatcher.Invoke(() => {
				tree.ItemsSource = language.Stepper.Steps;
				lastSelectedStep = int.MaxValue;
			});
#endif
		}

		private void ShowStateAfter_Click(object sender, RoutedEventArgs e)
		{
			Stepper.Node n = (Stepper.Node)tree.SelectedItem;
			if (n == null)
				return;
			DecompileAsync(n.EndStep);
		}

		private void ShowStateBefore_Click(object sender, RoutedEventArgs e)
		{
			Stepper.Node n = (Stepper.Node)tree.SelectedItem;
			if (n == null)
				return;
			DecompileAsync(n.BeginStep);
		}

		private void DebugStep_Click(object sender, RoutedEventArgs e)
		{
			Stepper.Node n = (Stepper.Node)tree.SelectedItem;
			if (n == null)
				return;
			DecompileAsync(n.BeginStep, true);
		}

		int lastSelectedStep = int.MaxValue;

		void DecompileAsync(int step, bool isDebug = false)
		{
			lastSelectedStep = step;
			var window = MainWindow.Instance;
			var state = DockWorkspace.Instance.ActiveTabPage.GetState();
			DockWorkspace.Instance.ActiveTabPage.ShowTextViewAsync(textView => textView.DecompileAsync(window.CurrentLanguage, window.SelectedNodes,
				new DecompilationOptions(window.CurrentLanguageVersion, window.CurrentDecompilerSettings, window.CurrentDisplaySettings) {
					StepLimit = step,
					IsDebug = isDebug,
					TextViewState = state as TextView.DecompilerTextViewState
				}));
		}

		private void tree_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter || e.Key == Key.Return)
			{
				if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
					ShowStateBefore_Click(sender, e);
				else
					ShowStateAfter_Click(sender, e);
				e.Handled = true;
			}
		}
	}
}