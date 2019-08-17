﻿namespace Ecng.Xaml.DevExp.Excel
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Windows.Media;

	using DevExpress.Export.Xl;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Interop;

	public class DevExpExcelWorkerProvider : IExcelWorkerProvider
	{
		private class DevExpExcelWorker : IExcelWorker
		{
			private class SheetData : IDisposable
			{
				private readonly DevExpExcelWorker _worker;
				private readonly Dictionary<int, Dictionary<int, object>> _cells = new Dictionary<int, Dictionary<int, object>>();
				
				public readonly HashSet<int> Columns = new HashSet<int>();
				public readonly HashSet<int> Rows = new HashSet<int>();

				public SheetData(DevExpExcelWorker worker)
				{
					_worker = worker ?? throw new ArgumentNullException(nameof(worker));
				}

				public string Name { get; set; }

				public void SetCell<T>(int col, int row, T value)
				{
					Columns.Add(col);
					Rows.Add(row);
					_cells.SafeAdd(row, key => new Dictionary<int, object>())[col] = value;
				}

				public T GetCell<T>(int col, int row)
				{
					return (T)_cells.SafeAdd(row, key => new Dictionary<int, object>()).TryGetValue(col);
				}

				public void Dispose()
				{
					using (var sheet = _worker._document.CreateSheet())
					{
						if (!Name.IsEmpty())
							sheet.Name = Name;

						foreach (var column in Columns)
						{
							using (sheet.CreateColumn(column))
							{
							}
						}

						foreach (var row in Rows)
						{
							using (var xlRow = sheet.CreateRow(row))
							{
								if (!_cells.TryGetValue(row, out var dict))
									continue;

								foreach (var pair in dict)
								{
									using (var cell = xlRow.CreateCell(pair.Key))
									{
										if (pair.Value == null)
											continue;

										XlVariantValue xlVal;

										if (pair.Value is bool b)
											xlVal = new XlVariantValue { BooleanValue = b };
										else if (pair.Value is DateTime dt)
											xlVal = new XlVariantValue { DateTimeValue = dt };
										else if (pair.Value is string s)
											xlVal = new XlVariantValue { TextValue = s };
										else if (pair.Value.GetType().IsNumeric())
											xlVal = new XlVariantValue { NumericValue = pair.Value.To<double>() };
										//else if (typeof(T) == typeof(Exception))
										//	xlVal = new XlVariantValue { ErrorValue = new NameError() };
										else
											throw new ArgumentOutOfRangeException(pair.Value?.ToString());

										cell.Value = xlVal;
									}
								}	
							}
						}
					}

					Columns.Clear();
					Rows.Clear();

					_cells.Clear();
				}
			}

			private readonly IXlExporter _exporter = XlExport.CreateExporter(XlDocumentFormat.Xlsx);
			private readonly IXlDocument _document;
			private readonly List<SheetData> _sheets = new List<SheetData>();
			private SheetData _currSheet;

			public DevExpExcelWorker(Stream stream)
			{
				_document = _exporter.CreateDocument(stream);
			}

			void IDisposable.Dispose()
			{
				_sheets.ForEach(s => s.Dispose());
				_sheets.Clear();

				_document.Dispose();
			}

			IExcelWorker IExcelWorker.SetCell<T>(int col, int row, T value)
			{
				_currSheet.SetCell(col, row, value);
				return this;
			}

			T IExcelWorker.GetCell<T>(int col, int row)
			{
				return _currSheet.GetCell<T>(col, row);
			}

			IExcelWorker IExcelWorker.SetStyle(int col, Type type)
			{
				return this;
			}

			IExcelWorker IExcelWorker.SetStyle(int col, string format)
			{
				return this;
			}

			IExcelWorker IExcelWorker.SetConditionalFormatting(int col, ComparisonOperator op, string condition, Color? bgColor, Color? fgColor)
			{
				return this;
			}

			IExcelWorker IExcelWorker.RenameSheet(string name)
			{
				_currSheet.Name = name;
				return this;
			}

			IExcelWorker IExcelWorker.AddSheet()
			{
				_currSheet = new SheetData(this);
				_sheets.Add(_currSheet);
				return this;
			}

			bool IExcelWorker.ContainsSheet(string name) => _sheets.Any(s => s.Name.CompareIgnoreCase(name));

			IExcelWorker IExcelWorker.SwitchSheet(string name)
			{
				_currSheet = _sheets.First(s => s.Name.CompareIgnoreCase(name));
				return this;
			}

			int IExcelWorker.GetColumnsCount() => _currSheet.Columns.Count;
			int IExcelWorker.GetRowsCount() => _currSheet.Rows.Count;
		}

		IExcelWorker IExcelWorkerProvider.CreateNew(Stream stream, bool readOnly)
		{
			return new DevExpExcelWorker(stream);
		}

		IExcelWorker IExcelWorkerProvider.OpenExist(Stream stream)
		{
			return new DevExpExcelWorker(stream);
		}
	}
}