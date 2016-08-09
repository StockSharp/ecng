﻿namespace Ecng.Xaml
{
	using System;
	using System.Globalization;
	using System.IO;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.Localization;

	using Ookii.Dialogs.Wpf;

	/// <summary>
	/// Визуальный редактор для выбора файла.
	/// </summary>
	public partial class FileBrowserPicker
	{
		/// <summary>
		/// Создать <see cref="FileBrowserPicker"/>.
		/// </summary>
		public FileBrowserPicker()
		{
			InitializeComponent();
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="File"/>.
		/// </summary>
		public static readonly DependencyProperty FileProperty =
			DependencyProperty.Register(nameof(File), typeof(string), typeof(FileBrowserPicker), new PropertyMetadata(default(string)));

		/// <summary>
		/// Директория.
		/// </summary>
		public string File
		{
			get { return (string)GetValue(FileProperty); }
			set { SetValue(FileProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="IsSaving"/>.
		/// </summary>
		public static readonly DependencyProperty IsSavingProperty =
			DependencyProperty.Register(nameof(IsSaving), typeof(bool), typeof(FileBrowserPicker), new PropertyMetadata(default(bool)));

		public bool IsSaving
		{
			get { return (bool)GetValue(IsSavingProperty); }
			set { SetValue(IsSavingProperty, value); }
		}

		/// <summary>
		/// Событие изменения <see cref="File"/>.
		/// </summary>
		public event Action<string> FileChanged;

		private void OpenFile_Click(object sender, RoutedEventArgs e)
		{
			var dlg = IsSaving
				? (VistaFileDialog)new VistaSaveFileDialog()
				: new VistaOpenFileDialog { CheckFileExists = true };

			if (!FilePath.Text.IsEmpty())
				dlg.FileName = FilePath.Text;

			var owner = sender is DependencyObject ? ((DependencyObject)sender).GetWindow() : null;

			if (dlg.ShowDialog(owner) == true)
			{
				FilePath.Text = dlg.FileName;
				FileChanged?.Invoke(dlg.FileName);
			}
		}

		private void FilePath_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			FileChanged?.Invoke(FilePath.Text);
		}
	}

	class FileValidationRule : ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			if (value == null || !File.Exists((string)value))
				return new ValidationResult(false, "Invalid file path.".Translate());

			return ValidationResult.ValidResult;
		}
	}
}